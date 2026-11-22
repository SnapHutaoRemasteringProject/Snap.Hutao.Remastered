# 元数据服务重构：GitMetadataService / ProtoMetadataService

> 状态：**设计稿，未实现**。本文只描述目标结构与迁移步骤，当前代码仍是
> [`Service/Metadata/MetadataService.cs`](../src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Service/Metadata/MetadataService.cs) 的 Git + JSON 单一实现。
> 文中"应用"事实来自 `feat/aot-compatible` @ `b08006116`；"产物"事实来自
> `D:\SnapHutaoRemasteringProject\MetadataBuilder` @ `cb1eb30` 的 `Proto/`、`Core/Builder/ProtoBuilder.cs`
> 与 `Core/Builder/*CurveBuilder.cs`。
>
> **2026-11-22 更新**：产物已补齐 `AvatarCurve` / `MonsterCurve` / `WeaponCurve`（builder `89ddba9`）并引入
> `LegacyJsonFileNames`（builder `cb1eb30`），Proto 的类别覆盖达到 30/30；本文已同步覆盖矩阵、字段数、
> 映射规则与实施计划。仍缺 `schema_version`、清单文件、语言目录输出与"缺失类别致命"（见 §10）。
>
> 关联：`CODE_REVIEW.md#ENG-08`（`LegacyMetadataService` 死代码）、`CODE_REVIEW.md#ARCH-01`（避免新增 `IServiceProvider` 服务定位）、
> `CLAUDE.md` §5.5（元数据初始化）、§5.9（Sentry span）、§6（parse-first / 事务式替换）。

## 结论摘要（TL;DR）

1. 现有 `MetadataService`（约 27 处注入、40 次按类别读取）**改名为 `GitMetadataService`**，
   保留 JSON + git 仓库路径，作为兼容源与回退源；`IMetadataService` 仍是消费方契约，消费方一行不改。
2. 新增 **`ProtoMetadataService`** 读取 `MetadataBuilder` 产出的 `GenshinMetadata.{locale}.hutaobin`
   （`IV ‖ AES-256-CBC(zlib(protobuf))`），单文件替代 158 个 JSON 文件。
3. 两个实现按 **`MetadataSourceKind` keyed singleton** 注册；新增一个**无状态门面 `MetadataService`**
   负责选源、回退和 `ClearCache` 广播——否则 27 个消费方无法在运行时切换来源。
4. 数据流从"页面按需读 JSON 文件"改为"**启动时确保并解析 bundle，页面按需映射类别**"；
   `InitializeAsync` 变为**可自启动、幂等**，不再依赖 `AppActivation` 的调用顺序。
5. 新增 `MetadataCache`（带来源/语言前缀 + 代际号）与 **`ClearCache()`** 契约：
   清派生缓存、释放解析后的 bundle、代际 +1，保证清理与并发加载不会互相污染。
6. 二进制包已覆盖全部 30 个应用类别（schema 共 35 个 message 字段，5 个暂不消费）。
   仍缺的是**版本与分发契约**：`MetadataInfo` 没有 `schema_version`、没有清单文件、单语言包不在语言目录、
   缺失类别不致命（见 §10）。这些不阻塞映射层开发，但阻塞把 Proto 设为默认源。

## 1. 为什么要改（本机实测数据，7.2 版本）

| 指标 | JSON / git（现状） | proto bundle（新） |
|---|---|---|
| 单语言文件数 | 158 个（CHS） | 1 个 |
| 单语言体积 | 19.2 MiB（CHS） | 3.60 MiB（CHS） |
| 全仓库工作树 | 349.9 MiB / 2,378 文件 | — |
| 拉取方式 | 整仓 `git` shallow clone + fetch（含 `.git`，本机 388 MiB） | 单文件条件请求（约 3.6 MiB，未变化时仅清单） |
| 解析方式 | 每个类别一次 `JsonSerializer` 反序列化 | 一次 protobuf 解析，类别按需映射 |
| 分散文件 | `Avatar/` 目录 122 个文件 / 6.32 MiB，逐文件打开 | 无 |
| 缓存失效 | 无（只能重启） | `ClearCache()` |

实测来源：`D:\SnapHutaoRemasteringProject\Snap.Metadata.Work`、`D:\SnapHutaoRemasteringProject\Snap.Metadata.Hutaobin`。
`.git` 体积随本地历史变化，不能当作 shallow clone 的固定值；上表只用来说明"整仓拉取"与"单文件"的量级差异。

除体积/延迟外，还有三个结构性问题：

- **启动被整仓 clone 阻塞。** `AppActivation` 在 `Task.WhenAll` 里触发
  `IMetadataService.InitializeInternalAsync()`（[`Core/LifeCycle/AppActivation.cs:296`](../src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Core/LifeCycle/AppActivation.cs)），
  内部是 `gitRepositoryService.EnsureRepositoryAsync("Snap.Metadata")`；任何页面在此之前只会挂起。
- **数据源写死。** 文件路径由 [`MetadataOptions.LocalizedDataFolder`](../src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Service/Metadata/MetadataOptions.cs)
  决定，指向 `{Data}/Repository/Snap.Metadata/Genshin/{locale}`，没有抽象层。
- **没有失效入口。** 缓存键是 `"MetadataService.Cache.{name}"`，没有语言/来源前缀，也没有任何 `Remove`，
  元数据更新后只能重启进程。

## 2. 现状事实清单

### 2.1 契约与实现

| 文件 | 事实 |
|---|---|
| `Service/Metadata/IMetadataService.cs` | `IMetadataServiceInitialization` + `IMemoryCache MemoryCache` + `FromCacheOrFileAsync<T>(MetadataFileStrategy, CancellationToken)` + `InitializeAsync()` |
| `Service/Metadata/MetadataService.cs` | `[Service(ServiceLifetime.Singleton, typeof(IMetadataService))]`，164 行；私有 `initializeCompletionSource`(TCS) + `SemaphoreSlim initializationGate` + `volatile bool isInitialized`；`[HttpClient(Default)]` 属性存在但服务内没有 HTTP 调用（残留） |
| `Service/Metadata/MetadataOptions.cs` | `LocalizedDataFolder` = `{Data}/Repository/Snap.Metadata/Genshin/{locale}`，创建目录 |
| `Service/Metadata/MetadataFileStrategy.cs` / `MetadataFileStrategies.cs` | 30 个类别；`IsScattered` 仅 `Avatar` 为 true；`BydMaterial` 的文件名仍是 `BeyondItem` |
| `Service/Metadata/MetadataServiceImmutableArrayExtension.cs` | 26 个强类型读取扩展，全部转调 `FromCacheOrFileAsync<T>` |
| `Service/Metadata/MetadataServiceImmutableDictionaryExtension.cs` | 28 个派生字典，直接写共享 `IMemoryCache`，键形如 `MetadataService.Cache.{Name}.Map.{K}.{V}` |
| `Service/Metadata/ContextAbstraction/MetadataServiceContextExtension.cs` | 42 个 `if (context is IMetadata…)` 分支、40 次 `await metadataService.…`；大量页面由此取数据 |
| `Service/Metadata/LegacyMetadataService.cs` / `LegacyMetadataOptions.cs` | 397 + 51 行 HTTP 下载实现，`[Service]` 已注释，**死代码**（ENG-08） |

### 2.2 当前数据流

```
AppActivation (后台)
  └─ IMetadataService.InitializeInternalAsync()
        ├─ initializationGate.Wait
        └─ IGitRepositoryService.EnsureRepositoryAsync("Snap.Metadata")   ← 整仓 clone/fetch
             成功 → isInitialized = true, TCS.SetResult
             失败 → 记日志；TCS 保持 pending（下次重试）

页面 Load
  └─ metadataService.InitializeAsync()          ← 等 TCS，若没人调用 InitializeInternal 会一直等
  └─ GetContextAsync<TContext>()                ← 40 次按类别读取
        └─ FromCacheOrFileAsync<T>(strategy)
             ├─ IMemoryCache 命中 → Unsafe.Unbox<ImmutableArray<T>>
             └─ 未命中 → 读 {locale}/{Name}.json（或 {Name}/*.json 逐个读）
                        → JsonSerialize.DeserializeAsync<ImmutableArray<T>>
                        → IMemoryCache.Set(cacheKey, result)      ← 永不失效
```

### 2.3 可直接复用的既有能力

| 能力 | 位置 | 用途 |
|---|---|---|
| 元数据模板端点 | [`IInfrastructureMetadataEndpoints`](../src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Web/Endpoint/Hutao/IInfrastructureMetadataEndpoints.cs)（`MetadataTemplate()` / `Metadata(template, locale, file)`） | bundle 下载 URL |
| 模板模型 + JSON 契约 | `Service/Metadata/MetadataTemplate.cs`、`HttpApiJsonContext` | 下载前置请求 |
| 重试/代理/域名回退 | `RetryHttpHandler`（幂等方法 3 次、TLS 失败改写域名）、`HttpProxyUsingSystemProxy` | 网络鲁棒性 |
| 哈希 | [`XxHash64.HashFileAsync`](../src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Core/IO/Hashing/XxHash64.cs) | 包完整性 |
| 字符串驻留 | `JsonWireOptions` 里的 `InternStringConverter` | proto 映射必须同样驻留 |
| 后台进度提示 | `BackgroundActivityOptions` + `BackgroundActivity` | 下载/解析进度 |
| 初始化闸门 | 现 `MetadataService` 的 TCS + `SemaphoreSlim` 语义（含"失败不完成 TCS，允许后续重试"） | 抽到基类 |

## 3. 产物契约（MetadataBuilder 现状）

### 3.1 文件与格式

| 项 | 值 |
|---|---|
| 输出目录 | `MetadataOptions.ProtoOutputDirectory`（默认 `D:\SnapHutaoRemasteringProject\Snap.Metadata.Hutaobin`） |
| 合并包 | `GenshinMetadata.hutaobin`（62,633,616 B ≈ 59.7 MiB，全部语言） |
| 单语言包 | `GenshinMetadata.{locale}.hutaobin`（CHS 3,771,744 B ≈ 3.60 MiB；15 个语言 3.60–4.73 MiB） |
| 磁盘布局 | `EncryptProtoOutput = true` 时：`IV(16) ‖ AES-256-CBC(zlib(protobuf)), PKCS7`；`false` 时明文 protobuf |
| 密钥 | `ProtoBuilder.AesKeyHex`（32 字节，硬编码），注释要求与客户端同步 |
| 顶层消息 | `MetadataBundle { GameVersion version; MetadataInfo info; repeated LanguageMetadata languages }` |
| `MetadataInfo` | 只有 `publisher`、`generated_at`；**没有 schema/format 版本** |
| 单语言包内容 | `languages` 恰好 1 项，`language` 为 locale 名（`CHS`/`EN`/…） |
| 类别字段 | `LanguageMetadata` 共 35 个 message 字段（字段号 2–36） |
| 曲线消息 | `AvatarCurve` / `MonsterCurve` / `WeaponCurve` 各为 `{ repeated GrowCurvePoint curves; int32 level }`，共享 `common.proto` 的 `GrowCurvePoint { GrowCurveValue type; double value }` |

> 加密只是**混淆**：AES-CBC 无认证标签，密钥随客户端分发。它不构成安全边界，
> 客户端必须把包当不可信输入处理（见 §7.2）。

### 3.2 类别覆盖矩阵（对当前 app）

应用 `MetadataFileStrategies` 共 30 个类别；`MetadataFileJsonContext` 注册 27 个根类型（`GrowCurve` 服务 3 个曲线类别、`Promote` 服务 2 个类别）。
proto 的 `LanguageMetadata` 共 35 个类别字段，其中 30 个被应用消费、5 个暂未消费。

| 应用类别（JSON 文件名） | proto 字段 | 状态 |
|---|---|---|
| `Achievement` | `achievements` | ✅ |
| `AchievementGoal` | `achievement_goals` | ✅ |
| `Avatar`（分散，122 文件） | `avatars` | ✅ |
| `AvatarCurve` | `avatar_curves` | ✅ |
| `AvatarPromote` | `avatar_promotes` | ✅ |
| `BeyondItem`（proto 类别名 `BydMaterial`） | `byd_materials` | ✅（名称不一致，见下） |
| `Chapter` | `chapters` | ✅ |
| `Combine` | `combines` | ✅ |
| `CookRecipe` | `cook_recipes` | ✅ |
| `DisplayItem` | `display_items` | ✅ |
| `GachaEvent` | `gacha_events` | ✅ |
| `HardChallengeSchedule` | `hard_challenge_schedules` | ✅ |
| `HyperLinkName` | `hyper_link_names` | ✅ |
| `MainQuest` | `main_quests` | ✅ |
| `Material` | `materials` | ✅ |
| `Monster` | `monsters` | ✅ |
| `MonsterCurve` | `monster_curves` | ✅ |
| `ProfilePicture` | `profile_pictures` | ✅ |
| `Reliquary` | `reliquaries` | ✅ |
| `ReliquaryMainAffix` | `reliquary_main_affixes` | ✅ |
| `ReliquaryMainAffixLevel` | `reliquary_main_affix_levels` | ✅ |
| `ReliquarySet` | `reliquary_sets` | ✅ |
| `ReliquarySubAffix` | `reliquary_sub_affixes` | ✅ |
| `RoleCombatSchedule` | `role_combat_schedules` | ✅ |
| `TowerFloor` | `tower_floors` | ✅ |
| `TowerLevel` | `tower_levels` | ✅ |
| `TowerSchedule` | `tower_schedules` | ✅ |
| `Weapon` | `weapons` | ✅ |
| `WeaponCurve` | `weapon_curves` | ✅ |
| `WeaponPromote` | `weapon_promotes` | ✅ |

- `BydMaterial`：proto 类别名与 JSON 文件名不同（JSON 仍发布 `BeyondItem.json`，所以 Git 源的
  `MetadataFileStrategy` 继续叫 `BeyondItem`）。`ProtoBuilder.LegacyJsonFileNames` 负责两侧解析；
  客户端同样需要一张"类别 → proto 名"的表（§7.3 的 `MetadataCategory.ProtoName`）。
- 暂未消费的 5 个 proto 类别：`furnitures`、`furniture_makes`、`furniture_suites`、`furniture_types`、
  `reliquary_affix_weights`（`Model/Metadata/Furniture/*` 已存在，但没有对应的 `MetadataFileStrategy`）。

结论：**Proto 源覆盖 30/30 个应用类别**，映射层不再需要"部分委托给 Git"的混合方案。

## 4. 目标数据流

```
启动（AppActivation，后台）
  └─ IMetadataService.InitializeInternalAsync()                      [门面 MetadataService]
        ├─ MetadataSourceOptions.Source（默认 Auto）
        ├─ Auto/Proto → IProtoMetadataService.InitializeInternalAsync
        │     ├─ IMetadataBundleSource.EnsureLocalBundleAsync
        │     │     ├─ GET {api}/metadata/template                    （可用本地缓存 24h）
        │     │     ├─ GET {template}/Genshin/{locale}/GenshinMetadata.manifest.json
        │     │     ├─ 清单哈希 == 本地文件哈希 → 跳过下载
        │     │     ├─ 否则 GET …/GenshinMetadata.hutaobin（流式、限时）
        │     │     ├─ size + xxHash64 + schema + language 校验
        │     │     └─ 写 .tmp → 原子替换 {Data}/Metadata/{locale}/GenshinMetadata.hutaobin
        │     ├─ MetadataBundleCodec.Read → 解析 + 头部校验
        │     └─ 保留 LanguageMetadata（**不映射类别**）
        ├─ 失败 → IGitMetadataService.InitializeInternalAsync（clone/fetch Snap.Metadata）
        ├─ 成功 → active = 选中的源；TCS 完成
        └─ 全部失败 → 向 INetworkRetryCoordinator 注册/标记 pending；网络恢复后重跑本流程
                        （TCS 保持 pending，等待重试成功或 ShutdownToken）

页面加载
  └─ metadataService.InitializeAsync()          ← 快路径；未启动时**自行启动**初始化
  └─ GetContextAsync<TContext>()                ← 42 个分支 / 40 次按类别读取
        └─ FromCacheOrSourceAsync<T>(category)
              ├─ [L2] MetadataCache 命中 → 返回（boxed ImmutableArray<T> 解箱）
              └─ [L2] 未命中
                    ├─ Proto：ProtoCategoryTable[category].Read(language) → 逐条映射
                    └─ Git ：读 {locale}/{Name}.json 或 {Name}/*.json → JsonSerialize
                    → MetadataCache.SetIfCurrent(generation, key, value)

手动刷新 / 数据源切换 /（未来）定时更新
  └─ IMetadataService.ClearCache()              ← L2/L3 清空 + 释放 bundle + 代际 +1
  └─ （可选）IMetadataBundleSource 重新确保文件 → 下次读取重新解析
```

与现状的关键差异：

1. **初始化不再依赖调用顺序**：`InitializeAsync()` 若发现初始化未启动会自己启动；`AppActivation` 的
   `SafeForget` 仍是后台预热入口，页面不再可能因为"没人调用 `InitializeInternalAsync`"而永久等待。
   启动失败改为登记到 `INetworkRetryCoordinator`（与 `RepositoryCatalogService` 同一套模式），
   网络恢复后自动重跑，而不是像现在这样只能等下一次启动。
2. **解析与映射分离**：bundle 只在初始化时解析一次；类别映射推迟到第一次访问该类别，
   单页只付出它真正需要的类别成本。
3. **源可替换**：消费方只看到 `IMetadataService`，选源/回退/清理都在门面内完成。
4. **缓存可失效**：所有派生数据都进 `MetadataCache`，`ClearCache` 一次清干净且不误删其他服务的缓存。

## 5. 类型与职责

### 5.1 类型表

| 类型 | 种类 | 职责 |
|---|---|---|
| `IMetadataService` | 契约（不改名） | `IsInitialized` / `Cache` / `InitializeAsync` / `FromCacheOrSourceAsync<T>` / `ClearCache` |
| `IMetadataServiceInitialization` | 契约（不改名） | `InitializeInternalAsync(CancellationToken)`，启动入口 |
| `IGitMetadataService : IMetadataService` | 契约 | keyed `Git` 注册类型；暴露 `RepositoryDirectory` 便于诊断 |
| `IProtoMetadataService : IMetadataService` | 契约 | keyed `Proto` 注册类型；暴露 `BundleInfo` / `BundleFilePath` |
| `MetadataService` | 门面，非 keyed 单例 | 选源、回退、初始化状态机、`ClearCache` 广播、暴露 `ActiveSource` |
| `MetadataServiceBase` | 抽象基类（不注册） | 初始化闸门 + TCS 语义 + logger + `MetadataCache` 实例 |
| `GitMetadataService` | 实现（原 `MetadataService`） | JSON + git 仓库读取 |
| `ProtoMetadataService` | 实现（新增） | bundle 确保/解析/类别映射 |
| `MetadataCategory` / `MetadataCategories` | 值类型 + 静态表 | 原 `MetadataFileStrategy` / `MetadataFileStrategies`；新增 `ProtoName`（默认 = `Name`，`BydMaterial` 例外） |
| `MetadataCache` | sealed | 前缀 + 代际 + 键跟踪 + `Clear()` |
| `MetadataSourceKind` | enum | `Auto/Git/Proto`；**`Auto` 永不作为 DI key** |
| `MetadataSourceOptions` | 选项 | 持久化来源偏好，`IObservableProperty<MetadataSourceKind>` |
| `GitMetadataOptions` | 选项 | git 仓库名（`Snap.Metadata`）与本地 JSON 路径 |
| `MetadataBundleOptions` | 选项 | bundle 文件名、本地目录、schema 上限、下载超时 |
| `IMetadataBundleSource` | 契约 | 确保本地 bundle 存在并返回路径 |
| `HttpMetadataBundleSource` | 实现 | 模板端点 + 清单 + 条件下载 + 原子替换 |
| `MetadataBundleCodec` | static | 解密/解压/解析 + 头部校验 + 尺寸上限 |
| `MetadataBundleInfo` | 值对象 | 来源、游戏版本、生成时间、schema 版本、语言、哈希 |
| `ProtoCategoryTable` | static | `MetadataCategory` → 适配器；覆盖性校验 |
| `ProtoCategoryAdapter<TProto, TModel>` | 抽象 | 枚举某类别的 proto 条目并映射成 `ImmutableArray<TModel>` |
| `MetadataConvert` | static | 枚举/整数/时间/集合的集中转换与失败策略 |

### 5.2 契约

```csharp
// Service/Metadata/IMetadataService.cs
public interface IMetadataService : IMetadataServiceInitialization
{
    bool IsInitialized { get; }

    MetadataCache Cache { get; }

    ValueTask<bool> InitializeAsync();

    ValueTask<ImmutableArray<T>> FromCacheOrSourceAsync<T>(MetadataCategory category, CancellationToken token = default)
        where T : class;

    void ClearCache();
}
```

```csharp
// Service/Metadata/Git/IGitMetadataService.cs
public interface IGitMetadataService : IMetadataService
{
    string RepositoryDirectory { get; }
}

// Service/Metadata/Proto/IProtoMetadataService.cs
public interface IProtoMetadataService : IMetadataService
{
    MetadataBundleInfo? BundleInfo { get; }

    string BundleFilePath { get; }
}
```

`FromCacheOrFileAsync` → `FromCacheOrSourceAsync`：proto 源没有"文件"这个概念，继续叫 File 会误导；
改动只落在两个扩展文件（26 + 28 个方法）+ 实现，消费方（27 处）不受影响。

### 5.3 DI 注册（全部由生成器产出，不手写）

```csharp
[Service(ServiceLifetime.Singleton, typeof(IGitMetadataService), Key = MetadataSourceKind.Git)]
public sealed partial class GitMetadataService : MetadataServiceBase, IGitMetadataService
{
    [GeneratedConstructor(CallBaseConstructor = true)]
    public partial GitMetadataService(IServiceProvider serviceProvider);
}

[Service(ServiceLifetime.Singleton, typeof(IProtoMetadataService), Key = MetadataSourceKind.Proto)]
public sealed partial class ProtoMetadataService : MetadataServiceBase, IProtoMetadataService
{
    [GeneratedConstructor(CallBaseConstructor = true)]
    public partial ProtoMetadataService(IServiceProvider serviceProvider);
}

[Service(ServiceLifetime.Singleton, typeof(IMetadataService))]
public sealed partial class MetadataService : IMetadataService, IDisposable
{
    [FromKeyed(MetadataSourceKind.Git)]
    private readonly IGitMetadataService gitMetadataService;

    [FromKeyed(MetadataSourceKind.Proto)]
    private readonly IProtoMetadataService protoMetadataService;

    [GeneratedConstructor]
    public partial MetadataService(IServiceProvider serviceProvider);
}
```

要点：

- 门面用 `[FromKeyed]` 注入两个实现（生成器会产出 `GetRequiredKeyedService<T>(key)`，见
  `NotifyIconViewModel` 的生成构造）。两个 keyed 单例只在门面首次构建时创建；
  它们的构造函数**不做 I/O**，因此"两个都构造"没有代价，也避免门面注入 `IServiceProvider`
  （`CODE_REVIEW.md#ARCH-01` 明令不要新增服务定位）。
- 门面必须保持 **非 keyed** 注册：否则 `ValidateOnBuild = true` 会在启动时报
  `IMetadataService` 不可解析；27 个消费方也全部依赖非 keyed 解析。
- 两个实现都从 `MetadataServiceBase` 继承，使用
  `[GeneratedConstructor(CallBaseConstructor = true)] public partial XxxMetadataService(IServiceProvider serviceProvider);`，
  基类构造函数手写（它不是 `[Service]` 类型，生成器不为它生成构造函数）。
- 为什么 key 在 `IGitMetadataService` / `IProtoMetadataService` 而不是直接 keyed 注册两次
  `IMetadataService`：Proto 源需要额外暴露 `BundleInfo` / `BundleFilePath`（诊断/设置页），
  子接口让门面拿得到；如果只 key `IMetadataService`，门面就必须往下转型具体类。
  备选的"消费方自己写 `[FromKeyed(MetadataSourceKind.Proto)]`"被否决：那会把来源选择固化到 27 个消费方，
  失去运行时回退能力。
- `MetadataSourceKind.Auto` 只用于设置与选源逻辑，**永不**作为 DI key（否则 `ValidateOnBuild` 会通过、
  运行时却解析不到，属于最难排查的一类注册错误）；用一条断言（启动期或测试）保证
  "keyed 注册集合 == {Git, Proto}"。

### 5.4 基类：单个源的初始化闸门

把现有"TCS + SemaphoreSlim + isInitialized"（含"失败不完成 TCS、允许后续重试"的语义）抽到基类，
避免两份实现漂移。基类只负责**单个源**；选源/回退在门面（§5.5）：

```csharp
public abstract class MetadataServiceBase : IMetadataService
{
    private readonly SemaphoreSlim initializationGate = new(1, 1);
    private TaskCompletionSource initializationCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private bool isInitialized;

    protected MetadataServiceBase(IServiceProvider serviceProvider)
    {
        Logger = serviceProvider.GetRequiredService<ILogger<MetadataService>>();

        // 前缀 = Metadata.{Git|Proto}.{locale}
        // SourceName 只允许返回常量，不得依赖派生类字段（构造函数期虚调用）
        string locale = serviceProvider.GetRequiredService<CultureOptions>().LocaleName;
        Cache = new(serviceProvider.GetRequiredService<IMemoryCache>(), $"Metadata.{SourceName}.{locale}");
    }

    protected ILogger Logger { get; }

    public MetadataCache Cache { get; }

    public bool IsInitialized => Volatile.Read(ref isInitialized);

    public async ValueTask<bool> InitializeAsync()
    {
        if (IsInitialized) return true;
        await initializationCompletionSource.Task.WaitAsync(XamlApplicationLifetime.ShutdownToken).ConfigureAwait(false);
        return IsInitialized;
    }

    public async ValueTask<bool> InitializeInternalAsync(CancellationToken token = default)
    {
        using CancellationTokenSource linked = CancellationTokenSource.CreateLinkedTokenSource(token, XamlApplicationLifetime.ShutdownToken);
        await initializationGate.WaitAsync(linked.Token).ConfigureAwait(false);
        try
        {
            if (IsInitialized) return true;
            using SentrySpanScope span = SentrySpanScope.StartOperation($"{SourceName}.initialize", "metadata");
            try
            {
                isInitialized = await InitializeCoreAsync(linked.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception exception)
            {
                Logger.LogWarning(exception, "Metadata initialization failed; awaiting a later retry");
                isInitialized = false;
            }

            return IsInitialized;
        }
        finally
        {
            // 沿用现语义：只有成功才完成 TCS；失败保持 pending，
            // 由 ShutdownToken 释放挂起的消费者，后续初始化可以重试。
            if (IsInitialized) initializationCompletionSource.TrySetResult();
            initializationGate.Release();
        }
    }

    protected abstract string SourceName { get; }

    protected abstract ValueTask<bool> InitializeCoreAsync(CancellationToken token);

    public abstract ValueTask<ImmutableArray<T>> FromCacheOrSourceAsync<T>(MetadataCategory category, CancellationToken token)
        where T : class;

    public virtual void ClearCache()
    {
        Cache.Clear();
    }
}
```

要点：

- `Cache` 的构造只需要 `IMemoryCache` + 前缀；前缀由基类用 `Metadata.{SourceName}.{locale}` 组装，
  `SourceName` 在派生类里是常量（`Git` / `Proto`）。
- `InitializeCoreAsync` 对 Proto 是"确保文件 + 解码 + 头部校验"，对 Git 是"clone/fetch 仓库"。
- 基类的 TCS 只服务于"有人直接持有 keyed 实例"的场景；门面有自己的一层 TCS（§5.5）。

### 5.5 门面：选源、自启动与网络重试

```csharp
[Service(ServiceLifetime.Singleton, typeof(IMetadataService))]
public sealed partial class MetadataService : IMetadataService
{
    private const string RetryKey = "MetadataService.Initialization";

    private readonly MetadataSourceOptions sourceOptions;
    private readonly INetworkRetryCoordinator networkRetryCoordinator;
    private readonly ILogger<MetadataService> logger;
    private readonly IMessenger messenger;
    private readonly Lock gate = new();

    // §5.3 的两个 [FromKeyed] 字段：gitMetadataService / protoMetadataService
    [GeneratedConstructor]
    public partial MetadataService(IServiceProvider serviceProvider);

    private TaskCompletionSource initialized = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private Task<bool>? attempt;
    private IMetadataService? active;
    private IDisposable? retryRegistration;

    public bool IsInitialized => active?.IsInitialized is true;

    public MetadataCache Cache => active?.Cache ?? throw HutaoException.InvalidOperation(SH.ServiceMetadataNotInitialized);

    public async ValueTask<bool> InitializeAsync()
    {
        if (IsInitialized) return true;                    // 快路径

        Task completion;
        lock (gate) { completion = initialized.Task; }
        _ = InitializeInternalAsync();                     // 自启动；并发调用由 attempt 合并

        await completion.WaitAsync(XamlApplicationLifetime.ShutdownToken).ConfigureAwait(false);
        return IsInitialized;
    }

    public ValueTask<bool> InitializeInternalAsync(CancellationToken token = default)
    {
        lock (gate) { return new(attempt ??= AttemptAsync(token)); }
    }

    private async Task<bool> AttemptAsync(CancellationToken token)
    {
        MetadataSourceKind preference = sourceOptions.Source.Value;
        bool fellBack = false;
        try
        {
            foreach (IMetadataService candidate in EnumerateCandidates(preference))
            {
                if (await candidate.InitializeInternalAsync(token).ConfigureAwait(false))
                {
                    active = candidate;
                    networkRetryCoordinator.ClearPending(RetryKey);
                    lock (gate) { initialized.TrySetResult(); }

                    // 只有"想要 Proto 却用了 Git"才提示；Auto 的静默回退不打扰用户
                    if (fellBack && preference is MetadataSourceKind.Proto)
                    {
                        messenger.Send(InfoBarMessage.Warning(SH.ServiceMetadataSourceFallback));
                    }

                    return true;
                }

                fellBack = true;
            }

            // 失败：允许下一次调用/网络恢复重开，TCS 保持 pending（沿用现语义）
            lock (gate) { attempt = null; }
            networkRetryCoordinator.MarkPending(RetryKey, SH.ServiceMetadataInitializationFailed);
            return false;
        }
        finally
        {
            lock (gate) { if (!IsInitialized) attempt = null; }
        }
    }

    private async ValueTask<bool> RetryAfterNetworkRecoveryAsync(CancellationToken token)
    {
        lock (gate) { attempt = null; }
        return await InitializeInternalAsync(token).ConfigureAwait(false);
    }

    partial void PostConstruct(IServiceProvider serviceProvider)
    {
        retryRegistration = networkRetryCoordinator.Register(RetryKey, RetryAfterNetworkRecoveryAsync);
    }
}
```

选源/回退策略（完整矩阵见 §9.2）：

- `EnumerateCandidates(preference)`：`Auto`/`Proto` → `[protoMetadataService, gitMetadataService]`；
  `Git` → `[gitMetadataService]`。两个字段都是 `[FromKeyed]` 注入的单例，顺序即优先级。
- `Auto`：Proto → Git。
- 显式 `Proto`：先 Proto；失败仍回退 Git，但发一次 `InfoBarMessage.Warning`
  （不能因为一次发布事故让整个应用不可用，"Proto 偏好"不等于"禁止 Git"）。
- 显式 `Git`：只用 Git，不回退。
- 回退只发生在**本次尝试**内；页面访问不会触发对 Proto 的再次尝试。重新选源的入口只有三个：
  网络恢复时 `NetworkRetryCoordinator` 的重跑、用户刷新 / `ClearCache`、下次启动。
- 网络恢复后的重试走 `NetworkRetryCoordinator`（与 `RepositoryCatalogService.ReconnectSharedAsync` 同一模式：
  `Register` + `MarkPending`/`ClearPending`），并且**由元数据自己的 job 产出数据**，
  不能让等待元数据的页面 job 相互阻塞（该文件 284 行的注释记录的正是这个坑）。
- 数据源设置变更：顺序必须是 **`ClearCache()`（旧源）→ `active = null` → 重建 TCS**，然后下次
  `InitializeAsync` 重选。这样在途读取要么因旧缓存代际已变而不写入，要么以 `not initialized` 失败一次；
  不会把新源数据写进旧源的缓存。TCS 必须整体替换：已完成的旧 TCS 会让 `InitializeAsync` 立刻返回
  `false`（同样的坑在 `LegacyMetadataService` 里已经用"重建 TCS"处理过一次）。
- `Cache` 在初始化完成前被访问时，仍然走现有 `SH.ServiceMetadataNotInitialized` 语义
  （`HutaoException.InvalidOperation`），与 `FromCacheOrFileAsync` 的 `Verify.Operation` 一致。
- `AttemptAsync` 必须自己吞掉非取消异常并记日志（返回 `false`）：它同时被 `InitializeAsync` 的
  自启动路径 `_ = InitializeInternalAsync()` 丢弃，未观察的异常会变成未处理异常。
- 门面实现 `IDisposable` 释放 `retryRegistration`；`ValidateOnBuild` 只校验注册可解析，不校验这个引用。

## 6. 缓存与 `ClearCache`

### 6.1 三层缓存

| 层 | 内容 | 位置 | 生命周期 |
|---|---|---|---|
| L0 | 磁盘文件（JSON 树 / bundle / 清单） | `{Data}/Repository/Snap.Metadata/…`、`{Data}/Metadata/{locale}/…` | 跨进程 |
| L1 | 解析后的 `LanguageMetadata`（proto 源） | 服务字段，`AsyncLock` 保护 | 进程内，`ClearCache` 释放 |
| L2 | 类别数组 `ImmutableArray<T>` | `MetadataCache` | 进程内，`ClearCache` 清空 |
| L3 | 派生字典（id→item、name→item、level→curve） | `MetadataCache`（由扩展方法写入） | 同上 |

git 源没有 L1（每次未命中直接读文件），保留 L2/L3。

### 6.2 键与前缀

```csharp
// 前缀示例：Metadata.Proto.CHS / Metadata.Git.CHS
public string CreateKey(MetadataCategory category, string shape, params ReadOnlySpan<string> parts);
// Metadata.Proto.CHS.Avatar
// Metadata.Proto.CHS.ReliquarySet.Map.ReliquarySetId.ReliquarySet
```

- 前缀包含 **source + locale**：现有键是 `MetadataService.Cache.…`，既没有来源也没有语言，
  切换来源或（未来）不重启切换语言时会直接读到脏数据。
- `MetadataServiceImmutableDictionaryExtension` 里手写的 `nameof(...)` 拼接全部改走 `Cache.CreateKey`，
  顺便修掉 `GetIdToDisplayItemAndMaterialMapAsync` 里那个与其它键不同构的特例键。

### 6.3 代际号

```csharp
public sealed class MetadataCache
{
    private readonly IMemoryCache cache;
    private readonly ConcurrentDictionary<string, byte> keys = new(StringComparer.Ordinal);
    private int generation;

    public int Generation => Volatile.Read(ref generation);

    public bool TryGet(string key, [NotNullWhen(true)] out object? value);

    // 派生字典/数组的统一入口：创建前捕获代际，创建后只在代际未变时写入
    public ValueTask<T> GetOrCreateAsync<T>(string key, Func<CancellationToken, ValueTask<T>> factory, CancellationToken token)
        where T : notnull;

    // 只把"清理前开始、清理后结束"的加载结果丢弃，不污染新代际
    public void SetIfCurrent(int generation, string key, object value);

    public void Clear()
    {
        Interlocked.Increment(ref generation);
        foreach (string key in keys.Keys) cache.Remove(key);
        keys.Clear();
    }
}
```

`Clear()` 与并发加载的竞态：

1. 加载线程在读取前捕获 `g = Cache.Generation`。
2. 映射/反序列化完成后调用 `SetIfCurrent(g, key, value)`。
3. 若期间发生 `Clear`（代际已 +1），结果**不入缓存**；调用方仍拿到本次结果（可能是清理前读到的旧文件），
   但下一次读取一定重新加载。这比"清理时丢弃调用方结果"简单，也比"允许旧结果回写"正确。
4. `keys.Clear()` 之后写入的键属于新代际，不会被误删。

### 6.4 `ClearCache()` 契约

| 项 | 约定 |
|---|---|
| 签名 | `void ClearCache()`（同步、幂等、线程安全；不抛异常） |
| 清理范围 | **仅元数据服务的键**：`Metadata.{Git|Proto}.{locale}.…` 前缀下、`MetadataCache` 跟踪的键；不触碰 `AnnouncementService` 等共享 `IMemoryCache` 的其他使用者 |
| 释放 | proto 源释放 L1 `LanguageMetadata` 引用；git 源无需释放 |
| 初始化状态 | **不重置** `IsInitialized`。磁盘/网络来源仍然有效，下一次类别读取会重新解析/重读文件；页面的 `InitializeAsync()` 快路径继续成立 |
| 失败 | 单键 `Remove` 失败不影响其余键（`IMemoryCache.Remove` 本身不抛业务异常） |
| Sentry | `metadata.cache.clear` span，仅记录被清理的键数量与来源，不写路径 |
| 调用时机 | ① 用户"重新加载元数据"命令（新增，见 §12 Phase 3）；② 数据源设置变更后；③ 成功替换 bundle 后（刷新流程）；④ 语言切换（当前靠 `AppInstance.Restart`，重启后天然干净；保留此调用以备将来取消重启） |
| 不负责 | 删磁盘文件、重新下载、改变来源 |

门面的 `ClearCache()` 广播到**两个** keyed 源：清理非活动源是幂等且廉价的操作，
能避免"切到 Proto → 切回 Git"时命中旧的 L2/L3 数据。实现上先 `active?.ClearCache()`，
再 `gitMetadataService.ClearCache(); protoMetadataService.ClearCache();`（两个引用本来就已注入，
不需要 `IServiceProvider`）。

> 命名说明：`ClearCache` 只清"缓存"，不叫 `Refresh`。需要"重新拉取并清缓存"时，由调用方组合
> `IMetadataBundleSource.EnsureLocalBundleAsync` + `ClearCache`（Phase 3 的刷新命令），两者职责分开。

## 7. Proto 读取与映射

### 7.1 解码

```csharp
// Service/Metadata/Proto/MetadataBundleCodec.cs
internal static class MetadataBundleCodec
{
    private const int MaxEncryptedBytes = 64 * 1024 * 1024;   // 单语言包远小于此
    private const int MaxDecompressedBytes = 256 * 1024 * 1024;

    public static MetadataBundle Read(string path, CancellationToken token);
}
```

流程：长度检查 → 读 16 字节 IV → `CryptoStream(AES-CBC/PKCS7, 读)` → `ZLibStream(解压)` →
`BoundedReadStream(max)` → `MetadataBundle.Parser.ParseFrom(stream)`。

- 只用加密布局。`EncryptProtoOutput = false` 的明文包**不支持**；不要用"首字节像不像 zlib"去猜格式，
  猜错会把随机密文当 protobuf 解析，错误信息毫无意义。开发期要测明文包就显式改常量，不进生产代码路径。
- `BoundedReadStream` 防解压炸弹：AES-CBC 无认证，输入来自网络，解压上限必须显式。
- `Google.Protobuf` 的 `MessageParser` + 生成类是 AOT 友好的（应用已用它解析 Yae/Sophon）；
  **不要**用 `JsonFormatter`/`JsonParser` 把消息转 JSON 再交给现有反序列化——那条路反射重、还要先构造
  一份 19.2 MiB 量级的 JSON 字符串，既违背 AOT 也丢掉体积收益。

### 7.2 头部与完整性校验（顺序即失败优先级）

下载阶段（清单）：

1. 清单：`schemaVersion <= MetadataBundleSchemaVersion`（见 §10.1；缺失按 0 处理并警告）。

解码后（bundle）：

2. `bundle.Languages.Count == 1` 且 `Language == locale`。
3. `Version.Major > 0`（否则警告，不致命）。
4. `Info.Publisher` 非空（仅日志）。
5. 类别内容：某类别**首次映射**后至少 1 条；为 0 记警告并按空数组缓存，避免每次访问都重试映射。
6. 覆盖性：30 个已注册适配器必须都能在 `LanguageMetadata` 找到对应 message 字段
   （按 `ProtoName` 匹配）；另外 5 个明确列在 `IgnoredProtoCategories` 的类别跳过；
   出现两者都不属于的未知 message 字段 → 记一次"本版本忽略该类别"。

映射失败策略：

| 情形 | 处理 |
|---|---|
| 单个条目构造抛异常（如必需字段为 null） | **整类别失败**：`HutaoException` 带类别与条目 id，不写入半截缓存 |
| 枚举值未定义（新版本枚举） | 收敛到该枚举的 `NONE/None`，每个 `(类别, 字段, 值)` 只记一次警告，条目保留 |
| id/等级数值溢出（如 `long` → `uint`） | 跳过该条目 + 警告（模型无法表达，保留会得到错误数据） |
| 时间字符串不合 ISO 8601 | 用 `default` + 警告（与 JSON 源 `DateTimeOffset` 默认值语义一致） |
| `BeginTime` 等可选时间为空 | `default` |

### 7.3 适配器与映射规则

```csharp
internal abstract class ProtoCategoryAdapter<TProto, TModel> : IProtoCategoryAdapter
    where TProto : class, IMessage
{
    public Type ElementType => typeof(TModel);
    public abstract IEnumerable<TProto> Enumerate(LanguageMetadata language);
    public abstract TModel Map(TProto message);
    object IProtoCategoryAdapter.Read(LanguageMetadata language) => Enumerate(language).Select(Map).ToImmutableArray();
}
```

- 注册表按 `MetadataCategory.ProtoName` 建 `ImmutableDictionary<string, IProtoCategoryAdapter>`（`Ordinal`）。
  用名字而不是对象引用，避免"两个 `MetadataFileStrategy` 实例相等性不同"导致的查表失败。
  `ProtoName` 默认等于 `Name`，只有 `BydMaterial` 例外：`Name = "BeyondItem"`（JSON 文件名）、
  `ProtoName = "BydMaterial"`（proto 类别名），与 `ProtoBuilder.LegacyJsonFileNames` 对称。
- `IgnoredProtoCategories`：`Furniture` / `FurnitureMake` / `FurnitureSuite` / `FurnitureType` /
  `ReliquaryAffixWeight` 五个消息名，显式声明"本版本不消费"。这样覆盖性测试能区分
  "故意忽略"与"漏注册"，而不是把两者都当成未知字段。
- 一个类别一个 `sealed class`（30 个），每个只写 `Enumerate` + `Map` 两个成员。
  **不做反射泛型映射**（`ProtoMapper` 那种反射只允许留在构建器里）。
- `MetadataConvert` 集中处理下表这些易错点：

| 易错点 | 契约 |
|---|---|
| proto 枚举 → 应用枚举 | 数值直转（`enums.proto` 与 `Model/Intrinsic` 数值一致，已逐项核对 `FightProperty/GrowCurveType/QualityType/ItemType/MaterialType/BodyType/WeaponType/Arkhe/MonsterType/QuestType/ElementType/GachaType`）；越界收敛 |
| proto 用裸 `int32` 的字段 | `Reliquary.rank_level/equip_type/item_type`、`DisplayItem.*`、`ReliquaryMainAffix.type`、`ReliquaryMainAffixLevelProperty.type`、`ReliquarySubAffix.type` → 显式 `(AppEnum)value` + 定义域检查 |
| `long/int` → 应用 `uint` / 标识结构 | `checked((uint)value)`；标识结构用隐式转换（`AvatarId` 等生成代码带 `implicit operator AvatarId(uint)`） |
| 字符串 → `Uri` | `GachaEvent.banner/banner2`：`Uri.TryCreate` 失败置 `null`（`Banner` 必需则整条跳过） |
| 字符串 → `DateTimeOffset` | 构建器用默认 STJ 写 ISO 8601（`"2020-09-28T00:00:00+08:00"`），因此用 `DateTimeOffset.TryParse(InvariantCulture, RoundtripKind)`，**不要**复用 `SimpleDateTimeOffsetConverter`（那是 `yyyy-MM-dd HH:mm:ss`，只用于其他模型） |
| `repeated` → `TypeValueCollection` / `WeaponTypeValueCollection` / `IdLevelParametersCollection` | 先构造 `ImmutableArray<TypeValue<…>>` / `ImmutableArray<WeaponTypeValue>` / `ImmutableArray<IdLevelParameters<…>>`，再调对应构造函数（构造器会建排序字典，保持与 JSON 路径一致的排序/去重语义） |
| `HashSet<uint>` → proto `repeated int64` | `checked((uint)…)`，重复项自然去重 |
| 字符串驻留 | 所有字符串字段 `string.Intern`（JSON 路径经 `InternStringConverter` 已驻留；不驻留会让 proto 源的内存优势消失） |
| `IJsonOnDeserialized` | `Avatar` 是该接口的实现（`VisionOverride` 依赖它）；映射后显式调用 `avatar.OnDeserialized()`，否则 JSON / Proto 两源行为不一致 |

- `Avatar`、`Weapon`、`Monster` 的内嵌曲线字段在 proto 里是 `repeated { type, value }`，应用模型是
  `TypeValueCollection`；`Weapon.grow_curves` 还有 `init_value` → `WeaponTypeValue`。
- 三个曲线类别（`AvatarCurve` / `MonsterCurve` / `WeaponCurve`）→ 应用 `GrowCurve { Level, Curves }`：
  proto 的 `repeated GrowCurvePoint { GrowCurveValue type; double value }` 对应
  `TypeValueCollection<GrowCurveType, float>`（`double → float` 显式收窄），`level` 对应 `Level`。
  这三类是"整表"数据（每个等级一条），不是按角色/武器拆分的。
- `Avatar.SkillDepot.Proud.parameters` 是 `repeated SkillParameter{id, level, parameters}` →
  `IdLevelParametersCollection<ProudSkillId, SkillLevel, float>`。

### 7.4 bundle 生命周期（内存）

- 初始化时解析并保留 L1；类别按需映射，映射结果进 L2。
- 已映射类别数 == `ProtoCategoryTable.Adapters.Count` 时，**释放 L1**（置空并计数），
  稳态只保留 L2/L3；之后 `ClearCache` 需要重新读盘解析。
- 若测量显示"保留 L1"与"释放 L1"的总工作集差异不明显，保持简单（保留），用测量结果决定；
  这个判断点写入 §13 的验收项，不靠猜测。

## 8. 传输与更新

### 8.1 方案选择

| 方案 | 首次启动 | 更新粒度 | 需要服务器/目录变更 | 结论 |
|---|---|---|---|---|
| **HTTP：metadata 端点 / CDN**（复用模板 + `Metadata(locale, file)`） | 1 个文件（CHS ≈ 3.6 MiB） | 清单哈希；未变化不下载 | 只需把产物放进现有 metadata 树 | **推荐** |
| 新 git 仓库 `Snap.Metadata.Hutaobin` | 整仓（15 个语言包 + 59.7 MiB 合并包） | `git fetch`，可能重下多文件 | 需要 catalog 行、`BackgroundActivity`、镜像配置 | 备选；保留 `IMetadataBundleSource` 缝隙 |
| 复用 `Snap.Metadata` 仓库 | 349.9 MiB 工作树 | 同上 | 无 | 否 |

推荐 HTTP 的理由：只需要一个语言包；`RetryHttpHandler` / 代理 / 域名回退 / 限时都已具备；
不引入第二个 git 仓库与 catalog 维护成本。仓库方案留作 `IMetadataBundleSource` 的第二个实现，
`ProtoMetadataService` 不需要改。

### 8.2 `HttpMetadataBundleSource`

```
GET {apiRoot}/metadata/template                     → Response<MetadataTemplate>
GET {template}/Genshin/{locale}/GenshinMetadata.manifest.json   （禁止缓存：Cache-Control: no-cache）
本地 {Data}/Metadata/{locale}/GenshinMetadata.hutaobin 存在且 xxHash64 == 清单 → 直接返回
否则 GET {template}/Genshin/{locale}/GenshinMetadata.hutaobin
  → 写 {path}.tmp（流式 + 进度）
  → size / xxHash64 / schemaVersion / language 校验
  → File.Move(tmp, path, overwrite: true)           ← 原子替换；校验失败删除 tmp，保留旧文件
```

约束：

- **先校验后替换**：与 `CLAUDE.md` §6 "parse-first、事务式替换" 同一条规则。不能用"先覆盖再校验"，
  否则一次网络抖动会让用户失去可用的旧包。
- 超时：`HttpClient.Timeout = InfiniteTimeSpan` 是全局策略，下载必须自带
  `CancellationTokenSource.CancelAfter`（建议 120 s）并链接 `ShutdownToken`。
- 进度：通过计数流回调 `BackgroundActivityOptions.MetadataBundleInitialization`（新增），
  在 UI 上表现为和现在 git clone 一样的后台活动。
- 模板缓存：模板短期内不变，缓存 24 h 于本地设置；失败时退回 `Metadata(locale, file)` 直连形式。
  模板是含 `{0}` 的 `string.Format` 串，格式化时用 `CultureInfo.InvariantCulture`
  （现有 `LegacyMetadataOptions.GetLocalizedRemoteFile` 用的是 `CurrentCulture`，迁移时一并修正）。
- 失败时**不清空**已有 bundle：没有网络但本地有旧包时，Proto 源仍可初始化（离线可用）。
- 只在 `Auto`/`Proto` 下调用；`Git` 模式完全不碰网络下载。
- 开发联调：读取本地设置 `Snap::Hutao::Metadata::BundlePath`（绝对路径）时跳过下载与清单，
  直接从该文件解码；让 Phase 2 不必等发布端就绪就能验证映射。该开关不暴露 UI。

## 9. 源选择与失败矩阵

### 9.1 `MetadataSourceKind` 与设置

```csharp
public enum MetadataSourceKind
{
    Auto = 0,    // 默认：Proto → Git
    Proto = 1,   // 优先 Proto，失败仍允许回退（并提示）
    Git = 2,     // 只用 Git
}
```

- `[Service(..., Key = MetadataSourceKind.Git)]` / `Key = MetadataSourceKind.Proto` 只允许这两个值。
- 设置项：`SettingKeys.MetadataSource = "Snap::Hutao::Metadata::Source"`，
  `MetadataSourceOptions : DbStoreOptions` 暴露 `IObservableProperty<MetadataSourceKind>`；
  门面订阅 `PropertyChanged` → `ClearCache()` → 下次 `InitializeAsync` 重新选源。
  另加仅开发用的 `SettingKeys.MetadataBundlePath`（§8.2），不参与 UI。
- Phase 2/3 先作为**隐藏/开发向设置**（无 UI），避免在发布前冻结 UI 文案；Phase 3 再决定是否进设置页。

### 9.2 失败矩阵

| 设置 | Proto | Git | 结果 | 用户可见 |
|---|---|---|---|---|
| Auto | ✅ | 未尝试 | Proto | 无 |
| Auto | ❌ | ✅ | Git | 无（静默回退；诊断信息进日志/span） |
| Auto | ❌ | ❌ | 未初始化 | 页面原有"元数据未初始化"路径；网络恢复后自动重试 |
| Proto | ✅ | 未尝试 | Proto | 无 |
| Proto | ❌ | ✅ | Git | 一次性警告：已回退到 Git（可在设置中改回 Auto） |
| Proto | ❌ | ❌ | 未初始化 | 同上 + 网络恢复后自动重试 |
| Git | 未尝试 | ✅ | Git | 无 |
| Git | 未尝试 | ❌ | 未初始化 | 同上 + 网络恢复后自动重试 |

"网络恢复后自动重试"由 `INetworkRetryCoordinator` 提供（§5.5）；不依赖用户重新进入页面。

具体失败点与处理：

| 触发 | 检测 | 行为 |
|---|---|---|
| 无网络 / 404 / 5xx（重试后） | HTTP | 保留旧包 → 用旧包；无旧包则回退 |
| 下载中断 / 大小不符 | 清单 size | 删 tmp，保留旧包 |
| 哈希不符 | xxHash64 | 删 tmp，保留旧包，记警告（可能是发布事故） |
| 解密/解压/解析失败 | codec | 同上；异常进日志与 Sentry breadcrumb |
| 解压超上限 | `BoundedReadStream` | 拒绝，按损坏处理 |
| `schemaVersion` 过新 | 清单/头部 | 拒绝；提示升级（复用 `SH.ServiceMetadataVersionNotSupported`）→ 回退 |
| `language` 不符 | 头部 | 拒绝（发布错误），回退 |
| 某类别为空 | 映射 | 警告；该类别空数组缓存；其余类别不受影响 |
| 单条目异常 | 映射 | 该类别失败抛出（带类别/条目 id），不写半截缓存 |
| 磁盘写失败 / 磁盘满 | 原子替换 | 保留旧包，回退，记日志 |

## 10. `MetadataBuilder` 侧契约

对 builder `cb1eb30` 的状态：

| 项 | 状态 |
|---|---|
| 曲线类别（`AvatarCurve` / `MonsterCurve` / `WeaponCurve`） | ✅ 已完成（`89ddba9`，2026-11-21） |
| proto 类别名与 JSON 文件名解耦（`BydMaterial` ↔ `BeyondItem`） | ✅ 已完成（`cb1eb30`，`ProtoBuilder.LegacyJsonFileNames`） |
| `schema_version` | ❌ 仍缺（§10.1） |
| 清单文件（size/hash/schema/categories） | ❌ 仍缺（§10.1） |
| 单语言包写进语言目录 | ❌ 仍缺（§10.3） |
| 缺失类别致命 | ❌ 仍缺（§10.4） |
| 客户端 schema 分发方式 | ❌ 待定（§10.5） |
| 密钥/格式常量 | ⚠️ 两端硬编码（§10.6） |

### 10.1 版本与清单（仍需）

1. `MetadataInfo` 增加 `int32 schema_version = 3;`。
   规则：**新增可选字段不升版本；字段改名/删除/语义变化/字段号变动必须升版本**。客户端
   `MetadataBundleSchemaVersion = 1`，接受 `<= 1`，大于则拒绝并回退。
   过渡期（生产者未发布该字段）`0` 视为"未版本化"，接受但记一次警告。
   注意：本轮补曲线时，`byd_materials` 之后的字段号整体后移（`avatar_promotes` 5→6、
   `byd_materials` 6→7、`materials` 19→20、`weapons` 32→34…）。schema 没有版本，这种变更只能靠
   "客户端和包同时更新"兜底；一旦发布过任何 bundle，就必须先补 `schema_version`。
2. 每个语言目录写 `GenshinMetadata.manifest.json`（PascalCase，与现有 `MetadataOptions.JsonSerializerOptions` 一致）：

```json
{
  "SchemaVersion": 1,
  "GameVersion": { "Major": 7, "Minor": 2, "Patch": 0 },
  "GeneratedAt": "2026-11-21T15:00:24Z",
  "File": "GenshinMetadata.hutaobin",
  "Size": 3771744,
  "XxHash64": "0123456789ABCDEF",
  "Categories": ["Achievement", "AchievementGoal", "Avatar", "AvatarCurve", "..."]
}
```

   客户端据此在下载 3.6 MiB 之前判断"是否变化 + 是否能读"。清单本身很小，每次初始化取一次。

### 10.2 曲线类别（已完成 2026-11-21）

实际落地的 schema（以代码为准）：

```proto
// common.proto —— 三个曲线类别共享
message GrowCurvePoint {
  GrowCurveValue type = 1;
  double value = 2;
}

// avatar_curve.proto / monster_curve.proto / weapon_curve.proto
message AvatarCurve {                 // MonsterCurve / WeaponCurve 同构
  repeated GrowCurvePoint curves = 1;
  int32 level = 2;
}

// bundle.proto —— LanguageMetadata
repeated AvatarCurve avatar_curves = 5;
repeated MonsterCurve monster_curves = 22;
repeated WeaponCurve weapon_curves = 35;
```

- 数据源是 `AvatarCurveExcelConfigData.json` / `MonsterCurveExcelConfigData.json` /
  `WeaponCurveExcelConfigData.json`，由新增的 `AvatarCurveBuilder` / `MonsterCurveBuilder` /
  `WeaponCurveBuilder` 生成 JSON 后再进入 bundle（不依赖 `ProtoBuilder` 的"从 JSON 树回读"分支）。
- 三个 builder 已加入 `MetadataBuilder.BuildAll` 的启用列表，且排在 `MetaBuilder` 之前，
  因此 `Meta.json` 现在也包含这三个类别的哈希。
- 输出文件名仍是 `AvatarCurve.json` / `MonsterCurve.json` / `WeaponCurve.json`，
  Git 源的 `MetadataFileStrategy` 与读取路径完全不变，两源可做逐条平价。
- 曲线值在 builder 模型、JSON 与 proto 里都是 `double`；应用模型是 `float`，映射时显式收窄到
  `TypeValueCollection<GrowCurveType, float>`（与现有 JSON 反序列化路径的精度损失一致，见 §7.3）。
- `GrowCurvePoint.type` 用 `GrowCurveValue` 枚举（不是 `FightProperty`）；与应用 `GrowCurveType`
  数值一致，可直接数值转换。
- 类别改名映射（builder 的 `LegacyJsonFileNames` 与客户端 `MetadataCategory.ProtoName`）目前各存一份；
  以后再有改名必须同时改，并在覆盖性测试里断言 `BydMaterial ↔ BeyondItem` 这组对应关系。

### 10.3 单语言包放进语言目录（仍需，为了 HTTP 布局）

- 产物写到 `{OutputPath}/{locale}/GenshinMetadata.hutaobin`（而非只在平铺的 `ProtoOutputDirectory`）。
  平铺目录与合并包可以保留给工具/离线用途。
- 这样 URL 与本地缓存路径同构：`Genshin/{locale}/GenshinMetadata.hutaobin`，
  清单与包在同一个目录，排查时不需要解释"为什么路径里有两次 locale"。

### 10.4 类别缺失必须致命（仍需，建议）

`ProtoBuilder.OnFinalize` 现在对缺失类别只 `Console.WriteLine` 后继续发布；客户端无法区分
"这个类别本来就空"与"构建器漏跑"。发布模式应直接 **build fail**，或在 `MetadataInfo` 增加
`repeated string missing_categories` 供客户端拒绝。否则 §7.2 的第 5 条校验只能降级成警告。

### 10.5 schema 分发（仍需，二选一）

- **推荐**：从所有 `.proto` 删除 `option csharp_namespace`（包名 `snap.hutao.metadata` 会派生
  `Snap.Hutao.Metadata`），客户端把 `Proto/*.proto` 原样 vendor 进应用仓库，构建时用 `Grpc.Tools` 生成。
  两端同名，不需要改写。
- 备选：客户端只提交生成好的 `.cs`（先例：`Web/Hoyolab/Downloader/Proto/SophonManifestProto.cs`），
  schema 变更时手工重新生成；需要一条"生成文件头记录 protoc 版本 + 源提交"的约定。

推荐第一种的理由：schema 只有一份，构建产物不手改，符合 `CLAUDE.md` "Generated code is generated"。
代价是新增 `Grpc.Tools`（`PrivateAssets=all`，集中版本管理）与还原时的一次 protoc 下载。

### 10.6 密钥与格式常量

`AesKeyHex` / 布局注释目前只写在 `ProtoBuilder` 里，且注释写着"keep in sync with Snap.Hutao.Remastered"。
客户端落地时应由 codec 测试断言两端常量一致（测试里放同一组常量并做一次 round-trip）；
更彻底的做法是生产者把密钥/格式版本改成可配置并写入 `MetadataInfo.format_version`，为将来轮换取留出空间。

## 11. 消费方影响与符号变更

### 11.1 消费方

27 处注入（18 个 ViewModel、3 个 `*ScopeContext`、1 个 Window、5 个 Service，其中 2 个是 UIGF 导出抽象基类）
**不改代码**：它们只依赖 `IMetadataService`、`InitializeAsync()` 和 `GetContextAsync<TContext>()`。
唯一行为变化是 `InitializeAsync()` 可能自行触发初始化（幂等、快路径）。

### 11.2 符号变更表

| 现状 | 目标 | 备注 |
|---|---|---|
| `IMetadataService` | `IMetadataService` | 语义扩展，不改名（避免 27 处改动） |
| `MetadataService` | `GitMetadataService` | 用户明确要求 |
| `MetadataOptions` | `GitMetadataOptions` | git 路径 / 仓库名 |
| `MetadataFileStrategy` | `MetadataCategory` | 可选；"文件"对 proto 不成立；新增 `ProtoName`（默认 = `Name`，`BydMaterial` ↔ `BeyondItem`） |
| `MetadataFileStrategies` | `MetadataCategories` | 30 个类别常量 |
| `FromCacheOrFileAsync<T>` | `FromCacheOrSourceAsync<T>` | 两个扩展文件（26 + 28 个方法）机械替换 |
| `IMetadataService.MemoryCache` | `IMetadataService.Cache`（`MetadataCache`） | 唯一使用点在字典扩展 |
| `LegacyMetadataService` / `LegacyMetadataOptions` | 删除 | ENG-08；其模板/远端 URL 逻辑迁入 `HttpMetadataBundleSource` |
| —（新增） | `MetadataSourceKind` / `MetadataSourceOptions` / `MetadataService` 门面 / `MetadataServiceBase` / `ProtoMetadataService` / `MetadataBundleCodec` / `ProtoCategoryTable` / `MetadataCache` / `IMetadataBundleSource` | 见 §5.1 |

### 11.3 新增文案（只改 `SH.resx`）

| key | 用途 |
|---|---|
| `ServiceMetadataInitializationFailed` | 初始化失败（`NetworkRetryCoordinator.MarkPending` 的提示；离线时由网络提示接管） |
| `ServiceMetadataSourceFallback` | 显式 Proto 偏好回退到 Git 后的一次性警告 |
| `ServiceMetadataBundleDownloadFailed` | 清单/包下载失败（带原因占位符） |
| `ServiceMetadataBundleInvalid` | 校验失败（大小/哈希/语言/解析） |
| `ServiceBackgroundActivityMetadataBundleInitialization`（+ `…Description`） | 后台活动：下载/校验 bundle |

`ServiceMetadataNotInitialized`、`ServiceMetadataFileNotFound`、`ServiceMetadataVersionNotSupported`
已存在，直接复用；其余语言文件不在同一 PR 修改（`CLAUDE.md` §5.7）。

## 12. 分阶段实施

### Phase 0：解码器与 schema 接入（无行为变化）

- 新增 `MetadataBundleCodec`、`MetadataBundleInfo`、`BoundedReadStream`、常量（`MetadataBundleSchemaVersion`）。
- 按 §10.5 接入 schema 生成（vendored `Proto/` + `Grpc.Tools`，或提交生成 `.cs`）。
- 测试项目新增 `Google.Protobuf` 引用 + 链接 codec；加"加密→解码"round-trip 测试（小合成消息）。

验收：`dotnet build` / `dotnet test` 通过；不改变任何现有读取路径。

### Phase 1：拆分、改名、缓存抽象（仍只用 Git）

- `MetadataService` → `GitMetadataService`；`MetadataOptions` → `GitMetadataOptions`；
  `MetadataServiceBase`；`MetadataCache`；门面 `MetadataService`（此时只注入 Git，Proto 字段在 Phase 2 加）。
- `MetadataCategory`/`MetadataCategories`（可延后）、`FromCacheOrSourceAsync`、`Cache` 替换 `MemoryCache`。
- 删除 `LegacyMetadataService` / `LegacyMetadataOptions`。
- `InitializeAsync` 改为自启动；`ClearCache` 落地（Git 源）。

验收：行为与现状一致；`ValidateOnBuild` 通过；手动验证 5 个页面；`ClearCache` 后页面重新读取成功。

### Phase 2：Proto 读取器

- `ProtoMetadataService`、`ProtoCategoryTable` + 30 个适配器、`MetadataBundleOptions`、
  `HttpMetadataBundleSource`、`MetadataBundleInitialization` 后台活动 + 文案。
- keyed `Proto` 注册；门面按 `Auto` 优先 Proto。
- 映射层开发不依赖 §10.1/§10.3：用 §8.2 的 `Snap::Hutao::Metadata::BundlePath` 本地开关，
  直接读当前 builder 产出的 `GenshinMetadata.CHS.hutaobin` 即可联调；但**上线** Proto 仍需
  §10.1（版本 + 清单）与 §10.3（语言目录布局）。
- `MetadataBuilder` 侧 §10.2 已完成；剩余契约见 §10 状态表。

验收：`Snap.Metadata` 仓库**不被 clone**（删除本地仓库后启动验证）；30 个类别全部读取成功；
数量与 JSON 源一致；离线（本地已有包）可启动。

### Phase 3：切换、回退、刷新与清理

- `MetadataSourceKind` 设置项 + 门面回退 + 回退警告。
- "重新加载元数据"命令（`SettingStorageViewModel` 或新的元数据卡片）：
  `EnsureLocalBundleAsync` → `ClearCache`。
- 诊断信息（`BundleInfo` 版本/生成时间）暴露给设置页（可选）。

验收：三种设置组合行为符合 §9.2；刷新命令后页面数据更新；回退提示只出现一次。

### Phase 4：验证与文档

- 性能/内存测量、Debug 平价校验（§13）、`CLAUDE.md` §5.5 更新、`CODE_REVIEW.md` §14 追加一行
  （并标记 ENG-08 已解决）。

## 13. 验证方案

| 项 | 方法 | 通过标准 |
|---|---|---|
| 构建 / DI | `dotnet build src/Snap.Hutao.Remastered/Snap.Hutao.Remastered.slnx -c Debug -p:Platform=x64` | `ValidateOnBuild` 无异常；两个 keyed + 一个非 keyed 注册齐全 |
| codec | 测试项目：构造小 `MetadataBundle` → 加密（同常量）→ `Read` | 字段 round-trip；截断/超限/坏 IV 三种负例抛明确异常 |
| 类别覆盖 | 测试：`LanguageMetadata.Descriptor` 的 35 个 message 字段 == 30 个适配器的 `ProtoName` + 5 个 `IgnoredProtoCategories` | 无遗漏、无重复、无未知字段 |
| 转换辅助 | 测试：枚举越界、`long→uint` 溢出、ISO 时间、`TypeValueCollection` 构造、`double→float` 曲线收窄 | 行为符合 §7.2/§7.3 表 |
| 缓存 | 测试：`MetadataCache.Clear` 与并发 `SetIfCurrent` | 清理后不残留旧键；旧代际结果不入缓存 |
| 选源/回退 | 断网 + 无本地包启动（Auto / Proto / Git 三种设置） | 行为符合 §9.2；Auto 静默、Proto 只提示一次、Git 不回退 |
| 网络重试 | 断网启动 → 恢复网络 | `NetworkRetryCoordinator` 重跑初始化并完成 TCS；页面无需重进 |
| 平价 | Debug 本地开关：同时初始化 Git 与 Proto，逐类别比较条数与稳定投影（id + 关键字段） | 30 个类别 0 差异；差异日志带类别与首个不同 id |
| 手动 | Wiki 角色/武器/怪物、成就、日历、深渊、幻想真境、幽境危战、培养、背包、抽卡记录、UIGF 导出 | 数据完整、无异常、切换来源后一致 |
| 性能 | Sentry span：`metadata.bundle.ensure`、`metadata.bundle.parse`、`metadata.category.map` | 初始化在 3 s 内（3.6 MiB + 解析）；首次类别 < 300 ms（以实机测量为准） |
| 内存 | 任务管理器/`GC.GetTotalMemory` 对比两源稳态 | 记录数值并决定 §7.4 是否保留 L1 |

测试项目限制（`CODE_REVIEW.md#BUG-02`）：只能链接已加入 `<Compile Include>` 的生产文件，且
`net11.0` 不能引用 WinUI。因此 codec/registry/缓存/转换辅助可测；**依赖 `IAdvancedCollectionView` 等
WinUI 类型的模型（`Avatar`、`Weapon` 等）的完整映射只能靠平价校验与手动验证**，不要假装单测覆盖了它们。

## 14. 风险与未决问题

| 风险 | 影响 | 缓解 |
|---|---|---|
| 30 个适配器 = 长期维护面 | schema 变更时容易漏改 | 覆盖性测试 + Debug 平价校验 + §10.1 版本规则 |
| 枚举未定义值 | 显示错误或抛异常 | 收敛 + 一次性警告；不因单个枚举丢条目 |
| AES-CBC 无认证 | 恶意包可触发解析/内存攻击 | HTTPS + 清单哈希 + 尺寸/解压上限 + 解析后结构校验 |
| 解析内存峰值 | 3.6 MiB 包解析后对象图可能数十 MiB | §7.4 测量后决定是否提前释放 L1 |
| schema 无版本、字段号已整体后移 | 旧客户端按字段号解析新包会静默错位 | 发布前补 §10.1；解码后仍按语言/类别名/条目数做结构校验 |
| `Grpc.Tools` 构建依赖 | 新增原生 protoc 与构建时间 | 版本集中管理；CI 缓存 NuGet；失败时回退到提交生成 `.cs` |
| 双源行为漂移 | 同一页面两源数据不一致 | 平价校验；以 JSON 源为基准，Proto 修复对齐 |
| 设置项默认 `Auto` | 发布端尚未就绪时首启回退一次 | 回退只在无本地包时发生；提示一次性；可在设置中固定 Git |

未决问题：

1. 单语言包最终托管在 metadata CDN 还是新 git 仓库？（本文推荐前者；`IMetadataBundleSource` 已隔离该决定）
2. 是否保留 Git 源长期作为回退，还是在某个版本后下线 JSON 路径（影响 `MetadataFileJsonContext` 与 19.2 MiB JSON 维护成本）。
3. "重新加载元数据"放在设置页何处、是否需要取消正在进行的初始化。
4. `schema_version` 的所有权与发布流程：由谁在 `MetadataBuilder` 里决定升版本（建议：改 proto 的 PR 必须动版本并在 PR 描述写原因）。
5. 平价校验是否长期保留在 Debug（建议保留，成本低、收益高）。

## 附录 A：文件清单（目标结构）

```
Service/Metadata/
  IMetadataService.cs                  (改：+Cache +ClearCache +FromCacheOrSourceAsync)
  IMetadataServiceInitialization.cs    (不变)
  MetadataServiceBase.cs               (新)
  MetadataService.cs                   (门面，重写)
  MetadataCategory.cs                  (原 MetadataFileStrategy)
  MetadataCategories.cs                (原 MetadataFileStrategies)
  MetadataCache.cs                     (新)
  MetadataSourceKind.cs                (新)
  MetadataSourceOptions.cs             (新)
  Git/
    IGitMetadataService.cs             (新)
    GitMetadataService.cs              (原 MetadataService)
    GitMetadataOptions.cs              (原 MetadataOptions)
  Proto/
    IProtoMetadataService.cs           (新)
    ProtoMetadataService.cs            (新)
    MetadataBundleOptions.cs           (新)
    MetadataBundleInfo.cs              (新)
    MetadataBundleCodec.cs             (新)
    BoundedReadStream.cs               (新)
    IMetadataBundleSource.cs           (新)
    HttpMetadataBundleSource.cs        (新)
    ProtoCategoryTable.cs              (新)
    ProtoCategoryAdapter.cs            (新)
    MetadataConvert.cs                 (新)
    Mapper/*.cs                        (新：30 个类别适配器)
  ContextAbstraction/…                 (不变)
  MetadataServiceImmutable*Extension.cs (改：键走 Cache.CreateKey，方法名 FromCacheOrSourceAsync)
Proto/                                  (新：vendored schema，§10.5)
  *.proto                               (从 MetadataBuilder/Proto 原样复制，38 个文件)
```

## 附录 B：与现有文档的衔接

- 实现落地时更新 `CLAUDE.md` §5.5：两个 keyed 源 + 门面、`ClearCache` 语义、bundle 格式与来源。
- `CODE_REVIEW.md` §14 追加一行；ENG-08（`LegacyMetadataService`）随 Phase 1 关闭。
- 若 Phase 2 引入 `Grpc.Tools`，`Directory.Packages.props` 与 `CLAUDE.md` §2 的构建说明需要同步。

## 附录 C：本文变更记录

| 日期 | 变更 |
|---|---|
| 2026-11-22 | 同步 builder `cb1eb30`：曲线类别（`89ddba9`）已补齐，`LanguageMetadata` 变为 35 个字段、应用覆盖 30/30；新增 `GrowCurvePoint` 与三个曲线类别映射、`MetadataCategory.ProtoName`（`BydMaterial` ↔ `BeyondItem`，对应 `ProtoBuilder.LegacyJsonFileNames`）；更新包体积（CHS 3.60 MiB、15 语言 3.60–4.73 MiB、合并 59.7 MiB）与 JSON 树实测（19.2 MiB / 349.9 MiB）；§10.2 标记完成，`schema_version`、清单、语言目录、缺失类别致命仍待做；实施计划与验收改为 30 个适配器。 |
| 2026-11-21 | 初稿：现状、目标数据流、keyed 双源 + 门面、缓存与 `ClearCache`、传输方案、映射规则、分阶段计划。 |
