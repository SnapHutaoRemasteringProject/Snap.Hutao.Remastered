# 元数据服务重构：IMetadataProvider / IMetadataService 两层设计

> 状态：**已实现（2026-11-22）**。`IMetadataProvider` / `IMetadataService` 两层、keyed Git/Proto provider、
> `MetadataCache` 与 `ClearCache`、proto bundle 解码与 30 个类别适配器、HTTP bundle 源、选源/回退/切换/
> 网络重试均已落地；**例外**：缓存的新鲜度/远端更新检测尚未实现（本地包存在即信任，见 §8.3）。
> 与本文设计的差异、验证结果和仍是 builder 侧的待办见 §15。
> 本文其余部分保留为设计依据；"应用"原始事实来自 `feat/aot-compatible` @ `b08006116`，"产物"事实来自
> `D:\SnapHutaoRemasteringProject\MetadataBuilder` @ `cb1eb30` 的 `Proto/`、`Core/Builder/ProtoBuilder.cs`
> 与 `Core/Builder/*CurveBuilder.cs`。
>
> **2026-11-22 更新（一）**：产物已补齐 `AvatarCurve` / `MonsterCurve` / `WeaponCurve`（builder `89ddba9`）并引入
> `LegacyJsonFileNames`（builder `cb1eb30`），Proto 的类别覆盖达到 30/30；本文已同步覆盖矩阵、字段数、
> 映射规则与实施计划。仍缺 `schema_version`、清单文件、语言目录输出与"缺失类别致命"（见 §10）。
>
> **2026-11-22 更新（二）**：按"服务管理 provider"重新分层——新增 `IMetadataProvider` /
> `IMetadataProviderFactory`，原 `GitMetadataService` / `ProtoMetadataService` 改名为
> `GitMetadataProvider` / `ProtoMetadataProvider`，`MetadataServiceBase` 改名为 `MetadataProviderBase`；
> keyed 注册落在 `IMetadataProvider` 上，`IMetadataService` 负责创建、选源、`SwitchAsync`、清理广播
> 与网络重试（§5）。
>
> **2026-11-22 更新（三）**：**缓存机制还没有完全实现好**——L2/L3 内存缓存与 `ClearCache` 已可用，但 L0
> 磁盘 bundle 的"新鲜度"层仍是空的：本地包存在即直接信任，**当前无法侦测远程服务器的更新**，服务端发布
> 新包不会到达已安装的客户端（除非删除本地包或使用开发覆盖路径）。后续计划用服务器返回的 **`ETag`** +
> `If-None-Match` 条件请求判断是否更新（§8.3）。
>
> 关联：`CODE_REVIEW.md#ENG-08`（`LegacyMetadataService` 死代码）、`CODE_REVIEW.md#ARCH-01`（避免新增 `IServiceProvider` 服务定位）、
> `CLAUDE.md` §5.5（元数据初始化）、§5.9（Sentry span）、§6（parse-first / 事务式替换）。

## 结论摘要（TL;DR）

1. 现有 `MetadataService`（约 27 处注入、40 次按类别读取）**改名为 `GitMetadataProvider`**，
   保留 JSON + git 仓库路径，成为 `IMetadataProvider` 的一个实现（keyed `Git`）。
2. 新增 **`ProtoMetadataProvider`**（keyed `Proto`）读取 `MetadataBuilder` 产出的
   `GenshinMetadata.{locale}.hutaobin`（`IV ‖ AES-256-CBC(zlib(protobuf))`），单文件替代 158 个 JSON 文件。
3. **`IMetadataService` 是 provider 的管理者**：通过 `IMetadataProviderFactory` 创建/取得 provider，
   负责选源、回退、`SwitchAsync` 切换、`ClearCache` 广播与网络重试；27 个消费方只注入它，
   看不到 provider，也不需要改代码。
4. 数据流从"页面按需读 JSON 文件"改为"**启动时确保并解析 bundle，页面按需映射类别**"；
   `InitializeAsync` 变为**可自启动、幂等**，不再依赖 `AppActivation` 的调用顺序。
5. 新增 `MetadataCache`（带来源/语言前缀 + 代际号）与 **`ClearCache()`** 契约：
   清派生缓存、释放解析后的 bundle、代际 +1，保证清理与并发加载不会互相污染。
6. 二进制包已覆盖全部 30 个应用类别（schema 共 35 个 message 字段，5 个暂不消费）。
   仍缺的是**版本与分发契约**：`MetadataInfo` 没有 `schema_version`、没有清单文件、单语言包不在语言目录、
   缺失类别不致命（见 §10）。这些不阻塞映射层开发，但阻塞把 Proto 设为默认源。
7. **缓存机制还没有完全实现好**：内存侧的 L2/L3 与 `ClearCache` 已可用，但磁盘侧没有新鲜度检查——
   **当前无法侦测远程服务器的更新**。后续用服务器返回的 **`ETag`** 配 `If-None-Match` 条件请求判断
   是否更新（`304` 复用本地包，`200` 才下载并原子替换），详见 §8.3。

## 1. 为什么要改（本机实测数据，7.2 版本）

| 指标 | JSON / git（现状） | proto bundle（新） |
|---|---|---|
| 单语言文件数 | 158 个（CHS） | 1 个 |
| 单语言体积 | 19.2 MiB（CHS） | 3.60 MiB（CHS） |
| 全仓库工作树 | 349.9 MiB / 2,378 文件 | — |
| 拉取方式 | 整仓 `git` shallow clone + fetch（含 `.git`，本机 388 MiB） | 单文件下载（约 3.6 MiB；条件请求/ETag 尚未实现，见 §8.3） |
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
  └─ IMetadataService.InitializeInternalAsync()                      [管理者 MetadataService]
        ├─ MetadataSourceOptions.Source（默认 Auto）
        ├─ 候选序列：Auto/Proto → [Proto, Git]；Git → [Git]
        ├─ IMetadataProviderFactory.GetOrCreate(kind)                 ← keyed provider，首次解析时创建
        └─ IMetadataProvider.InitializeAsync(token)
              ├─ ProtoMetadataProvider
              │     ├─ IMetadataBundleSource.EnsureLocalBundleAsync
              │     │     ├─ GET {api}/metadata/template                    （可用本地缓存 24h）
              │     │     ├─ GET {template}/Genshin/{locale}/GenshinMetadata.manifest.json
              │     │     ├─ 清单哈希 == 本地文件哈希 → 跳过下载
              │     │     ├─ 否则 GET …/GenshinMetadata.hutaobin（流式、限时）
              │     │     ├─ size + xxHash64 + schema + language 校验
              │     │     └─ 写 .tmp → 原子替换 {Data}/Metadata/{locale}/GenshinMetadata.hutaobin
              │     ├─ MetadataBundleCodec.Read → 解析 + 头部校验
              │     └─ 保留 LanguageMetadata（**不映射类别**）
              └─ GitMetadataProvider → clone/fetch Snap.Metadata
        ├─ 成功 → active = 选中的 provider；TCS 完成
        ├─ 失败 → 尝试下一个候选；显式 Proto 回退时提示一次
        └─ 全部失败 → 向 INetworkRetryCoordinator 注册/标记 pending；网络恢复后重跑本流程
                        （TCS 保持 pending，等待重试成功或 ShutdownToken）

页面加载
  └─ metadataService.InitializeAsync()          ← 快路径；未启动时**自行启动**初始化
  └─ GetContextAsync<TContext>()                ← 42 个分支 / 40 次按类别读取
        └─ IMetadataService.FromCacheOrSourceAsync<T>(category)     ← 转发给 active provider
              ├─ [L2] MetadataCache 命中 → 返回（boxed ImmutableArray<T> 解箱）
              └─ [L2] 未命中
                    ├─ Proto：ProtoCategoryTable[category].Read(language) → 逐条映射
                    └─ Git ：读 {locale}/{Name}.json 或 {Name}/*.json → JsonSerialize
                    → MetadataCache.SetIfCurrent(generation, key, value)

切换来源（设置变更 / 手动重试）
  └─ IMetadataService.SwitchAsync(kind)
        ├─ 旧 active.ClearCache()（先清旧缓存，让在途结果因代际失效）
        ├─ active = null；attempt = null；重建 TCS
        └─ 按 kind 重跑候选序列（Proto 仍允许回退 Git，Git 不回退）

清缓存 / 手动刷新
  └─ IMetadataService.ClearCache()              ← 广播给已创建的所有 provider
  └─ （可选）IMetadataBundleSource 重新确保文件 → 下次读取重新解析
```

与现状的关键差异：

1. **初始化不再依赖调用顺序**：`InitializeAsync()` 若发现初始化未启动会自己启动；`AppActivation` 的
   `SafeForget` 仍是后台预热入口，页面不再可能因为"没人调用 `InitializeInternalAsync`"而永久等待。
   启动失败改为登记到 `INetworkRetryCoordinator`（与 `RepositoryCatalogService` 同一套模式），
   网络恢复后自动重跑，而不是像现在这样只能等下一次启动。
2. **解析与映射分离**：bundle 只在初始化时解析一次；类别映射推迟到第一次访问该类别，
   单页只付出它真正需要的类别成本。
3. **职责分层**：`IMetadataProvider` 只管"把某一来源的数据准备好并读出来"；
   `IMetadataService` 只管"创建/持有 provider、按偏好选源、回退、切换、广播清理"。
   消费方（27 处）永远只注入 `IMetadataService`，看不到 provider。
4. **缓存可失效**：所有派生数据都进 `MetadataCache`，`ClearCache` 一次清干净且不误删其他服务的缓存。

## 5. 类型与职责

**为什么拆成两层**：现有 `IMetadataService` 同时承担"某个数据源的实现"和"消费方入口"两种职责，
导致新增来源要不断改名/加子接口，且切换、回退、重试与读取逻辑挤在同一个类里。拆开后：

- `IMetadataProvider` 是**可替换的数据源**：Git / Proto 各自独立实现、独立测试，只关心"把数据准备好并读出来"；
- `IMetadataService` 是**管理者**：创建/持有 provider、按偏好选源、回退、切换、广播清理、网络重试；
- 消费方（27 处）只看到 `IMetadataService`，与"这次用的是哪个源"解耦。

上一版设计（单接口 + keyed 两个实现 + 门面）也能工作，但"来源"和"服务入口"都叫 `*Service`，
命名与职责都不清晰；本版以 provider 命名数据源后，keyed 注册、诊断接口和失败策略都各自归位。

### 5.1 类型表

| 类型 | 种类 | 职责 |
|---|---|---|
| `IMetadataProvider` | 契约（新增） | **数据源**：`Kind` / `IsInitialized` / `Cache` / `InitializeAsync` / `FromCacheOrSourceAsync<T>` / `ClearCache` |
| `IGitMetadataProvider : IMetadataProvider` | 契约 | keyed `Git`；暴露 `RepositoryDirectory` 便于诊断 |
| `IProtoMetadataProvider : IMetadataProvider` | 契约 | keyed `Proto`；暴露 `BundleInfo` / `BundleFilePath` |
| `IMetadataProviderFactory` | 契约（新增） | `AvailableKinds` / `GetOrCreate(kind)`；唯一把 `MetadataSourceKind` 映射到 keyed provider 的地方 |
| `MetadataProviderFactory` | 实现 | 通过 `IServiceProvider.GetRequiredKeyedService<IMetadataProvider>(kind)` 取单例（仅此一处服务定位） |
| `MetadataProviderBase` | 抽象基类（不注册） | 单 provider 的初始化闸门 + logger + `MetadataCache` 实例 |
| `GitMetadataProvider` | 实现（原 `MetadataService`） | JSON + git 仓库读取 |
| `ProtoMetadataProvider` | 实现（新增） | bundle 确保/解析/类别映射 |
| `IMetadataService` | 契约（消费方） | **管理者**：`InitializeAsync` / `SwitchAsync` / `ActiveSource` / `AvailableSources` / 转发读取与 `ClearCache` |
| `IMetadataServiceInitialization` | 契约（不改名） | `InitializeInternalAsync(CancellationToken)`，启动入口 |
| `MetadataService` | 实现（非 keyed 单例） | 创建/持有 provider、选源、回退、切换、网络重试、`ClearCache` 广播 |
| `MetadataCategory` / `MetadataCategories` | 值类型 + 静态表 | 原 `MetadataFileStrategy` / `MetadataFileStrategies`；新增 `ProtoName`（默认 = `Name`，`BydMaterial` 例外） |
| `MetadataCache` | sealed | 前缀 + 代际 + 键跟踪 + `Clear()` |
| `MetadataSourceKind` | enum | `Auto/Git/Proto`；**`Auto` 永不作为 DI key，也不是 provider 的 `Kind`** |
| `MetadataSourceOptions` | 选项 | 持久化来源偏好，`IObservableProperty<MetadataSourceKind>` |
| `GitMetadataProviderOptions` | 选项 | git 仓库名（`Snap.Metadata`）与本地 JSON 路径 |
| `MetadataBundleOptions` | 选项 | bundle 文件名、本地目录、schema 上限、下载超时 |
| `IMetadataBundleSource` | 契约 | 确保本地 bundle 存在并返回路径（仅 Proto provider 使用） |
| `HttpMetadataBundleSource` | 实现 | 模板端点 + 清单 + 条件下载 + 原子替换 |
| `MetadataBundleCodec` | static | 解密/解压/解析 + 头部校验 + 尺寸上限 |
| `MetadataBundleInfo` | 值对象 | 来源、游戏版本、生成时间、schema 版本、语言、哈希 |
| `ProtoCategoryTable` | static | `MetadataCategory` → 适配器；覆盖性校验 |
| `ProtoCategoryAdapter<TProto, TModel>` | 抽象 | 枚举某类别的 proto 条目并映射成 `ImmutableArray<TModel>` |
| `MetadataConvert` | static | 枚举/整数/时间/集合的集中转换与失败策略 |

### 5.2 契约

```csharp
// Service/Metadata/Provider/IMetadataProvider.cs
public interface IMetadataProvider
{
    MetadataSourceKind Kind { get; }              // Git / Proto，绝不为 Auto

    bool IsInitialized { get; }

    MetadataCache Cache { get; }

    ValueTask<bool> InitializeAsync(CancellationToken token = default);

    ValueTask<ImmutableArray<T>> FromCacheOrSourceAsync<T>(MetadataCategory category, CancellationToken token = default)
        where T : class;

    void ClearCache();
}
```

```csharp
// Service/Metadata/Provider/Git/IGitMetadataProvider.cs
public interface IGitMetadataProvider : IMetadataProvider
{
    string RepositoryDirectory { get; }
}

// Service/Metadata/Provider/Proto/IProtoMetadataProvider.cs
public interface IProtoMetadataProvider : IMetadataProvider
{
    MetadataBundleInfo? BundleInfo { get; }

    string BundleFilePath { get; }
}
```

```csharp
// Service/Metadata/Provider/IMetadataProviderFactory.cs
public interface IMetadataProviderFactory
{
    ImmutableArray<MetadataSourceKind> AvailableKinds { get; }

    IMetadataProvider GetOrCreate(MetadataSourceKind kind);
}
```

```csharp
// Service/Metadata/IMetadataService.cs（管理者，不继承 IMetadataProvider）
public interface IMetadataService : IMetadataServiceInitialization
{
    bool IsInitialized { get; }

    MetadataSourceKind ActiveSource { get; }          // 未激活时返回 Auto

    IReadOnlyList<MetadataSourceKind> AvailableSources { get; }

    MetadataCache Cache { get; }                      // 转发 active.Cache

    ValueTask<bool> InitializeAsync();

    ValueTask<bool> SwitchAsync(MetadataSourceKind kind, CancellationToken token = default);

    ValueTask<ImmutableArray<T>> FromCacheOrSourceAsync<T>(MetadataCategory category, CancellationToken token = default)
        where T : class;

    void ClearCache();
}
```

- **不继承**：`IMetadataService` 是 provider 的管理者，把 `IsInitialized` / `Cache` / `FromCacheOrSourceAsync` /
  `ClearCache` 显式转发给 active provider。这样 27 个消费方只看得到一个"元数据服务"，provider 的类型不会泄漏到页面层；
  代价是 4 个转发成员（用编译期签名保证两边一致，不需要共享基接口）。
- `SwitchAsync` 只负责"激活"，**不写设置**；持久化由 `MetadataSourceOptions` 的调用方负责。
  `SwitchAsync(Auto)` 表示恢复自动选择（等价于重新按 `[Proto, Git]` 选源）；
  `Auto` 只作为偏好值，绝不能作为 provider 的 `Kind` 或 DI key。
- `AvailableSources` 来自 `IMetadataProviderFactory.AvailableKinds`，用于设置页/诊断显示"当前构建能用哪些源"。

`FromCacheOrFileAsync` → `FromCacheOrSourceAsync`：proto 源没有"文件"这个概念，继续叫 File 会误导；
改动只落在两个扩展文件（26 + 28 个方法）+ 实现，消费方（27 处）不受影响。

### 5.3 DI 注册（全部由生成器产出，不手写）

```csharp
// provider：keyed singleton，key 是 MetadataSourceKind
[Service(ServiceLifetime.Singleton, typeof(IMetadataProvider), Key = MetadataSourceKind.Git)]
public sealed partial class GitMetadataProvider : MetadataProviderBase, IGitMetadataProvider
{
    [GeneratedConstructor(CallBaseConstructor = true)]
    public partial GitMetadataProvider(IServiceProvider serviceProvider);
}

[Service(ServiceLifetime.Singleton, typeof(IMetadataProvider), Key = MetadataSourceKind.Proto)]
public sealed partial class ProtoMetadataProvider : MetadataProviderBase, IProtoMetadataProvider
{
    [GeneratedConstructor(CallBaseConstructor = true)]
    public partial ProtoMetadataProvider(IServiceProvider serviceProvider);
}

// 工厂：服务通过它"创建/取得" provider，是唯一的 keyed 解析点
[Service(ServiceLifetime.Singleton, typeof(IMetadataProviderFactory))]
internal sealed partial class MetadataProviderFactory : IMetadataProviderFactory
{
    private readonly IServiceProvider serviceProvider;

    [GeneratedConstructor]
    public partial MetadataProviderFactory(IServiceProvider serviceProvider);

    // Phase 1 只有 Git；Phase 2 起为 { Git, Proto }
    public ImmutableArray<MetadataSourceKind> AvailableKinds => [MetadataSourceKind.Git, MetadataSourceKind.Proto];

    public IMetadataProvider GetOrCreate(MetadataSourceKind kind) => kind switch
    {
        MetadataSourceKind.Git or MetadataSourceKind.Proto
            => serviceProvider.GetRequiredKeyedService<IMetadataProvider>(kind),
        _ => throw HutaoException.Argument("Auto is not a provider kind", nameof(kind)),
    };
}

// 管理者：非 keyed，27 个消费方注入的就是它
[Service(ServiceLifetime.Singleton, typeof(IMetadataService))]
public sealed partial class MetadataService : IMetadataService, IDisposable
{
    [GeneratedConstructor]
    public partial MetadataService(IServiceProvider serviceProvider);
}
```

要点：

- **keyed 注册落在 `IMetadataProvider` 上**：这是"两个实现、一个契约、按 key 选择"的标准形式
  （仓库里 `IGachaLogQueryProvider` / `IHutaoEndpoints` 同型），不需要为每个源再造一个子接口只为注册。
  子接口 `IGitMetadataProvider` / `IProtoMetadataProvider` 只用于 provider 特有的诊断成员。
- **由工厂"创建"而不是由管理者直接注入两个 provider**：
  - 管理者只依赖 `IMetadataProviderFactory`，不依赖具体 provider，切换/回退逻辑与 DI 细节解耦；
  - 只有**被选中的** provider 才会被解析（keyed singleton 首次解析时创建），未选中的源不占内存；
  - 测试可以注入一个假工厂，不需要构造真实 keyed 容器。
  - 服务定位只存在于 `MetadataProviderFactory.GetOrCreate` 一处；这是运行时按 key 选择的必要例外，
    不是把 `IServiceProvider` 塞进页面/ViewModel（`CODE_REVIEW.md#ARCH-01`）。
- 管理者必须保持 **非 keyed** 注册：否则 `ValidateOnBuild = true` 会在启动时报
  `IMetadataService` 不可解析；27 个消费方也全部依赖非 keyed 解析。
- `MetadataSourceKind.Auto` **不能**出现在任何 `Key` 上，也不能被 provider 的 `Kind` 返回；
  用一条断言（启动期或测试）保证"keyed 注册集合 == factory 的 `AvailableKinds`"
  （Phase 1 为 `{Git}`，Phase 2 起为 `{Git, Proto}`）。
- 两个 provider 都从 `MetadataProviderBase` 继承，使用
  `[GeneratedConstructor(CallBaseConstructor = true)] public partial XxxProvider(IServiceProvider serviceProvider);`，
  基类构造函数手写（它不是 `[Service]` 类型，生成器不为它生成构造函数）。
- provider 特有的诊断信息（`BundleInfo` / `RepositoryDirectory`）不进 `IMetadataService`：
  设置页/诊断页可以直接 `[FromKeyed(MetadataSourceKind.Proto)]` 注入 `IProtoMetadataProvider` 读取，
  这是仓库既有的 keyed 消费方式，不需要管理者转发。

### 5.4 Provider 基类：单源初始化闸门

Provider 只负责"把自己的数据准备好、读出来"，**不关心**谁在调用、失败后是否回退：

```csharp
public abstract class MetadataProviderBase : IMetadataProvider
{
    private readonly SemaphoreSlim initializationGate = new(1, 1);
    private bool isInitialized;

    protected MetadataProviderBase(IServiceProvider serviceProvider)
    {
        Logger = serviceProvider.GetRequiredService<ILogger<MetadataProviderBase>>();

        // 前缀 = Metadata.{Git|Proto}.{locale}
        // Kind 只允许返回常量，不得依赖派生类字段（构造函数期虚调用）
        string locale = serviceProvider.GetRequiredService<CultureOptions>().LocaleName;
        Cache = new(serviceProvider.GetRequiredService<IMemoryCache>(), $"Metadata.{Kind}.{locale}");
    }

    public abstract MetadataSourceKind Kind { get; }

    public MetadataCache Cache { get; }

    public bool IsInitialized => Volatile.Read(ref isInitialized);

    protected ILogger Logger { get; }

    public async ValueTask<bool> InitializeAsync(CancellationToken token = default)
    {
        using CancellationTokenSource linked = CancellationTokenSource.CreateLinkedTokenSource(token, XamlApplicationLifetime.ShutdownToken);
        await initializationGate.WaitAsync(linked.Token).ConfigureAwait(false);
        try
        {
            if (IsInitialized) return true;
            using SentrySpanScope span = SentrySpanScope.StartOperation($"metadata.{Kind}.initialize", "metadata");
            try
            {
                isInitialized = await InitializeCoreAsync(linked.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception exception)
            {
                // 失败不缓存"已初始化"，管理者可再次调用以重试
                Logger.LogWarning(exception, "Metadata provider {Kind} initialization failed", Kind);
                isInitialized = false;
            }

            return IsInitialized;
        }
        finally
        {
            initializationGate.Release();
        }
    }

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

- 没有 TCS：provider 的 `InitializeAsync` 只是"确保+返回结果"，等待/重试/回退的语义在管理者
  （`IMetadataService`）里统一处理，避免两层 TCS 语义漂移。
- `InitializeCoreAsync` 对 Proto 是"确保文件 + 解码 + 头部校验"，对 Git 是"clone/fetch 仓库"；
  失败返回 `false`（不抛）由管理者决定是否换源；只有取消会抛出。
- `ClearCache` 负责清自己的 L2/L3；Proto 保留 L1 `LanguageMetadata`（实现选择，见 §15.2 第 7 条），不重新解码。

### 5.5 管理者：创建 provider、选源、切换与网络重试

```csharp
[Service(ServiceLifetime.Singleton, typeof(IMetadataService))]
public sealed partial class MetadataService : IMetadataService, IDisposable
{
    private const string RetryKey = "MetadataService.Initialization";

    // 生成构造解析：IMetadataProviderFactory / MetadataSourceOptions /
    // INetworkRetryCoordinator / IMessenger / ILogger<MetadataService>
    private readonly IMetadataProviderFactory providerFactory;
    private readonly MetadataSourceOptions sourceOptions;
    private readonly INetworkRetryCoordinator networkRetryCoordinator;
    private readonly IMessenger messenger;
    private readonly ILogger<MetadataService> logger;
    private readonly Lock gate = new();
    private readonly Dictionary<MetadataSourceKind, IMetadataProvider> created = new();

    private TaskCompletionSource initialized = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private Task<bool>? attempt;
    private MetadataSourceKind? requested;            // SwitchAsync 指定的偏好，优先于设置
    private volatile IMetadataProvider? active;
    private IDisposable? retryRegistration;

    [GeneratedConstructor]
    public partial MetadataService(IServiceProvider serviceProvider);

    public MetadataSourceKind ActiveSource => active?.Kind ?? MetadataSourceKind.Auto;

    public IReadOnlyList<MetadataSourceKind> AvailableSources => providerFactory.AvailableKinds;

    public bool IsInitialized => active?.IsInitialized is true;

    public MetadataCache Cache => Active.Cache;

    public ValueTask<ImmutableArray<T>> FromCacheOrSourceAsync<T>(MetadataCategory category, CancellationToken token = default)
        where T : class
    {
        return Active.FromCacheOrSourceAsync<T>(category, token);
    }

    private IMetadataProvider Active => active ?? throw HutaoException.InvalidOperation(SH.ServiceMetadataNotInitialized);

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
        lock (gate)
        {
            MetadataSourceKind preference = requested ?? sourceOptions.Source.Value;
            return new(attempt ??= AttemptAsync(initialized, preference, token));
        }
    }

    public async ValueTask<bool> SwitchAsync(MetadataSourceKind kind, CancellationToken token = default)
    {
        // kind == Auto 表示恢复自动选择；Git/Proto 表示固定偏好
        // 先清旧缓存：在途读取的结果会因代际失效而不写入旧 cache
        if (active is { } previous) previous.ClearCache();

        TaskCompletionSource next = new(TaskCreationOptions.RunContinuationsAsynchronously);
        lock (gate)
        {
            requested = kind;
            active = null;
            attempt = null;
            initialized = next;
        }

        return await InitializeInternalAsync(token).ConfigureAwait(false);
    }

    private IMetadataProvider GetProvider(MetadataSourceKind kind)
    {
        IMetadataProvider provider = providerFactory.GetOrCreate(kind);   // keyed singleton，首次解析时创建
        lock (gate) { created[provider.Kind] = provider; }
        return provider;
    }

    private async Task<bool> AttemptAsync(TaskCompletionSource completion, MetadataSourceKind preference, CancellationToken token)
    {
        bool fellBack = false;
        try
        {
            foreach (MetadataSourceKind kind in EnumerateCandidates(preference))
            {
                IMetadataProvider provider = GetProvider(kind);
                if (await provider.InitializeAsync(token).ConfigureAwait(false))
                {
                    // 初始化期间发生了 SwitchAsync：本次结果作废，不能覆盖新的 active。
                    // active 与 TCS 必须在同一把锁里提交，否则 SwitchAsync 可能刚好插在中间。
                    lock (gate)
                    {
                        if (!ReferenceEquals(initialized, completion)) return false;
                        active = provider;
                        completion.TrySetResult();
                    }

                    networkRetryCoordinator.ClearPending(RetryKey);

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
        catch (OperationCanceledException) { throw; }
        catch (Exception exception)
        {
            // 自启动路径 `_ = InitializeInternalAsync()` 会丢弃这个 Task，异常必须在这里收敛
            logger.LogWarning(exception, "Metadata initialization attempt failed");
            return false;
        }
        finally
        {
            lock (gate)
            {
                if (!ReferenceEquals(initialized, completion) || !IsInitialized) attempt = null;
            }
        }
    }

    public void ClearCache()
    {
        IMetadataProvider[] providers;
        lock (gate) { providers = [.. created.Values]; }
        foreach (IMetadataProvider provider in providers) provider.ClearCache();
    }

    private static IEnumerable<MetadataSourceKind> EnumerateCandidates(MetadataSourceKind preference)
    {
        if (preference is MetadataSourceKind.Proto or MetadataSourceKind.Auto)
        {
            yield return MetadataSourceKind.Proto;
        }

        yield return MetadataSourceKind.Git;
    }

    private async ValueTask<bool> RetryAfterNetworkRecoveryAsync(CancellationToken token)
    {
        lock (gate) { attempt = null; }
        return await InitializeInternalAsync(token).ConfigureAwait(false);
    }

    partial void PostConstruct(IServiceProvider serviceProvider)
    {
        retryRegistration = networkRetryCoordinator.Register(RetryKey, RetryAfterNetworkRecoveryAsync);
        sourceOptions.Source.PropertyChanged += OnSourceOptionChanged;
    }

    private void OnSourceOptionChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName is nameof(IObservableProperty<MetadataSourceKind>.Value))
        {
            // 设置已由 MetadataSourceOptions 持久化；这里只负责切换
            SwitchAsync(sourceOptions.Source.Value).AsTask().SafeForget();
        }
    }

    public void Dispose()
    {
        sourceOptions.Source.PropertyChanged -= OnSourceOptionChanged;
        retryRegistration?.Dispose();
    }
}
```

职责边界（完整失败矩阵见 §9.2）：

- **创建**：`GetProvider` 是唯一的 provider 来源。`MetadataProviderFactory.GetOrCreate` 首次解析时创建
  keyed singleton，此后切换复用同一实例；未选中的源不会构造。
- **偏好来源**：候选顺序用 `requested ?? sourceOptions.Source.Value`。`SwitchAsync` 只改 `requested`
  （内存偏好），设置持久化仍由 `MetadataSourceOptions` 的调用方负责；启动、页面自启动、网络重试都走同一口径。
- **选源/回退**：`EnumerateCandidates`：`Auto`/`Proto` → `[Proto, Git]`；`Git` → `[Git]`。
  `Auto` 空闲时静默回退；显式 `Proto` 回退时提示一次；显式 `Git` 不回退。
- **切换**：`SwitchAsync(kind)` 只激活、不写设置（持久化由 `MetadataSourceOptions` 的调用方负责）。
  `kind` 是偏好：`Auto` → 自动选择；`Proto`/`Git` → 固定偏好。它按同一套候选规则激活目标源——
  切到 `Proto` 仍可能以 Git 结束并提示，切到 `Git` 则只试 Git，`Auto` 两个都试。
  切换时**先清旧缓存、再置空 `active` 并换 TCS、再初始化新 provider**；`AttemptAsync` 捕获自己所属的 TCS，
  完成时若已不是当前 TCS 就丢弃结果，避免旧 attempt 把 `active` 覆盖回旧源。
  代价：切换窗口内 `active` 为空，页面读取会收到 `SH.ServiceMetadataNotInitialized`；
  所以设置页应 `await SwitchAsync(...)` 完成后再刷新页面数据，不要让用户在窗口内打开新的元数据页。
  （若实测窗口不可接受，再改成"新源就绪后才原子替换 `active`"，本设计不预先增加这个复杂度。）
- **等待**：`InitializeAsync` 等的是管理者自己的 TCS；provider 不再持有 TCS（§5.4），语义只有一层。
- **网络重试**：与 `RepositoryCatalogService.ReconnectSharedAsync` 同一模式（`Register` + `MarkPending`/
  `ClearPending`），并由元数据自己的 job 产出数据，避免等待元数据的页面 job 相互阻塞。
- **清理**：`ClearCache` 广播给**所有已创建**的 provider（含回退尝试中创建但未激活的），
  所以"切到 Proto→切回 Git"不会命中旧数据（§6.4）。
- `Cache` / `FromCacheOrSourceAsync` 在初始化完成前被访问时，仍然走现有
  `SH.ServiceMetadataNotInitialized` 语义（`HutaoException.InvalidOperation`），与
  `FromCacheOrFileAsync` 的 `Verify.Operation` 一致。
- `AttemptAsync` 必须自己吞掉非取消异常并记日志（返回 `false`）：它同时被 `InitializeAsync` 的
  自启动路径 `_ = InitializeInternalAsync()` 丢弃，未观察的异常会变成未处理异常。
- 管理者实现 `IDisposable` 释放 `retryRegistration`；`ValidateOnBuild` 只校验注册可解析，不校验这个引用。

## 6. 缓存与 `ClearCache`

### 6.1 三层缓存

| 层 | 内容 | 位置 | 生命周期 |
|---|---|---|---|
| L0 | 磁盘文件（JSON 树 / bundle / 清单） | `{Data}/Repository/Snap.Metadata/…`、`{Data}/Metadata/{locale}/…` | 跨进程 |
| L1 | 解析后的 `LanguageMetadata`（proto 源） | `ProtoMetadataProvider` 字段，`AsyncLock` 保护 | 进程内，`ClearCache` 释放 |
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
| 释放 | Proto **保留** L1 `LanguageMetadata`（本地包在进程内不变，重新解码没有收益；见 §15.2 第 7 条）；Git provider 无需释放 |
| 初始化状态 | **不重置** `IsInitialized`。磁盘/网络来源仍然有效，下一次类别读取会重新解析/重读文件；页面的 `InitializeAsync()` 快路径继续成立。改变来源要显式 `SwitchAsync`，它不是 `ClearCache` 的副作用 |
| 失败 | 单键 `Remove` 失败不影响其余键（`IMemoryCache.Remove` 本身不抛业务异常） |
| Sentry | `metadata.cache.clear` span，仅记录被清理的键数量与来源，不写路径 |
| 调用时机 | ① 用户"重新加载元数据"命令（新增，见 §12 Phase 3）；② `SwitchAsync` 内部先清旧 provider；③ 成功替换 bundle 后（刷新流程）；④ 语言切换（当前靠 `AppInstance.Restart`，重启后天然干净；保留此调用以备将来取消重启） |
| 不负责 | 删磁盘文件、重新下载、更新检测、改变来源 |

**当前实现缺口**：`ClearCache` 只清内存里的已解析结果，**不**发网络请求、也不判断远端 bundle 是否更新。
L0 磁盘 bundle 的新鲜度检查（ETag 条件请求）尚未实现（§8.3），因此它落地前 `ClearCache` 只会让下一轮
重新解析**同一份**本地包，服务端的新数据仍然到不了客户端。

管理者的 `ClearCache()` 广播给**所有已创建**的 provider（`created` 字典，来自
`IMetadataProviderFactory`）：清理非活动源是幂等且廉价的操作，能避免"切到 Proto → 切回 Git"时
命中旧的 L2/L3 数据。provider 自己只清自己的缓存（Proto 保留 L1），管理者不接触具体缓存键。

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
| **HTTP：static metadata 目录**（`{apiRoot}/static/metadata/GenshinMetadata.{locale}.hutaobin`） | 1 个文件（CHS ≈ 3.6 MiB） | 本地优先；无清单，无法自动检测变化 | 只需把产物放进 `static/metadata` | **推荐（已实现）** |
| 新 git 仓库 `Snap.Metadata.Hutaobin` | 整仓（15 个语言包 + 59.7 MiB 合并包） | `git fetch`，可能重下多文件 | 需要 catalog 行、`BackgroundActivity`、镜像配置 | 备选；保留 `IMetadataBundleSource` 缝隙 |
| 复用 `Snap.Metadata` 仓库 | 349.9 MiB 工作树 | 同上 | 无 | 否 |

推荐 HTTP 的理由：只需要一个语言包；`RetryHttpHandler` / 代理 / 域名回退 / 限时都已具备；
不引入第二个 git 仓库与 catalog 维护成本。仓库方案留作 `IMetadataBundleSource` 的第二个实现，
`ProtoMetadataProvider` 不需要改。

### 8.2 `HttpMetadataBundleSource`

实际分发（2026-11-22 起）：

```
GET {apiRoot}/static/metadata/GenshinMetadata.{locale}.hutaobin
本地 {Data}/Metadata/{locale}/GenshinMetadata.hutaobin 存在 → 直接返回（不再下载）
否则 GET 上述 URL
  → 写 {path}.tmp（流式 + 进度）
  → 尺寸上限 + 解码 + language 校验
  → File.Move(tmp, path, overwrite: true)            ← 原子替换；校验失败删除 tmp
```

约束：

- **没有清单。** 生产者目前不输出 `GenshinMetadata.manifest.json`，因此客户端不做清单哈希/版本预检，也无法在"远端已更新"时自动刷新；本地包存在即优先使用。需要更新时删除本地包（或等 Phase 3 的"重新加载元数据"命令）。清单仍是 §10.1 的生产者待办；在清单落地之前，更新检测计划改用 ETag 条件请求（§8.3）。
- `{locale}` 是元数据 locale 名（`CHS`/`CHT`/`EN`/…），不是 language code；URL 由 `IInfrastructureMetadataEndpoints.MetadataBundle(locale)` 生成。
- **先校验后替换**：与 `CLAUDE.md` §6 "parse-first、事务式替换" 同一规则。不能用"先覆盖再校验"，否则一次网络抖动会让用户失去可用的旧包。
- 超时：`HttpClient.Timeout = InfiniteTimeSpan` 是全局策略，下载自带 `CancellationTokenSource.CancelAfter`（120 s）并链接 `ShutdownToken`。
- 进度：通过计数流回调 `BackgroundActivityOptions.MetadataInitialization`（`MainView.xaml` 唯一绑定的元数据活动）设置 `HasProgress` / `IsIndeterminate` / `ProgressValue`，描述显示下载百分比。
- 失败时**不清空**已有 bundle：没有网络但本地有旧包时，Proto 源仍可初始化（离线可用）。
- 只在 `Auto`/`Proto` 下调用；`Git` 模式完全不碰网络下载。
- 开发联调：读取本地设置 `Snap::Hutao::Metadata::BundlePath`（绝对路径）时跳过下载，直接从该文件解码；该开关不暴露 UI。

### 8.3 缓存新鲜度与远端更新检测（尚未实现，计划用 ETag）

**结论：缓存机制还没有完全实现好。** 已经实现的是"已解析结果"的缓存（L1/L2/L3 与 `ClearCache` 广播），
缺的是 **L0 磁盘层的"新鲜度"判定**——本地 bundle 存在就直接信任，此后不再询问服务器，
因此**当前无法侦测远程服务器的更新**。

| 层 | 现状 | 缺口 |
|---|---|---|
| L0 磁盘 bundle | 下载"先校验后原子替换"；`File.Exists` 命中即返回 | **没有新鲜度检查**：`EnsureLocalBundleAsync` 在本地包存在时直接返回，不发任何请求，服务端发布新包无从得知 |
| L0 磁盘 JSON 树（Git 源） | `GitRepositoryService` 每次初始化 clone/fetch，能发现远端更新 | 代价是整仓拉取（§1），不是 bundle 方案想要的路径 |
| L1 `LanguageMetadata` | 进程内保留，`ClearCache` 不释放（§15.2 第 7 条） | 与 L0 同生共死：磁盘包不变就不会重新解码，天然无法带来新数据 |
| L2/L3 `MetadataCache` | 前缀 + 代际 + 键跟踪，`ClearCache` 可清 | 只能清"已解析结果"，不能判断"上游是否有新数据" |

实际行为：**首次启动下载一次，此后一直使用本地包**。服务端发布新 bundle（新游戏版本、类别修复）时，
已安装的客户端不会察觉；只有删除 `{Data}/Metadata/{locale}/GenshinMetadata.hutaobin`、使用
`SettingKeys.MetadataBundlePath` 开发覆盖，或等 Phase 3"重新加载元数据"命令（在无清单/无 ETag 时同样只能
**无条件全量重下**）才会更新。这不是缓存策略调优问题，而是"更新检测"这一整块还没写。

**后续方案：服务器返回的 `ETag` + `If-None-Match`**

1. 下载成功后把响应头 `ETag`（连同 `Last-Modified`、locale、大小、`XxHash64`、下载时间）保存到
   `{BundlePath}.state.json`（PascalCase JSON；先写 `.tmp` 再 `File.Move(overwrite: true)`，与 bundle
   本身同一套原子替换规则）。state 与 bundle 必须成对更新，避免"包新 state 旧"。
2. `EnsureLocalBundleAsync` 的"本地包存在"分支不再直接返回，改为发**条件 GET**：
   `If-None-Match: <etag>`（有 `Last-Modified` 时同时带 `If-Modified-Since`）。
   用 GET 而不是 HEAD：未变化时服务器回 `304`（响应体几乎为空），变化时直接回 `200` + 新包正文，
   省掉一次往返。
3. 按状态码分支：
   - `304 Not Modified` → 继续用本地包，只刷新 state 里的"上次检查时间"。
   - `200 OK` → 走现有下载/校验路径，成功原子替换 bundle 后同步替换 state。
   - `412 Precondition Failed`、或响应缺 `ETag` → 退回无条件下载（服务端没配 ETag 时仍必须能更新）。
   - 其他失败（离线、5xx、超时）→ 保留本地包并静默降级，维持"离线可用"契约（§8.2）。
4. 触发时机与频率：只在初始化时检查一次，**不随页面加载重复请求**；同一进程最多检查一次；两次检查之间设
   最短间隔（建议 6–24 h，具体值待定），避免每次启动都打 CDN。`ClearCache` 不触发检查（§6.4）。
5. 不检查的情况：`MetadataBundlePath` 开发覆盖路径（直接读指定文件，不发请求），以及本地包缺失
   （此时本来就必须无条件下载）。
6. 结果校验：`304` 之后仍要确认本地文件存在且可解码，失败则改走无条件下载；ETag 只作"是否变化"的提示，
   **不能**替代解码与 language 校验（AES-CBC 无认证，§7.2）。
7. 可行性：`HttpMetadataBundleSource` 已用 `HttpRequestMessageBuilder` + `HttpClient` 直连，请求头可写
   （`builder.SetHeader(...)`）、响应头可读（`response.Headers.ETag`），`RetryHttpHandler` 对 `GET` 幂等
   重试，不需要新基础设施；也**不能依赖 HTTP 缓存层**——当前 `HttpClient` 未配任何响应缓存，`304` 语义
   必须自己处理。
8. 为什么现在没做：分发侧还没有清单与 `schema_version`（§10.1），ETag 属于同一条"分发契约"待办；
   在确认服务端会稳定返回并保留 ETag 之前，客户端写条件请求只会退化为"每次全量重下"，没有收益。

实现时的验收：假 `HttpMessageHandler` 覆盖 `200`（新包）/ `304` / `412` / 无 `ETag` / 离线五种情况；
`304` 不写 bundle、只写 state；`200` 与 state 成对原子替换；离线保留旧包且不报错；
`ClearCache` 不碰 L0（§6.4）。

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

- `[Service(..., typeof(IMetadataProvider), Key = MetadataSourceKind.Git)]` /
  `Key = MetadataSourceKind.Proto` 只允许这两个值；`Auto` 不是 provider，也不是 key。
- 设置项：`SettingKeys.MetadataSource = "Snap::Hutao::Metadata::Source"`，
  `MetadataSourceOptions : DbStoreOptions` 暴露 `IObservableProperty<MetadataSourceKind>`；
  管理者订阅 `PropertyChanged` → `SwitchAsync(newKind)`（切换会先清旧 provider 缓存、再按新偏好激活）。
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

`SwitchAsync(kind)` 使用同一张矩阵：`SwitchAsync(Proto)` 失败且 Git 可用时仍会回退并提示；
`SwitchAsync(Git)` 失败则停留在未初始化状态（不会偷偷回到 Proto）；`SwitchAsync(Auto)` 等价于恢复自动选择。

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
| `IMetadataService` | `IMetadataService` | 语义改为"provider 管理者"，不改名（避免 27 处改动） |
| `MetadataService` | `GitMetadataProvider` | 原实现成为 `IMetadataProvider` 的 Git 实现 |
| `MetadataOptions` | `GitMetadataProviderOptions` | git 路径 / 仓库名 |
| `IMetadataServiceInitialization` | `IMetadataServiceInitialization` | 不变；仍是管理者（`IMetadataService`）的启动入口 |
| `MetadataFileStrategy` | `MetadataCategory` | 可选；"文件"对 proto 不成立；新增 `ProtoName`（默认 = `Name`，`BydMaterial` ↔ `BeyondItem`） |
| `MetadataFileStrategies` | `MetadataCategories` | 30 个类别常量 |
| `FromCacheOrFileAsync<T>` | `FromCacheOrSourceAsync<T>` | 两个扩展文件（26 + 28 个方法）机械替换 |
| `IMetadataService.MemoryCache` | `IMetadataService.Cache`（`MetadataCache`） | 唯一使用点在字典扩展 |
| `LegacyMetadataService` / `LegacyMetadataOptions` | 删除 | ENG-08；其模板/远端 URL 逻辑迁入 `HttpMetadataBundleSource` |
| —（新增） | `IMetadataProvider` / `IGitMetadataProvider` / `IProtoMetadataProvider` / `IMetadataProviderFactory` / `MetadataProviderFactory` / `MetadataProviderBase` / `ProtoMetadataProvider` / `MetadataSourceKind` / `MetadataSourceOptions` / `MetadataBundleCodec` / `ProtoCategoryTable` / `MetadataCache` / `IMetadataBundleSource` | 见 §5.1 |

"门面"这个叫法在本文件里不再使用：`MetadataService` 是**管理者**（创建/持有 provider、选源、切换），
`IMetadataProvider` 才是数据来源。这两层名字与职责一一对应，避免 `I*Service` 同时指"来源"和"服务入口"。

### 11.3 新增文案（只改 `SH.resx`）

| key | 用途 |
|---|---|
| `ServiceMetadataInitializationFailed` | 初始化失败（`NetworkRetryCoordinator.MarkPending` 的提示；离线时由网络提示接管） |
| `ServiceMetadataSourceFallback` | 显式 Proto 偏好回退到 Git 后的一次性警告 |
| `ServiceMetadataBundleDownloadFailed` | 清单/包下载失败（带原因占位符） |
| `ServiceMetadataBundleInvalid` | 校验失败（大小/哈希/语言/解析） |
| `ServiceMetadataBundleDownloading` / `ServiceMetadataBundleValidating` / `ServiceMetadataDecoding` | 下载百分比、校验、解析进度文案 |

`ServiceMetadataNotInitialized`、`ServiceMetadataFileNotFound`、`ServiceMetadataVersionNotSupported`
已存在，直接复用；其余语言文件不在同一 PR 修改（`CLAUDE.md` §5.7）。

## 12. 分阶段实施

### Phase 0：解码器与 schema 接入（无行为变化）

- 新增 `MetadataBundleCodec`、`MetadataBundleInfo`、`BoundedReadStream`、常量（`MetadataBundleSchemaVersion`）。
- 按 §10.5 接入 schema 生成（vendored `Proto/` + `Grpc.Tools`，或提交生成 `.cs`）。
- 测试项目新增 `Google.Protobuf` 引用 + 链接 codec；加"加密→解码"round-trip 测试（小合成消息）。

验收：`dotnet build` / `dotnet test` 通过；不改变任何现有读取路径。

### Phase 1：分层、改名、缓存抽象（仍只用 Git）

- 新增 `IMetadataProvider` / `MetadataProviderBase` / `IMetadataProviderFactory`；
  `MetadataService` → `GitMetadataProvider`（keyed `Git`），`MetadataOptions` → `GitMetadataProviderOptions`。
- `MetadataService` 改为管理者：通过工厂创建 provider、`ActiveSource`、`SwitchAsync`、`ClearCache` 广播；
  此时 `AvailableKinds` 只有 `Git`。
- `MetadataCategory`/`MetadataCategories`（可延后）、`FromCacheOrSourceAsync`、`Cache` 替换 `MemoryCache`。
- 删除 `LegacyMetadataService` / `LegacyMetadataOptions`。
- `InitializeAsync` 改为自启动；`ClearCache` 落地（Git provider）。

验收：行为与现状一致；`ValidateOnBuild` 通过；手动验证 5 个页面；`ClearCache` 后页面重新读取成功。

### Phase 2：Proto provider

- `ProtoMetadataProvider`、`ProtoCategoryTable` + 30 个适配器、`MetadataBundleOptions`、
  `HttpMetadataBundleSource`、`MetadataInitialization` 进度文案。
- keyed `Proto` 注册；工厂 `AvailableKinds` 变为 `{Git, Proto}`；管理者按 `Auto` 优先 Proto。
- 映射层开发不依赖 §10.1/§10.3：用 §8.2 的 `Snap::Hutao::Metadata::BundlePath` 本地开关，
  直接读当前 builder 产出的 `GenshinMetadata.CHS.hutaobin` 即可联调；但**上线** Proto 仍需
  §10.1（版本 + 清单）与 §10.3（语言目录布局）。
- `MetadataBuilder` 侧 §10.2 已完成；剩余契约见 §10 状态表。

验收：`Snap.Metadata` 仓库**不被 clone**（删除本地仓库后启动验证）；30 个类别全部读取成功；
数量与 JSON 源一致；离线（本地已有包）可启动。

### Phase 3：切换、回退、刷新与清理

- `MetadataSourceKind` 设置项 + 管理者回退 + 回退警告 + `SwitchAsync` 入口。
- "重新加载元数据"命令（`SettingStorageViewModel` 或新的元数据卡片）：
  `EnsureLocalBundleAsync` → `ClearCache`。
- 诊断信息：`AvailableSources`、`ActiveSource`、`BundleInfo`（版本/生成时间）暴露给设置页（可选）。

验收：三种设置组合行为符合 §9.2；`SwitchAsync` 后页面数据更新且不残留旧源数据；回退提示只出现一次。

### Phase 4：验证与文档

- 性能/内存测量、Debug 平价校验（§13）、`CLAUDE.md` §5.5 更新、`CODE_REVIEW.md` §14 追加一行
  （并标记 ENG-08 已解决）。

## 13. 验证方案

| 项 | 方法 | 通过标准 |
|---|---|---|
| 构建 / DI | `dotnet build src/Snap.Hutao.Remastered/Snap.Hutao.Remastered.slnx -c Debug -p:Platform=x64` | `ValidateOnBuild` 无异常；两个 keyed provider + 工厂 + 管理者注册齐全 |
| 工厂 / 选源 | 测试：假 `IMetadataProviderFactory` 返回可控 provider | 管理者只创建被选中的 provider；`AvailableKinds` 与注册一致 |
| 切换 | 测试：初始化后 `SwitchAsync`；初始化中途 `SwitchAsync` | 旧 attempt 不覆盖新 active；旧缓存已清；未初始化访问抛 `SH.ServiceMetadataNotInitialized` |
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
| 解析内存峰值 | 3.6 MiB 包解析后对象图可能数十 MiB | 当前保留 L1（§15.2 第 7 条）；内存测量仍开放 |
| schema 无版本、字段号已整体后移 | 旧客户端按字段号解析新包会静默错位 | 发布前补 §10.1；解码后仍按语言/类别名/条目数做结构校验 |
| `Grpc.Tools` 构建依赖 | 新增原生 protoc 与构建时间 | 版本集中管理；CI 缓存 NuGet；失败时回退到提交生成 `.cs` |
| 双源行为漂移 | 同一页面两源数据不一致 | 平价校验；以 JSON 源为基准，Proto 修复对齐 |
| 设置项默认 `Auto` | 发布端尚未就绪时首启回退一次 | 回退只在无本地包时发生；提示一次性；可在设置中固定 Git |
| **无远端更新检测（缓存缺口）** | 服务端发布的元数据修复/新版本到不了已安装客户端：本地包被永久信任，除非删除文件或手动刷新 | 现状如此（§8.3）；后续用 `ETag` + `If-None-Match` 条件请求，`304` 复用、`200` 替换 |

未决问题：

1. 单语言包最终托管在 metadata CDN 还是新 git 仓库？（本文推荐前者；`IMetadataBundleSource` 已隔离该决定）
2. 是否保留 Git 源长期作为回退，还是在某个版本后下线 JSON 路径（影响 `MetadataFileJsonContext` 与 19.2 MiB JSON 维护成本）。
3. "重新加载元数据"放在设置页何处、是否需要取消正在进行的初始化。
4. `schema_version` 的所有权与发布流程：由谁在 `MetadataBuilder` 里决定升版本（建议：改 proto 的 PR 必须动版本并在 PR 描述写原因）。
5. 平价校验是否长期保留在 Debug（建议保留，成本低、收益高）。
6. 是否允许将来新增更多 provider（内置快照、HTTP 元数据……）：工厂 + keyed 注册已经支持，
   但 `MetadataSourceKind`、设置文案与 `AvailableSources` 展示需要同步扩展，不打算做成插件点。
7. ETag 检查的触发点与最短间隔（建议启动时一次、进程内一次、间隔 6–24 h），以及分发端能否稳定返回并
   保留 `ETag` / `Last-Modified`——需要与 metadata 发布流程一起确认（§8.3）。

## 15. 实现记录（2026-11-22）

实现范围：Phase 0 / 1 / 2 与 Phase 3 的服务侧（选源、静默回退、`SwitchAsync`、`ClearCache`、网络重试）。
设置页 UI（"重新加载元数据"命令、来源下拉框）未实现，因此 `SwitchAsync` / `ActiveSource` /
`AvailableSources` / `ClearCache` 目前只有服务代码与测试调用方。

### 15.1 落地的文件

| 区域 | 文件 |
|---|---|
| 契约 | `Service/Metadata/IMetadataService.cs`、`IMetadataServiceInitialization.cs`、`Provider/IMetadataProvider.cs`、`Provider/IMetadataProviderFactory.cs` |
| 管理者 | `Service/Metadata/MetadataService.cs`、`MetadataSourceOptions.cs` |
| provider | `Provider/MetadataProviderBase.cs`、`Provider/MetadataProviderFactory.cs`、`Provider/Git/*`、`Provider/Proto/*` |
| 缓存 / 类别 | `MetadataCache.cs`、`MetadataCategory.cs`、`MetadataCategories.cs`、`MetadataSourceKind.cs` |
| 解码 / 传输 | `Provider/Proto/MetadataBundleCodec.cs`、`BoundedReadStream.cs`、`MetadataBundleConstants.cs`、`MetadataBundleInfo.cs`、`HttpMetadataBundleSource.cs`、`IMetadataBundleSource.cs` |
| 映射 | `Provider/Proto/ProtoCategoryTable.cs`、`ProtoCategoryAdapter.cs`、`MetadataConvert.cs`、`Mapper/*.cs`（30 个） |
| 协议 | `Proto/*.proto`（38 个，vendored）、`Proto/Generated/*.cs`（提交的 protoc 输出）、`Proto/README.md`、`scripts/Generate-MetadataProto.ps1` |

`LegacyMetadataService` / `LegacyMetadataOptions` 已删除（ENG-08）；`MetadataOptions` 改名为
`GitMetadataProviderOptions`；`MetadataFileStrategy` / `MetadataFileStrategies` 改名为
`MetadataCategory` / `MetadataCategories`；`FromCacheOrFileAsync` 改名为 `FromCacheOrSourceAsync`。

### 15.2 与本文设计的差异

1. **标识 / 等级 / 计数改为 `uint32`（builder 与客户端同时改）。** 应用模型用 `uint` 承载 id、等级、排名与
   数量（identity 结构体的底层类型是 `uint`），原 schema 的 `int64` / `int32` 让映射层必须做范围检查并可能
   丢条目。`MetadataBuilder/Proto` 中这些字段已改为 `uint32`，builder 的 `ProtoMapper` /
   `ProtoJsonMapper` 增加 `UInt32` / `UInt64` 转换。小于 `2^31` 的数值在 varint 编码下与 `int64` 完全一致，
   因此本次改动前生成的旧 bundle 仍可被新客户端读取（已用本机 CHS 包验证）。
2. **生成代码提交进仓库，构建期不运行 protoc。** `Grpc.Tools` 只在"本次构建真的运行了 protoc"时把生成
   文件加入 `@(Compile)`；一次"生成成功但编译未完成"的构建会让后续增量构建永久缺少这些文件
   （`CS0234`）。因此 `Proto/Generated` 作为普通源码提交，`scripts/Generate-MetadataProto.ps1` 负责重生成，
   `Grpc.Tools` 仅以 `ExcludeAssets=all` 保留用于取得 `protoc`。这是 §10.5 的备选方案。
3. **未定义枚举值保留原数值，而不是收敛到 `NONE`。** JSON 源（System.Text.Json）同样保留原始数字，
   现有数据依赖它：`Chapter.CityId` 已出现 8 / 105，`ProfilePicture.UnlockType` 已出现 5。收敛会让两个源
   产生可观测差异。`MetadataConvert.ToEnum` 保留数值并对每个 `(枚举, 字段, 值)` 只记一次警告；
   `ProfilePictureUnlockBy` 增补 `Default = 5`，让该值成为已定义成员。
4. **proto3 空字符串在可空模型属性上映射为 `null`。** proto3 无法区分"缺失"与空串；JSON 源对
   `WhenWritingNull` 省略的属性是 `null`。少数 builder 显式写 `""` 的属性（如
   `MainQuest.UnlockDescription`）在 proto 源会变成 `null`，消费方均按 `IsNullOrEmpty` 处理，行为一致。
5. **`MetadataBundleInfo.SchemaVersion` 固定为 `0`。** bundle 本身没有 `schema_version` 字段，生产者也没有清单；客户端只在下载后解码校验，不做版本预检（§10.1 仍是 builder 待办）。
6. **分发使用 static 目录，没有模板与清单。** `GET {apiRoot}/static/metadata/GenshinMetadata.{locale}.hutaobin`；本地包存在即优先使用，因此**当前无法侦测远端更新**：需要删除本地包、走开发覆盖路径或等 Phase 3 的刷新命令（见 §8.2）。缓存新鲜度这一层尚未实现，后续用服务器返回的 `ETag` + `If-None-Match` 判断是否更新（§8.3）。
7. **L1 保留、映射移出调用线程。** 不再在"全部类别映射一次"后释放 `LanguageMetadata`，`ClearCache` 也只清 L2/L3；类别映射通过 `Task.Run` 在后台执行，并由 `MetadataCache.GetOrCreateAsync` 按 key 去重，页面加载不再在 UI 线程同步映射大类。
8. **平价校验没有内置到 Debug 启动路径。** 本次用一次性外部工具完成（见 15.4）；如果长期保留，应把
   它做成 Debug-only 命令，而不是每次启动都跑。

10. **启动自检修复。** `AttemptAsync` 的局部变量 `bool initialized` 遮蔽了同名字段 `initialized`（`TaskCompletionSource`），使每次成功初始化都被判定为"stale attempt"并丢弃，表现为页面一直等待元数据、Proto/Git 反复初始化。局部变量已改名为 `providerInitialized`，字段比较恢复正确；同时把分发 URL 从旧的模板/镜像路径改为 `{apiRoot}/static/metadata/GenshinMetadata.{locale}.hutaobin`。

11. **下载进度写在 `MetadataInitialization` 上。** 该活动是 `MainView.xaml` 唯一绑定的元数据活动；下载时设置 `HasProgress` / `IsIndeterminate` / `ProgressValue`，描述同时显示百分比和已下载/总大小（例如 `28%，1.0 MiB / 3.6 MiB`）；响应没有 `Content-Length` 时退化为"已下载 N MiB"并按 256 KiB 刷新；探测/解析阶段显示不确定进度。

### 15.3 构建与生成

```powershell
powershell -File scripts/Generate-MetadataProto.ps1          # 重生成 Proto/Generated
powershell -File scripts/Generate-MetadataProto.ps1 -Check   # 校验生成结果是否过期
dotnet build src/Snap.Hutao.Remastered/Snap.Hutao.Remastered.slnx -c Debug -p:Platform=x64
dotnet test  src/Snap.Hutao.Remastered/Snap.Hutao.Remastered.slnx --filter "FullyQualifiedName~Metadata"
```

`MetadataBuilder` 侧同样已构建通过（`dotnet build MetadataBuilder.csproj -c Debug`）。

### 15.4 验证结果

| 项 | 方法 | 结果 |
|---|---|---|
| 构建 / DI | Debug x64 构建（app 与测试项目） | 通过，0 错误；新增/改写文件 0 警告 |
| codec 单测 | 加密 → 解码 round-trip、截断 IV、坏密文、解压上限 | 4/4 通过 |
| 缓存单测 | 代际号、`SetIfCurrent`、`Clear` 只清自己的键 | 5/5 通过 |
| schema 覆盖 | `LanguageMetadata` 的 35 个 message 字段 == 30 个类别 + 5 个显式忽略 | 通过 |
| 映射平价 | 用本机 `GenshinMetadata.CHS.hutaobin` + `Snap.Metadata.Work/Genshin/CHS` 逐类别比较条数、键集合与标量投影 | 30/30 类别全等（`null` ↔ `""` 归一后，见 15.6） |
| 真实 bundle | 本机 CHS 包（旧 schema、`int64` 字段）用新 `uint32` 客户端解码 | 通过（见 15.2 第 1 条） |
| 真实下载 | 从 `{apiRoot}/static/metadata/GenshinMetadata.CHS.hutaobin` 下载并初始化（3,771,744 B） | 通过；首页抽卡/深渊卡片正常显示 |
| 全量测试 | `dotnet test`（app 测试项目） | 321/321 通过 |

### 15.5 仍未完成

**客户端侧（见 §8.3）**：

- **缓存机制还没有完全实现好**：L2/L3 内存缓存与 `ClearCache` 已可用，但 L0 磁盘 bundle 没有新鲜度检查，
  **当前无法侦测远程服务器的更新**。后续用服务器返回的 `ETag` + `If-None-Match` 条件请求判断是否更新
  （`304` 复用本地包，`200` 才下载并成对原子替换 state，`412`/无 `ETag` 退化为无条件下载）；
  前提是分发端稳定返回 ETag。
- 设置页 UI（"重新加载元数据"命令、来源下拉框）未实现，`SwitchAsync` / `ActiveSource` /
  `AvailableSources` / `ClearCache` 目前只有服务代码与测试调用方（§15.1 已注明）。即使命令上线，
  在 ETag/清单落地前它也只能无条件全量重下。

**builder 侧（见 §10）**：`schema_version`、清单文件、单语言包写入语言目录、缺失类别致命、schema 分发方式。
这些不阻塞客户端；但 Proto 成为默认源之前必须补上版本与清单，否则发布端与客户端的字段错位无法被发现。

### 15.6 平价结果

用一次性外部工具（`MetaParity`，引用 app 项目并链接 30 个适配器与解码器）对本机
`GenshinMetadata.CHS.hutaobin`（旧 schema、`int64` 字段，2026-11-21 产物）与
`Snap.Metadata.Work/Genshin/CHS` 逐类别比较：

- 30 个类别的条目数全部相等（例：Achievement 1909、Avatar 122、Material 7219、MainQuest 2760、
  Monster 572、TowerLevel 435、Weapon 253、GachaEvent 304）。
- 以 `Id` / `DescribeId` / `SetId` / `Rank+Level` / `Name+Version+Order` / `Reliquary.Ids[0]` 为键的
  集合完全一致；每个条目的标量投影（字符串、数值、枚举、`DateTimeOffset`、`Uri`、字符串数组）也
  一致。
- 唯一差异是 proto3 的 `null` ↔ JSON `""`（可空字符串，见 §15.2 第 4 条）；比较器把两者归一为同一
  空值后，30/30 类别 0 差异。
- `Avatar`（122 条，含技能、天赋、命座、服装、名刺、游迹）与 `Weapon` / `Monster` 的嵌套映射首次
  运行即全等，说明 `OnDeserialized`、曲线集合与排序字典语义与 JSON 路径一致。
- 空数组与 JSON 缺省一致：`TowerFloor.FirstDescriptions` / `SecondDescriptions`、
  `TowerLevel.FirstMonsters` / `SecondMonsters`、`TowerMonster.Affixes`、`Monster.Drops` 在 proto 侧
  为空时映射为 `default(ImmutableArray<T>)`，与 JSON 缺省相同。

工具没有提交：它通过 `ProjectReference` 引用 WinUI app 项目，与测试项目"不引用 app"的约束冲突；
如果长期保留，应改成 Debug-only 的服务命令（见 §15.2 第 8 条）。

旧 bundle 兼容性：上述 CHS 包由 `cb1eb30` 的 `int64` schema 生成；新客户端以 `uint32` 解析后全部字段
与期望一致（小于 `2^31` 的 varint 编码相同），因此 `uint32` 迁移不需要重建包即可验证。
## 附录 A：文件清单（目标结构）
9. **keyed 注册只落在 `IMetadataProvider` 上。** `IGitMetadataProvider` / `IProtoMetadataProvider` 没有单独注册（两个 keyed 注册会产生两个实例），目前也没有诊断页消费它们；将来需要时应通过工厂取到 `IMetadataProvider` 再转换，或让 provider 以工厂方式共用同一实例。

```
Service/Metadata/
  IMetadataService.cs                  (改：管理者契约 +SwitchAsync/ActiveSource/AvailableSources)
  IMetadataServiceInitialization.cs    (不变)
  MetadataService.cs                   (改：管理者实现)
  MetadataCategory.cs                  (原 MetadataFileStrategy)
  MetadataCategories.cs                (原 MetadataFileStrategies)
  MetadataCache.cs                     (新)
  MetadataSourceKind.cs                (新)
  MetadataSourceOptions.cs             (新)
  Provider/
    IMetadataProvider.cs               (新：数据源契约)
    MetadataProviderBase.cs            (新：单源初始化闸门/缓存)
    IMetadataProviderFactory.cs        (新)
    MetadataProviderFactory.cs         (新：唯一 keyed 解析点)
    Git/
      IGitMetadataProvider.cs          (新)
      GitMetadataProvider.cs           (原 MetadataService)
      GitMetadataProviderOptions.cs    (原 MetadataOptions)
    Proto/
      IProtoMetadataProvider.cs        (新)
      ProtoMetadataProvider.cs         (新)
      MetadataBundleOptions.cs         (新)
      MetadataBundleInfo.cs            (新)
      MetadataBundleCodec.cs           (新)
      BoundedReadStream.cs             (新)
      IMetadataBundleSource.cs         (新)
      HttpMetadataBundleSource.cs      (新)
      ProtoCategoryTable.cs            (新)
      ProtoCategoryAdapter.cs          (新)
      MetadataConvert.cs               (新)
      Mapper/*.cs                      (新：30 个类别适配器)
  ContextAbstraction/…                 (不变)
  MetadataServiceImmutable*Extension.cs (改：键走 Cache.CreateKey，方法名 FromCacheOrSourceAsync)
Proto/                                  (新：vendored schema，§10.5)
  *.proto                               (从 MetadataBuilder/Proto 原样复制，38 个文件)
```

## 附录 B：与现有文档的衔接

- 实现落地时更新 `CLAUDE.md` §5.5：`IMetadataProvider` / `IMetadataService` 两层、keyed provider、
  `SwitchAsync` 与 `ClearCache` 语义、bundle 格式与来源。
- `CODE_REVIEW.md` §14 追加一行；ENG-08（`LegacyMetadataService`）随 Phase 1 关闭。
- 若 Phase 2 引入 `Grpc.Tools`，`Directory.Packages.props` 与 `CLAUDE.md` §2 的构建说明需要同步。

## 附录 C：本文变更记录

| 日期 | 变更 |
|---|---|
| 2026-11-22（三） | 记录缓存机制尚未完全实现：L2/L3 与 `ClearCache` 已可用，L0 磁盘 bundle 无新鲜度检查，**当前无法侦测远程服务器的更新**；新增 §8.3（现状表、实际行为、后续 `ETag` + `If-None-Match` 条件请求方案、触发频率、兼容与验收），同步更新状态行、TL;DR 第 7 条、§6.4（修正"释放 L1"表述并补缺口说明）、§8.2、§15.2 第 6 条与 §15.5（拆出客户端侧待办）。 |
| 2026-11-22（二） | 按"服务管理 provider"重新分层：新增 `IMetadataProvider` / `IMetadataProviderFactory`，原 `IGit/IProtoMetadataService` 改为 `IGit/IProtoMetadataProvider`，`GitMetadataService` / `ProtoMetadataService` 改为 `GitMetadataProvider` / `ProtoMetadataProvider`，`MetadataServiceBase` 改为 `MetadataProviderBase`；keyed 注册移到 `IMetadataProvider`；`IMetadataService` 增加 `SwitchAsync` / `ActiveSource` / `AvailableSources`，provider 不再持有 TCS；`ClearCache` 广播给所有已创建 provider；同步更新数据流、DI、契约、实施与验收。 |
| 2026-11-22（一） | 同步 builder `cb1eb30`：曲线类别（`89ddba9`）已补齐，`LanguageMetadata` 变为 35 个字段、应用覆盖 30/30；新增 `GrowCurvePoint` 与三个曲线类别映射、`MetadataCategory.ProtoName`（`BydMaterial` ↔ `BeyondItem`，对应 `ProtoBuilder.LegacyJsonFileNames`）；更新包体积（CHS 3.60 MiB、15 语言 3.60–4.73 MiB、合并 59.7 MiB）与 JSON 树实测（19.2 MiB / 349.9 MiB）；§10.2 标记完成，`schema_version`、清单、语言目录、缺失类别致命仍待做；实施计划与验收改为 30 个适配器。 |
| 2026-11-21 | 初稿：现状、目标数据流、keyed 双源 + 门面、缓存与 `ClearCache`、传输方案、映射规则、分阶段计划。 |
