# Native AOT 阻塞项扫描报告

> 扫描日期：2026-11-08　分支：`feat/aot-compatible`　HEAD：`5f17088f9`（refactor(aot): remove Lib.Harmony）
> SDK：11.0.100-rc.2.26504.105　TFM：`net11.0-windows10.0.26100.0`　RID：`win-x64`　配置：Debug
> 依赖关键版本：Windows App SDK 2.6.6-experimental、CsWinRT 2.3.1、EF Core 11.0.0-rc.3、Roslyn 5.9.0
> 关联文档：[R2R 部署与 AOT 准备](aot-readiness.md)

本文回答一个问题：**这个仓库离 Native AOT 还差什么，差的部分能不能修。**
结论用一句话概括：**现在不能做 Native AOT——不是代码写得不够干净，而是 AOT 发布链路在 WinUI XAML 编译器里就断了；代码层另有 194 条可量化的诊断，以及 4 个需要产品决策的结构性依赖。**

---

## 0. 结论摘要

| # | 阻塞项 | 层级 | 规模 | 能否在仓库内解决 | 状态 |
|---|---|---|---|---|---|
| 1 | `PublishAot=true` 时 XAML 编译器拿到 **0 个引用**（`@(ReferencePath)` 为空）→ `WMC1007` | 工具链（MSBuild 目标顺序） | 1 个属性 | ✅ 已找到 workaround，见 §3.5 | **已解除**（2026-11-08） |
| 2 | `System.Text.Json` 反射序列化路径 | 代码 | 156 条 / 约 50 个文件 | ✅ 能，机械但量大 | 待修 |
| 3 | CommunityToolkit 按属性名排序 | 代码 | 15 条 / 2 个文件 | ✅ 能，改动很小 | 待修 |
| 4 | 运行期 `MakeGenericType` 构造 JSON 转换器 | 代码 | 9 条 / 4 个文件 | ✅ 能 | 待修 |
| 5 | EF Core：启动期迁移 + `DbContext` 构造 + 泛型集合注解 | 代码 + 设计 | 11 条 / 5 个文件 | ⚠️ 迁移需改设计 | 待决策 |
| 6 | 运行时 C# 脚本（Roslyn） | 外部组件 | 3 个入口 | ❌ 不能（AOT 下无解） | 需删除功能 |
| 7 | XAML：635 处 `{Binding}`、50 个无 `x:DataType` 的 `DataTemplate`、第三方控件库 | 框架 | 635 + 50 | ⚠️ 部分可改，部分看上游 | 未验证 |
| 8 | WebView2 + JS bridge | 外部组件 | 2 个窗口 / 3 个 bridge | ⚠️ 未验证 | 未验证 |
| 9 | M.E.AI / Anthropic SDK 的反射与 trim 警告 | 外部组件 | 3 处直接调用 | ⚠️ 部分可改 | 未验证 |

> 194 = 156（JSON）+ 15（排序）+ 9（转换器工厂）+ 11（EF）+ 2（生成的 XAML 代码）+ 1（DI 泛型）。

**一句话路线**：P0 只能等 Windows App SDK；P1 是 194 条里除 EF 迁移外的全部；P2 是脚本、迁移、`{Binding}` 这些产品/框架级取舍。

---

## 1. 背景与范围

### 1.1 已经完成的部分

这个分支不是从零开始。在本次扫描之前，仓库已经做了以下 AOT 准备（详见 [aot-readiness.md](aot-readiness.md)）：

- **R2R 已落地**：所有配置默认 `PublishReadyToRun`，构建与打包链路会校验最终 DLL 的 R2R 头。
- **插件系统已移除**（`0c055a326`）：`AssemblyLoadContext` + 动态加载是 AOT 的天然对立面。
- **`Lib.Harmony` 已移除**（`5f17088f9`）：运行期方法替换在 AOT 下不可用。
- **JSON 部分迁移**：设置文件、数据库 JSON 列、124 种传递闭包类型、枚举转换器已走源生成上下文。
- **EF 编译模型**：`Model/Entity/Database/Compiled` 已生成并接入。

所以"还有哪些不支持"必须建立在"哪些已经解决了"之上——否则会把已完成的活重算一遍。

### 1.2 本次扫描覆盖

- 应用工程 `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered`（全部 `.cs` / `.xaml` / `.csproj`）。
- 依赖图：应用工程直接 `PackageReference` 57 条，重点看与反射/动态代码相关的包。
- 不覆盖：测试工程（`net11.0`，不含 AOT 分析器）、Cake 脚本、安装器。

### 1.3 术语约定

| 诊断 | 含义 | 严重程度 |
|---|---|---|
| `IL2026` | 调用了标注 `[RequiresUnreferencedCode]` 的成员：裁剪后可能缺失元数据，运行期出现 `MissingMethodException` / 绑定失败 / 行为差异 | 高（运行时才炸） |
| `IL3050` | 调用了标注 `[RequiresDynamicCode]` 的成员：AOT 下**没有 JIT**，该路径必然不可用 | 最高（必然失败） |
| `IL2091` / `IL2055` / `IL2077` | 泛型实参或 `Type` 值不满足 `DynamicallyAccessedMembers` 契约：被裁剪后反射拿不到成员 | 中（取决于是否真的反射） |

**关键判读规则**：`IL3050` 不是"警告"，是"这段代码在 AOT 下不存在"。本次共 84 条。

---

## 2. 扫描方法

用三类互相独立的证据，避免只信分析器：

### 2.1 构建期分析器（覆盖得到的地方）

```powershell
dotnet build src/Snap.Hutao.Remastered/Snap.Hutao.Remastered.slnx -c Debug -p:AotAnalysis=true
```

`AotAnalysis=true` 会在 [csproj](../src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Snap.Hutao.Remastered.csproj) 中打开 `EnableAotAnalyzer`、`EnableTrimAnalyzer`、`CsWinRTAotWarningLevel=3`，但不裁剪、不改运行时行为。

结果：**0 error / 674 warning，耗时 5m12s**，其中 AOT 相关唯一诊断 **194 条 / 59 个文件**。

### 2.2 发布实验（覆盖"能不能产出 AOT 产物"）

| 命令 | 结果 | 说明 |
|---|---|---|
| `dotnet publish -p:PublishAot=true` | ❌ 数秒内失败 | `WMC9999`，XAML 编译器内部空引用 |
| 同上 + `-p:EnableBindingDiagnostics=false` | ❌ 失败 | `WMC1007`，无法解析 WinUI 类型元数据 |
| `dotnet publish -p:PublishTrimmed=true`（不启用 AOT） | ✅ 成功 | 241 条 trim 警告，8 个程序集 IL2104 |
| `dotnet publish`（基线，R2R 关闭） | ✅ 成功 | 证明前述失败由 AOT/诊断开关触发 |

### 2.3 静态检索（覆盖分析器看不到的地方）

分析器只认识"被标注的 API"。以下问题它看不到，必须靠检索：

- XAML 的 `{Binding}`（运行期属性路径解析）、无 `x:DataType` 的 `DataTemplate`；
- `ActivatorUtilities`、`Activator.CreateInstance`、`MakeGenericType`、`Serialize(obj, obj.GetType())` 这类"类型在运行期才知道"的写法；
- 原生/托管混合程序集（AOT 无法加载 C++/CLI）；
- 第三方 WinUI 控件库的 trim 警告。

### 2.4 局限（必须写在报告里）

- **"没有诊断" ≠ "AOT 可用"**：`{Binding}`、XAML 运行时、COM 互操作都不在分析器视野内。
- 只验证了 Debug/x64；Release 未跑（预期一致，但未证）。
- 没有真正启动过 AOT 产物（也启动不了），所以所有"运行期会怎样"都是推断，不是实测。
- 分析器对泛型方法只报调用点，不报"某个具体 `T` 实例化后缺元数据"。

---

## 3. 阻塞一：AOT 发布链路不通（P0，工具链）

### 3.1 复现

```powershell
dotnet publish src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Snap.Hutao.Remastered.csproj `
  -c Debug -r win-x64 -p:PublishAot=true -p:PublishReadyToRun=false -p:SelfContained=true
```

输出（完整日志见 `aot-publish-try.log`）：

```
Microsoft.UI.Xaml.Markup.Compiler.interop.targets(609,9): Xaml Internal Error error WMC9999:
未将对象引用设置到对象的实例。
```

加上 `-p:EnableBindingDiagnostics=false` 后（`aot-publish-try2.log`）：

```
Snap.Hutao.Remastered.csproj : XamlCompiler error WMC1007:
Cannot resolve metadata for WinUI types. Please ensure that Nuget Restore was successful.
```

### 3.2 失败点定位

失败发生在 **`MarkupCompilePass1`**，也就是 XAML 编译阶段——在 C# 编译、ILC、链接之前。也就是说：**AOT 编译根本没开始**，问题不在我们的代码。

`Microsoft.UI.Xaml.Markup.Compiler.interop.targets` 里与 AOT 相关的逻辑**只有一处**：

```xml
<!-- If marked as AOT compatible, warn for any bindings that are not code generated / compiled. -->
<EnableBindingDiagnostics Condition="'$(EnableBindingDiagnostics)' == '' and ('$(IsAotCompatible)' == 'true' or '$(PublishAot)' == 'true')">true</EnableBindingDiagnostics>
```

即：Windows App SDK 已经把 `PublishAot` 当作"要检查绑定是否可编译"的信号，说明上游**开始**考虑 AOT；但这条路径在 2.6.6-experimental 的 `XamlCompiler.exe` 里是坏的：

1. 默认（诊断开）→ 编译器在处理诊断时抛空引用，外层只拿到 `WMC9999`；
2. 关掉诊断 → 编译器继续走，但在 AOT 发布上下文中解析不到 WinUI 类型元数据（`WMC1007`）。

第 2 点与上游已知问题同源：[WindowsAppSDK#6394](https://github.com/microsoft/WindowsAppSDK/issues/6394)（Native AOT 发布不产出 `.xbf`/`.pri`）。

### 3.3 为什么现有 workaround 救不了

仓库里有一个 `UsePatchedXamlCompiler` 目标（[Build/PatchedXamlCompiler.targets](../src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Build/PatchedXamlCompiler.targets)，由 csproj 一行 `Import` 引入），作用是给 `XamlCompiler.exe` 的**资源名字符串**打字节补丁，绕开 [microsoft-ui-xaml#11157](https://github.com/microsoft/microsoft-ui-xaml/issues/11157)：

- 它只保证"编译器能正常输出诊断文本"，不修编译器在 AOT 上下文里的元数据解析；
- 它把补丁后的 exe 放到 `obj/patchedxamlcompiler`，再由 `XamlCompilerExePath` 指过去；AOT 发布走的是同一路径，所以补丁确实生效了——但失败点不在资源查找，而在更后面。

### 3.4 影响（已被 §3.5 的结论取代）

- 在 Windows App SDK 支持 AOT 发布之前，**任何代码级修复都无法端到端验证**（连产物都产不出来）。
- 这也解释了为什么 [aot-readiness.md](aot-readiness.md) 把目标定在 R2R：那是当时唯一可交付的形态。
- 结论不是"别修代码"：194 条诊断是确定债，修掉以后就能直接用；但**不要承诺"修完就能 AOT"**。

> 2026-11-08 追补：上面三条**已被推翻**。根因不是 Windows App SDK"不支持"，而是 `PublishAot=true` 时 MSBuild 目标顺序变化，`MarkupCompilePass1` 在 `ResolveReferences` 之前执行，导致 XAML 编译器拿到空的 `@(ReferencePath)`。加一个属性即可绕过，详见 §3.5 与附录 D。


---

### 3.5 根因与 workaround（2026-11-08 追补）

**根因**：`PublishAot=true` 时 `MarkupCompilePass1` 在 `ResolveReferences` 之前执行，XAML 编译器收到的 `@(ReferencePath)` 是**空集合**，于是报 `WMC1007: Cannot resolve metadata for WinUI types`。

用自定义 target 在 `MarkupCompilePass1` 之前 dump `@(ReferencePath)` 得到硬数据：

| 场景 | `@(ReferencePath)` 数量 | 结果 |
|---|---:|---|
| `dotnet build` | 314 | 通过 |
| `dotnet publish`（`PublishAot=false`） | 314 | 通过 |
| `dotnet publish`（`PublishAot=true`） | **0** | `WMC1007` |
| `PublishAot=true` ＋ 下面的 workaround | 314 | 通过，进入 ILC |

**workaround**（一行 MSBuild 属性）：

```powershell
dotnet publish src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Snap.Hutao.Remastered.csproj `
  -c Debug -r win-x64 -p:PublishAot=true -p:PublishReadyToRun=false -p:SelfContained=true `
  -p:MarkupCompilePass1DependsOn=ResolveReferences
```

也可以用同样的一行写进 csproj（条件写在 `PublishAot` 上，避免影响普通构建）。**仓库里的正式做法是放在独立的目标文件里**，见 [Build/AotPublish.targets](../src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Build/AotPublish.targets)：

```xml
<PropertyGroup Condition="'$(PublishAot)' == 'true'">
  <MarkupCompilePass1DependsOn>$(MarkupCompilePass1DependsOn);ResolveReferences</MarkupCompilePass1DependsOn>
</PropertyGroup>
```

该文件由 csproj 末尾一行 `<Import Project="Build\AotPublish.targets" />` 引入，同时处理第 2 个坑（AOT 发布丢 `Deployment.exe`，见 D.5）。普通构建完全不受影响：属性与两个 target 都带 `Condition="'$(PublishAot)' == 'true'"`。

**效果**：最小 WinUI 3 样例（3 个源文件）与主工程都通过了 XAML 编译并进入 ILC；主工程产出了 302,346,240 字节（≈288 MiB）的**原生** `Snap.Hutao.Remastered.exe`（无 `BSJB`、无 `coreclr.dll`），发布 `EXIT=0`。完整实验记录见附录 D。

## 4. 诊断总览

### 4.1 按诊断 ID

| 诊断 | 条数 | 含义 |
|---|---:|---|
| `IL2026` | 95 | 调用了 `RequiresUnreferencedCode` 成员 |
| `IL3050` | 84 | 调用了 `RequiresDynamicCode` 成员（AOT 下必然不可用） |
| `IL2091` | 8 | 泛型实参不满足 `DynamicallyAccessedMembers` |
| `IL2055` | 4 | `MakeGenericType` 的实参无法静态确定 |
| `IL2077` | 3 | `Type` 值来源不满足 `DynamicallyAccessedMembers` |
| **合计** | **194** | 59 个文件 |

> 构建日志里每条诊断会打印两次（392 行），去重后 194 条。统计脚本见附录 A。

### 4.2 按调用点

| 条数 | 诊断 | 调用/成员 |
|---:|---|---|
| 44 | IL2026, IL3050 | `JsonSerializer.Deserialize<TValue>(String, JsonSerializerOptions)` |
| 30 | IL2026, IL3050 | `JsonSerializer.Serialize<TValue>(TValue, JsonSerializerOptions)` |
| 15 | IL2026 | `CommunityToolkit...SortDescription(String, SortDirection, IComparer)` |
| 12 | IL2026, IL3050 | `JsonSerializer.SerializeAsync<TValue>(...)` |
| 12 | IL2026, IL3050 | `JsonSerializer.DeserializeAsync<TValue>(...)` |
| 12 | IL2026, IL3050 | `JsonArray.Add<T>(T)` |
| 8 | IL2026, IL3050 | `JsonSerializer.Deserialize<TValue>(Stream, ...)` |
| 7 | IL2091 | `TEntity`（EF 泛型集合） |
| 6 | IL2026, IL3050 | `JsonSerializer.Deserialize<TValue>(JsonElement, ...)` |
| 6 | IL2055, IL3050 | `Type.MakeGenericType(params Type[])` |
| 6 | IL2026, IL3050 | `JsonSerializer.Serialize<TValue>(Stream, ...)` |
| 6 | IL2026, IL3050 | `JsonSerializer.Deserialize<TValue>(ref Utf8JsonReader, ...)` |
| 4 | IL2026, IL3050 | `DefaultJsonTypeInfoResolver..ctor()` |
| 4 | IL2026, IL3050 | `JsonSerializer.Deserialize<TValue>(ReadOnlySpan<Byte>, ...)` |
| 4 | IL2026, IL3050 | `JsonSerializer.SerializeToUtf8Bytes<TValue>(...)` |
| 3 | IL2077 | `type`（转换器工厂里的 `Type`） |
| 2 | IL2026, IL3050 | `ReadFromJsonAsync<T>(...)` |
| 2 | IL2026, IL3050 | `JsonSerializer.Serialize<TValue>(Utf8JsonWriter, ...)` |
| 2 | IL2026, IL3050 | `JsonSerializer.Serialize(Object, Type, ...)` |
| 2 | IL2026, IL3050 | `JsonSerializer.SerializeToUtf8Bytes(Object, Type, ...)` |
| 2 | IL2026, IL3050 | `DbContext.DbContext(DbContextOptions)` |
| 1 | IL2091 | `T`（`ActivatorUtilities.CreateInstance<T>`） |
| 1 | IL2026 | `Microsoft.Xaml.Interactivity.EventTriggerBehavior..ctor()` |
| 1 | IL3050 | `CommunityToolkit.WinUI.EnumValuesExtension..ctor()` |
| 1 | IL3050 | `RelationalDatabaseFacadeExtensions.Migrate(DatabaseFacade)` |
| 1 | IL3050 | `RelationalDatabaseFacadeExtensions.GetPendingMigrations(DatabaseFacade)` |

**归并后：`System.Text.Json` + `HttpClient` JSON 相关 156 条（80%），其他 38 条。**

### 4.3 按目录

| 目录 | 条数 |
|---|---:|
| `UI/Xaml` | 16 |
| `Service/AI` | 16 |
| `Core/LifeCycle` | 14 |
| `ViewModel/AvatarProperty` | 10 |
| `Service/UIGF` | 10 |
| `Web/Bridge` | 10 |
| `Model/Metadata` | 8 |
| `Service/Metadata` | 8 |
| `Service/User` | 8 |
| `Service/Game` | 8 |
| `Service/Geetest` | 8 |
| `Service/Yae` | 6 |
| `Core/Database` | 6 |
| `Core/AI` | 6 |
| `ViewModel/Achievement` | 5 |
| `Core/Text` | 5 |
| `Service/Abstraction` | 5 |
| 其余 22 个目录 | 各 1–4 |
| `obj/.../XamlTypeInfo.g.cs`（生成代码） | 2 |

可以看到诊断**高度分散**：单个目录最多 16 条，说明不是"某个模块没做 AOT"，而是"JSON 统一入口没换 + 几处框架 API 用错"。

### 4.4 按文件（59 个，列出条数 ≥ 4 的 28 个）

| # | 文件 | 条数 | 诊断 | 主要调用 |
|---:|---|---:|---|---|
| 1 | `ViewModel/AvatarProperty/AvatarPropertySortDescriptions.cs` | 10 | IL2026×10 | `SortDescription(String, …)` |
| 2 | `UI/Xaml/View/Window/AccountInsightsWindow.Gacha.cs` | 8 | IL2026×4, IL3050×4 | `JsonArray.Add<T>` |
| 3 | `Service/Geetest/GeetestService.cs` | 8 | IL2026×4, IL3050×4 | `Deserialize<T>(String)`, `SerializeToUtf8Bytes<T>` |
| 4 | `Core/AI/EnumBox.cs` | 6 | IL2026×2, IL2055×1, IL3050×3 | `JsonSerializer`, `MakeGenericType` |
| 5 | `UI/Xaml/Control/Theme/LiquidGlassOverrideStore.cs` | 6 | IL2026×3, IL3050×3 | `Serialize<T>`, `Deserialize<T>` |
| 6 | `Service/User/UserVerificationService.cs` | 6 | IL2026×3, IL3050×3 | `Deserialize<T>`, `Serialize<T>` |
| 7 | `Service/Yae/PlayerStore/PlayerStoreParser.cs` | 6 | IL2026×3, IL3050×3 | `Serialize<T>` |
| 8 | `ViewModel/Achievement/AchievementViewModel.cs` | 5 | IL2026×5 | `SortDescription(String, …)` |
| 9 | `Service/AI/AgentConversationFileProvider.cs` | 4 | IL2026×2, IL3050×2 | `Serialize<T>(Stream)` |
| 10 | `Service/AI/Settings/JsonSettingsProvider.cs` | 4 | IL2026×2, IL3050×2 | `Serialize<T>(Stream)`, `Deserialize<T>(Stream)` |
| 11 | `Service/AccountInsights/AccountInsightsHistoryService.cs` | 4 | IL2026×2, IL3050×2 | `JsonArray.Add<T>` |
| 12 | `Service/Abstraction/Property/StructUsingJsonDbProperty.cs` | 4 | IL2026×2, IL3050×2 | `Serialize<T>`, `Deserialize<T>` |
| 13 | `Core/Diagnostics/DiagnosticsSettingsProvider.cs` | 4 | IL2026×2, IL3050×2 | `Serialize<T>`, `Deserialize<T>` |
| 14 | `Service/UIGF/UIGFService.cs` | 4 | IL2026×2, IL3050×2 | `Deserialize<T>` |
| 15 | `Service/Metadata/LegacyMetadataService.cs` | 4 | IL2026×2, IL3050×2 | `DeserializeAsync<T>` |
| 16 | `Service/Metadata/MetadataService.cs` | 4 | IL2026×2, IL3050×2 | `DeserializeAsync<T>` |
| 17 | `Service/Hutao/ObjectCacheRepository.cs` | 4 | IL2026×2, IL3050×2 | `Deserialize<T>`, `Serialize<T>` |
| 18 | `Service/Game/Package/Advanced/PackageOperation/GamePackagePredownloadOperation.cs` | 4 | IL2026×2, IL3050×2 | `SerializeAsync<T>` |
| 19 | `Core/Database/AdvancedDbCollectionView.cs` | 4 | IL2091×4 | `TEntity` |
| 20 | `Core/IO/ValueFileExtension.cs` | 4 | IL2026×2, IL3050×2 | `SerializeAsync<T>`, `DeserializeAsync<T>` |
| 21 | `Core/LifeCycle/InterProcess/FullTrust/PipeStreamExtension.cs` | 4 | IL2026×2, IL3050×2 | `SerializeToUtf8Bytes<T>` |
| 22 | `Web/Bridge/MiHoYoJSBridge.cs` | 4 | IL2026×2, IL3050×2 | `Serialize(Object, Type)` |
| 23 | `Core/Scripting/ScriptContext.cs` | 4 | IL2026×2, IL3050×2 | `Serialize<T>`, `Deserialize<T>` |
| 24 | `Core/LifeCycle/InterProcess/PipeStreamExtension.cs` | 4 | IL2026×2, IL3050×2 | `SerializeToUtf8Bytes(Object, Type)` |
| 25 | `Model/Metadata/Avatar/IdLevelParametersCollection.cs` | 4 | IL2026, IL2055, IL2077, IL3050 | `MakeGenericType` |
| 26 | `Model/Metadata/TypeValueCollection.cs` | 4 | IL2026, IL2055, IL2077, IL3050 | `MakeGenericType` |
| 27 | `ViewModel/Backpack/BackpackReliquaryItemView.cs` | 4 | IL2026×2, IL3050×2 | `DefaultJsonTypeInfoResolver` |
| 28 | `Core/Text/Json/Annotation/JsonEnumHandlingAttribute.cs` | 3 | IL2055, IL2077, IL3050 | `MakeGenericType` |

完整 59 个文件 × 194 条逐条清单见 **附录 C**。

---

## 5. 分类详解

### 5.1 JSON 反射路径（156 条，占 80%）

**根因**：公共入口 [Core/Text/Json/JsonOptions.cs](../src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Core/Text/Json/JsonOptions.cs) 用的是反射解析器：

```csharp
options.TypeInfoResolver = new DefaultJsonTypeInfoResolver
{
    Modifiers = { JsonTypeInfoResolvers.ResolveEnumType },
};
```

`DefaultJsonTypeInfoResolver` 本身标注 `RequiresUnreferencedCode` + `RequiresDynamicCode`；它下面挂着的所有 `JsonSerializer.*<T>(..., options)` 调用都会传染。

**为什么之前"部分迁移"没有消灭它**：

- 设置文件、数据库 JSON 列、元数据集合已经换成源生成上下文（`LocalSettingsJsonContext`、`PersistenceJsonContext`、`MetadataCollectionJsonContext` 等），这些路径上的实体类型有静态元数据；
- 但**网络 JSON、插件/脚本数据、缓存、UIGF、AI 会话、诊断设置**仍然把 `JsonOptions.Default` 当默认选项传进去，泛型 `T` 由调用方决定 → 分析器只能报"你调用了反射 API"。

**高风险子集**（这些不是"换个 options"能解决的）：

| 位置 | 问题 |
|---|---|
| [Core/LifeCycle/InterProcess/PipeStreamExtension.cs:56](../src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Core/LifeCycle/InterProcess/PipeStreamExtension.cs#L56) | `JsonSerializer.SerializeToUtf8Bytes(data, data.GetType())`：运行期类型，必须反射 |
| [Web/Bridge/MiHoYoJSBridge.cs:452](../src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Web/Bridge/MiHoYoJSBridge.cs#L452) | `Serialize(result, result.GetType())`：同上 |
| [Web/Enka/EnkaClient.cs:67](../src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Web/Enka/EnkaClient.cs#L67) | `ReadFromJsonAsync<EnkaResponse>`：`System.Net.Http.Json` 内部走反射 |
| `AccountInsightsWindow.Gacha.cs`、`AccountInsightsHistoryService.cs` | `JsonArray.Add<T>`（12 条）：`JsonNode` 的泛型 Add 也是反射序列化 |

**修法**：给每条路径配一个 `JsonSerializerContext`，把 `JsonOptions.Default` 从"公共默认值"降级为"仅插件/脚本兼容路径"，并在 AOT 版里直接禁用；运行期 `Type` 的两处改成"已知类型 switch + 显式上下文"。

2026-11-08 进展：已按域新增多个 `JsonSerializerContext`（`InterProcessJsonContext`、`UserJsonContext`、`UigfJsonContext` 等），由 `JsonSerializeContexts` 组合，
并新增 `JsonSerialize` 门面；直接调用 `JsonSerializer` 反射重载的应用代码改为先取 `JsonTypeInfo<T>`；
`JsonSerialize` 直接查询按域上下文，未命中时回退到调用方 options 的反射 resolver；`JsonOptions.Default` 保持反射 resolver，脚本窗口改用独立的 `JsonOptions.CreateReflection()`。
网络响应 DTO、元数据根类型和 AI 会话仍走兼容回退，需要后续按类型闭包登记后才能从 Native AOT 路径移除。

**规模**：156 处 / 约 50 个文件，机械但需要逐类型确认元数据闭包（历史教训：124 种类型的闭包是逐个核对出来的，见 aot-readiness.md §"已接入的 JSON 路径"）。

### 5.2 按属性名排序（15 条，最容易修）

`CommunityToolkit.WinUI.Collections.SortDescription(string propertyName, ...)` 用反射按名字找属性。分析器提示里直接给了替代方案：

- [ViewModel/AvatarProperty/AvatarPropertySortDescriptions.cs](../src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/ViewModel/AvatarProperty/AvatarPropertySortDescriptions.cs)（10 条，12–21 行）
- [ViewModel/Achievement/AchievementViewModel.cs](../src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/ViewModel/Achievement/AchievementViewModel.cs)（5 条，36–41 行）

修法二选一：改用 `SortDescription<T>`（编译期成员访问），或传入显式 `IComparer<T>`。**改动约 20 行，收益 15 条诊断**。

### 5.3 运行期构造泛型转换器（9 条，4 个文件）

| 位置 | 代码 |
|---|---|
| [Model/Metadata/TypeValueCollection.cs:52](../src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Model/Metadata/TypeValueCollection.cs#L52) | `Activator.CreateInstance(ConverterType.MakeGenericType(typeToConvert.GetGenericArguments()))` |
| [Model/Metadata/Avatar/IdLevelParametersCollection.cs:60](../src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Model/Metadata/Avatar/IdLevelParametersCollection.cs#L60) | 同上 |
| [Core/Text/Json/Annotation/JsonEnumHandlingAttribute.cs:31](../src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Core/Text/Json/Annotation/JsonEnumHandlingAttribute.cs#L31) | `UnsafeEnumConverterType.MakeGenericType(info.PropertyType)` |
| [Core/AI/EnumBox.cs:46](../src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Core/AI/EnumBox.cs#L46) | `typeof(EnumBoxJsonConverter<>).MakeGenericType(...)` |

这四处都是"自定义集合/枚举的 JSON 转换器工厂"。`MakeGenericType` 在 AOT 下只能对**静态可枚举的实例化**生效，靠 `Type` 变量的都不可靠（`IL2055`）。

修法：把工厂改成显式登记表（`switch (typeToConvert)` → 返回已构造好的转换器），或在源生成上下文里用 `[JsonConverter(typeof(...))]` 固定下来。**预期收益 9 条**。

### 5.4 EF Core（11 条 + 1 个设计问题）

**5.4.1 启动期迁移（设计问题，不是警告问题）**

[Core/DependencyInjection/ServiceCollectionExtension.cs:36-42](../src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Core/DependencyInjection/ServiceCollectionExtension.cs#L36-L42)：

```csharp
using (AppDbContext context = AppDbContext.Create(serviceProvider, sqlConnectionString))
{
    if (context.Database.GetPendingMigrations().Any())
    {
        context.Database.Migrate();
    }
}
```

`Migrate()` 与 `GetPendingMigrations()` 都是 `IL3050`。EF 官方立场是**迁移（Migrations）不支持 Native AOT**：迁移程序集靠反射发现 `Migration` 子类、动态构造 `MigrationBuilder`。这不是注解能修的。

选项：
1. 发行版改为"纯新增安装 + 版本化 SQL 脚本"，首次运行执行脚本而不是迁移；
2. 保留迁移但只在非 AOT 版启用（运行时按 `RuntimeFeature.IsDynamicCodeSupported` 分支）；
3. AOT 版首次启动检测到旧库时提示"请先用完整版升级"。

**5.4.2 `DbContext` 构造（2 条）**

[Model/Entity/Database/AppDbContext.cs:17](../src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Model/Entity/Database/AppDbContext.cs#L17) 触发 `DbContext(DbContextOptions)` 的 `RequiresUnreferencedCode`，EF 自己在消息里写得很清楚：

> EF Core isn't fully compatible with trimming, and running the application may generate unexpected runtime failures.

编译模型（`Compiled/`）解决的是"模型发现"，**不等于**"查询管道 AOT 安全"，更不等于"迁移可用"。这一点 aot-readiness.md 已经写明，但容易被误读成"EF 已经 OK"。

**5.4.3 泛型集合（7 条 `IL2091`）**

`RepositoryExtension`、`AdvancedDbCollectionView`、`ObservableReorderableDbCollection` 的 `TEntity` 没有标注 `[DynamicallyAccessedMembers]`，EF 内部要用实体元数据。修法是给泛型参数补注解并把契约一路传下去——**能消警告，但 EF 本身仍是 trim-unsafe**，属于"降噪"而不是"解决"。

### 5.5 XAML（分析器只报了 2 条，真实风险大得多）

分析器仅捕获生成的 `XamlTypeInfo.g.cs` 里两处反射构造（`EventTriggerBehavior`、`EnumValuesExtension`，均来自第三方 XAML 基础设施）。但 XAML 的真实 AOT 风险在别处：

| 指标 | 数量 | 含义 |
|---|---:|---|
| `{x:Bind}` | 1878 | 编译期绑定，AOT 友好 |
| `{Binding}` | **635** | 运行期属性路径解析，AOT 风险 |
| `DataTemplate` 总数 | 198 | — |
| 其中无 `x:DataType` | **50** | 模板内绑定无法编译，只能反射 |

Windows App SDK 自己的注释就是判据：

> If marked as AOT compatible, warn for any bindings that are not code generated / compiled.

也就是说：**`{Binding}` 在 AOT 语境下被上游明确视为"需要告警"的写法**。635 处分布在 55 个 XAML 文件里（`UI/` 下共 146 个 XAML 文件），最集中的是：

| 文件 | `{Binding}` 数 |
|---|---:|
| `UI/Xaml/View/Page/AnnouncementPage.xaml` | 77 |
| `UI/Xaml/Control/AgentSettingsView.xaml` | 44 |
| `UI/Xaml/Control/AgentWorkspaceView.xaml` | 39 |
| `UI/Xaml/View/GuideView.xaml` | 37 |
| `UI/Xaml/View/OverlayView.xaml` | 33 |
| `UI/Xaml/View/Dialog/CultivatePromotionDeltaDialog.xaml` | 30 |

此外还有第三方控件库：`DevWinUI`、`DevWinUI.Base` 在 trim 发布时直接产出 `IL2104`（程序集级 trim 警告），`WinUI.LiquidGlass`、CommunityToolkit Labs 系列未标注 AOT 兼容性。

**判断**：XAML 是"修不完但可以少踩"的一类。真正 AOT 前必须回答一个问题——WinUI 的绑定引擎在 AOT 下能否通过 `IXamlMetadataProvider` 解析 `{Binding}`。这需要上游给出结论，仓库侧无法自证。

### 5.6 运行时 C# 脚本（必须删的功能）

| 位置 | 用法 |
|---|---|
| [ViewModel/Scripting/ScriptingViewModel.cs:36-42](../src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/ViewModel/Scripting/ScriptingViewModel.cs#L36-L42) | `CSharpScript.Create(...)` + `InteractiveAssemblyLoader` |
| [ViewModel/TestViewModel.cs:391](../src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/ViewModel/TestViewModel.cs#L391) | `CSharpScript.EvaluateAsync(...)` |
| [Core/Scripting/ScriptContext.cs](../src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Core/Scripting/ScriptContext.cs) | 脚本全局上下文（含 JSON 序列化） |

Roslyn 脚本依赖 `Reflection.Emit`、动态程序集、`AssemblyLoadContext`，**AOT 下没有任何可行路径**。trim 发布时 `Microsoft.CodeAnalysis`、`Microsoft.CodeAnalysis.Scripting` 均报 `IL2104`。

这不是"改进"问题，是产品取舍：AOT 版必须去掉脚本窗口（`NotifyIconContextMenu` 的 `OpenScriptingWindowCommand`）。插件已经删了，脚本是同一个决策的另一半。

### 5.7 WebView2 / JS bridge（未验证）

- `Microsoft.Web.WebView2` 1.0.4314.35 + 2 个承载窗口（`WebView2Window`、`CompactWebView2Window`）；
- `MiHoYoJSBridge` / `SignInJSBridge` / `DailyJSBridge`：通过 `ActivatorUtilities.CreateInstance` 构造，内部 `JsonSerializer.Serialize(result, result.GetType())`（2 条诊断）；
- `GeetestService`（8 条）：base64 + 反射序列化的验证码数据。

WebView2 的 .NET 封装大量使用 COM 互操作与 `Marshal`。Native AOT 在 Windows 上支持内建 COM，但 `IDispatch` / late binding 与 `ComImport` 的边界需要实测。**本次未验证**，列为待办而不是结论。

### 5.8 AI 栈（未验证）

- [Service/AI/AgentService.cs:110-111](../src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Service/AI/AgentService.cs#L110-L111)：`AIFunctionFactory.Create(BuiltInFunctions.GetCurrentTime)` —— M.E.AI 通过反射读取委托签名生成 JSON schema；
- `Microsoft.Agents.AI.*` / `Anthropic` SDK：trim 发布时报 `IL2104`；
- `JsonSerializerOptionsKey.AgentConversation` 用 `AIJsonUtilities.DefaultOptions`（内部带 resolver，但语义未验证）。

M.E.AI 团队在做 AOT 支持，但当前版本不能假定可用。**这属于"跟随上游"的类别**。

### 5.9 其他第三方（需要单独验证，本次未下结论）

| 组件 | 关注点 |
|---|---|
| `Sentry` 7.0.0-rc.3 | 异常捕获与 breadcrumb；是否支持 AOT 需查版本文档 |
| `Quartz` 4.7.2 | 作业实例化/调度；`ShutdownAsync` 在退出路径上被同步等待 |
| `Snap.LibGit2Sharp` | 原生 libgit2 的 P/Invoke 包装，理论 AOT 可用 |
| `Microsoft.Graphics.Win2D` | WinRT 组件，依赖 CsWinRT 封送 |
| `System.Formats.Nrbf` | 纯托管解析，理论可用 |
| `Snap.Hutao.Remastered.Native` / `Snap.HPatch` | **纯原生 DLL（无 CLR 元数据）**，P/Invoke 可用 ✅ |
| `Snap.ZStandard` | 托管包装 + 原生 zstd，可用 ✅ |

> 原生 DLL 这一条是实测的：读取 PE 后确认 `Snap.Hutao.Remastered.Native.dll`、`Snap.HPatch.dll` 不含 `BSJB`（CLI 元数据）签名。**只要没有 C++/CLI 混合程序集，AOT 就不会因为"加载不了程序集"整体失败**——这是本次扫描的正面结论之一。

---

## 6. 已经 AOT 友好的部分（不要重复投入）

| 项 | 证据 |
|---|---|
| 插件系统已移除 | `0c055a326`；`API/`、`Service/Plugin/` 不再参与运行时加载 |
| `Lib.Harmony` 已移除 | `5f17088f9` |
| EF 编译模型 | `Model/Entity/Database/Compiled`（25 实体） |
| 设置 / DB JSON 已源生成 | `LocalSettingsJsonContext`、`PersistenceJsonContext`、`LocalSettingPlatformJsonContext` |
| 枚举转换器已封闭 | `JsonStringEnumConverter<TEnum>`、`UnsafeEnumConverter<TEnum>` |
| 元数据集合已源生成 | `MetadataCollectionJsonContext` + 3 组静态封闭转换器 |
| XAML 类型表是编译期生成的 | `XamlTypeInfo.g.cs` 中无 `Activator` / `GetProperty`，全部是 `typeof` 表 |
| 原生依赖是纯原生 | `Snap.Hutao.Remastered.Native.dll`、`Snap.HPatch.dll` 无 CLI 元数据 |
| R2R 已稳定 | 构建/发布/打包三处校验 DLL 头 |

---

## 7. 分析器看不到的风险（盲区）

这部分必须写进结论，否则会高估"194 条修完就 OK"：

1. **`{Binding}` 的 635 处**：分析器只在 `PublishAot` 打开时通过 XAML 编译器告警，而那条路径当前是坏的（见 §3），所以**没有任何工具能告诉我们这 635 处会不会坏**。
2. **XAML 运行时本身**：`Application.Start` → `InitializeComponent` → XBF 加载 → 控件实例化，链条在 `Microsoft.UI.Xaml.dll`（原生 C++）里，AOT 是否能跑通取决于上游。
3. **COM 互操作**：`[ComImport]`、`Marshal`、WebView2、WinRT 封送。CsWinRT 在 `CsWinRTAotWarningLevel=3` 下**没有报任何警告**，这是好信号，但只覆盖投影层，不覆盖 WebView2 的原生回调。
4. **`ActivatorUtilities`**：[ContentDialogFactory.cs:103](../src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Factory/ContentDialog/ContentDialogFactory.cs#L103) 等 4 处，通过 DI 反射构造带额外参数的实例；只报了 1 条 `IL2091`，因为泛型参数没被标注。
5. **`Convert.ChangeType`**：[Core/Setting/LocalSetting.cs:177](../src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Core/Setting/LocalSetting.cs#L177) 的类型转换依赖 `IConvertible`，AOT 下可用但语义需验证。

---

## 8. 修复路线图

按"收益 / 风险 / 是否被上游阻塞"排序。

### P0：先让 AOT 发布能跑起来（被上游阻塞，仓库侧只能等/绕）

| 动作 | 说明 |
|---|---|
| 跟踪 Windows App SDK 的 AOT 进展 | 关注 `EnableBindingDiagnostics` 路径的修复与 `.xbf`/`.pri` 打包（[#6394](https://github.com/microsoft/WindowsAppSDK/issues/6394)） |
| 建立"能否 AOT 发布"的冒烟任务 | 不要求成功，要求**失败点可比较**：记录每次尝试的 error code + 阶段，便于上游修复后立刻验证 |
| 不要在仓库里"伪造" AOT | 例如 `PublishTrimmed=false` + `PublishAot=true` 这类组合、或用手写描述符掩盖问题 |

### P1：194 条里除 EF 迁移外的全部（可在仓库内完成）

| 优先级 | 任务 | 诊断数 | 预估工作量 | 风险 |
|---|---|---:|---|---|
| P1-1 | 按属性名排序 → `SortDescription<T>` / `IComparer` | 15 | 小（2 文件，约 20 行） | 低 |
| P1-2 | 4 个泛型转换器工厂改成显式登记 | 9 | 中（4 文件，需回归 JSON 兼容性） | 中（改错会破坏旧数据读写） |
| P1-3 | 网络/缓存/UIGF/AI 的 JSON 走源生成上下文 | 约 130 | 大（约 50 文件） | 中（类型闭包遗漏 → 运行期 `NotSupportedException`） |
| P1-4 | 两处 `Serialize(obj, obj.GetType())` 改为已知类型分派 | 4 | 小 | 低 |
| P1-5 | EF 泛型补 `DynamicallyAccessedMembers` | 7 | 小（但只能降噪） | 低 |

**P1 做完的判据**：`AotAnalysis=true` 下 `IL2026/IL3050` 归零或只剩白名单，且**测试全绿**（尤其 `AotJsonCodecTest`、`LocalSettingsJsonTest`、`AgentMigrationTest` 这类跨版本读写的用例）。

### P2：结构性取舍（需要产品决策）

| 任务 | 决策点 | 影响面 |
|---|---|---|
| 删除运行时脚本 | AOT 版是否放弃"写 C# 脚本"能力 | 脚本窗口、`TestViewModel`、`ScriptContext` |
| EF 迁移改 SQL 脚本 | 旧库升级路径 | 首次启动、升级体验、回滚 |
| `{Binding}` → `{x:Bind}` | 635 处，是否值得 | 全部 XAML 视图 |
| WebView2 窗口 | AOT 版是否保留内嵌浏览器窗口 | 2 个窗口 + 3 个 bridge |
| 第三方控件库 | `DevWinUI` 等是否换成自绘/官方控件 | 主题、外观 |

### P3：可选优化

- `AddDbContextPool` + 编译模型已就绪，但查询预编译（`--precompile-queries`）此前因 XAML 设计时编译冲突失败，见 aot-readiness.md；AOT 版需要解耦设计时编译。

---

## 9. 验收标准（Definition of Done）

要宣称"支持 Native AOT"，至少同时满足：

1. `dotnet publish -p:PublishAot=true` 产出 exe，且输出目录含 `.xbf`、`.pri`、`Assets/`、`Resource/`；
2. ILC **0 error**，warning 全部有归属（要么修掉，要么有书面说明与上游 issue 链接）；
3. 产物在**没有 CoreCLR** 的机器上启动（`dotnet --list-runtimes` 看不到对应运行时也能跑）；
4. 冒烟路径全过：启动 → 数据库迁移（或 SQL 升级）→ 元数据初始化 → 签到 → 抽卡记录导入导出（UIGF）→ 背包 → 深境螺旋 → AI 对话 → 通知 → 退出；
5. MSIX 打包 + 安装 + 卸载验证；
6. 与 R2R 版对比：启动时间、安装体积、内存。**没有对比数据就不要写"启动更快"**；
7. 至少一轮"旧数据升级"验证（旧 `Userdata.db` + 旧设置文件）。

---

## 10. 附录 A：复现命令与日志

```powershell
# 1) AOT 分析器（不裁剪、不改行为）
dotnet build src/Snap.Hutao.Remastered/Snap.Hutao.Remastered.slnx -c Debug -p:AotAnalysis=true

# 2) AOT 发布尝试（预期失败：WMC9999）
dotnet publish src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Snap.Hutao.Remastered.csproj `
  -c Debug -r win-x64 -p:PublishAot=true -p:PublishReadyToRun=false -p:SelfContained=true

# 3) 关掉绑定诊断重试（预期失败：WMC1007）
#    在上面基础上加 -p:EnableBindingDiagnostics=false

# 4) 只裁剪（预期成功，用于观察 trim 警告）
dotnet publish src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Snap.Hutao.Remastered.csproj `
  -c Debug -r win-x64 -p:PublishTrimmed=true -p:PublishReadyToRun=false -p:SelfContained=true
```

本次日志：

| 文件 | 内容 |
|---|---|
| `aot-scan-current.log` | 分析器构建完整日志（674 warning / 0 error） |
| `aot-publish-try.log` | `PublishAot=true` 失败现场 |
| `aot-publish-try2.log` | 关绑定诊断后的失败现场 |
| `aot-publish-trimonly.log` | 仅裁剪的成功发布 + 241 条 trim 警告 |
| `aot-publish-baseline.log` | 基线发布 |

统计口径：从 `aot-scan-current.log` 中提取 `warning IL####` 行，按 `(ID, 文件, 行号)` 去重；构建日志每条打印两次，去重前 392 行。

---

## 11. 附录 B：相关文件索引

| 主题 | 文件 |
|---|---|
| R2R 现状 | [aot-readiness.md](aot-readiness.md) |
| AOT 分析器开关 | [Snap.Hutao.Remastered.csproj](../src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Snap.Hutao.Remastered.csproj)（54–58 行） |
| XAML 编译器补丁 | [Build/PatchedXamlCompiler.targets](../src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Build/PatchedXamlCompiler.targets)（`UsePatchedXamlCompiler`） |
| 公共 JSON 选项 | [Core/Text/Json/JsonOptions.cs](../src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Core/Text/Json/JsonOptions.cs) |
| 源生成 JSON 上下文 | `Core/Text/Json/*JsonContext.cs` |
| EF 编译模型 | `Model/Entity/Database/Compiled/` |
| 启动期迁移 | [Core/DependencyInjection/ServiceCollectionExtension.cs](../src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Core/DependencyInjection/ServiceCollectionExtension.cs)（25–60 行） |
| 运行时脚本 | [ViewModel/Scripting/ScriptingViewModel.cs](../src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/ViewModel/Scripting/ScriptingViewModel.cs) |
| 应用入口 | [Bootstrap.cs](../src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Bootstrap.cs)（`Application.Start`） |
| AOT 发布补丁 | [Build/AotPublish.targets](../src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Build/AotPublish.targets)（XAML 目标顺序 + 还原 `Deployment.exe` 载荷） |

---

## 12. 附录 C：逐条诊断清单（194 条）

| # | ID | 位置 | 调用/成员 |
|---:|---|---|---|
| 1 | IL2055 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Core/AI/EnumBox.cs:46` | `System.Type.MakeGenericType(params Type[])` |
| 2 | IL3050 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Core/AI/EnumBox.cs:46` | `System.Type.MakeGenericType(params Type[])` |
| 3 | IL2026 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Core/AI/EnumBox.cs:55` | `System.Text.Json.JsonSerializer.Deserialize<TValue>(ref Utf8JsonReader, JsonSerializerOptions)` |
| 4 | IL3050 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Core/AI/EnumBox.cs:55` | `System.Text.Json.JsonSerializer.Deserialize<TValue>(ref Utf8JsonReader, JsonSerializerOptions)` |
| 5 | IL2026 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Core/AI/EnumBox.cs:60` | `System.Text.Json.JsonSerializer.Serialize<TValue>(Utf8JsonWriter, TValue, JsonSerializerOptions)` |
| 6 | IL3050 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Core/AI/EnumBox.cs:60` | `System.Text.Json.JsonSerializer.Serialize<TValue>(Utf8JsonWriter, TValue, JsonSerializerOptions)` |
| 7 | IL2091 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Core/Database/AdvancedDbCollectionView.cs:53` | `TEntity` |
| 8 | IL2091 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Core/Database/AdvancedDbCollectionView.cs:57` | `TEntity` |
| 9 | IL2091 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Core/Database/AdvancedDbCollectionView.cs:129` | `TEntity` |
| 10 | IL2091 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Core/Database/AdvancedDbCollectionView.cs:133` | `TEntity` |
| 11 | IL2091 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Core/Database/ObservableReorderableDbCollection.cs:74` | `TEntity` |
| 12 | IL2091 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Core/Database/ObservableReorderableDbCollection.cs:126` | `TEntity` |
| 13 | IL2026 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Core/DataTransfer/DefaultClipboardSource.cs:32` | `System.Text.Json.JsonSerializer.Deserialize<TValue>(String, JsonSerializerOptions)` |
| 14 | IL3050 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Core/DataTransfer/DefaultClipboardSource.cs:32` | `System.Text.Json.JsonSerializer.Deserialize<TValue>(String, JsonSerializerOptions)` |
| 15 | IL3050 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Core/DependencyInjection/ServiceCollectionExtension.cs:38` | `Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions.GetPendingMigrations(DatabaseFacade)` |
| 16 | IL3050 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Core/DependencyInjection/ServiceCollectionExtension.cs:41` | `Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions.Migrate(DatabaseFacade)` |
| 17 | IL2026 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Core/Diagnostics/DiagnosticsSettingsProvider.cs:80` | `System.Text.Json.JsonSerializer.Deserialize<TValue>(String, JsonSerializerOptions)` |
| 18 | IL3050 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Core/Diagnostics/DiagnosticsSettingsProvider.cs:80` | `System.Text.Json.JsonSerializer.Deserialize<TValue>(String, JsonSerializerOptions)` |
| 19 | IL2026 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Core/Diagnostics/DiagnosticsSettingsProvider.cs:110` | `System.Text.Json.JsonSerializer.Serialize<TValue>(TValue, JsonSerializerOptions)` |
| 20 | IL3050 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Core/Diagnostics/DiagnosticsSettingsProvider.cs:110` | `System.Text.Json.JsonSerializer.Serialize<TValue>(TValue, JsonSerializerOptions)` |
| 21 | IL2026 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Core/IO/ValueFileExtension.cs:21` | `System.Text.Json.JsonSerializer.DeserializeAsync<TValue>(Stream, JsonSerializerOptions, CancellationToken)` |
| 22 | IL3050 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Core/IO/ValueFileExtension.cs:21` | `System.Text.Json.JsonSerializer.DeserializeAsync<TValue>(Stream, JsonSerializerOptions, CancellationToken)` |
| 23 | IL2026 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Core/IO/ValueFileExtension.cs:38` | `System.Text.Json.JsonSerializer.SerializeAsync<TValue>(Stream, TValue, JsonSerializerOptions, CancellationToken)` |
| 24 | IL3050 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Core/IO/ValueFileExtension.cs:38` | `System.Text.Json.JsonSerializer.SerializeAsync<TValue>(Stream, TValue, JsonSerializerOptions, CancellationToken)` |
| 25 | IL2026 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Core/LifeCycle/InterProcess/BetterGenshinImpact/BetterGenshinImpactNamedPipeClient.cs:174` | `System.Text.Json.JsonSerializer.Deserialize<TValue>(JsonElement, JsonSerializerOptions)` |
| 26 | IL3050 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Core/LifeCycle/InterProcess/BetterGenshinImpact/BetterGenshinImpactNamedPipeClient.cs:174` | `System.Text.Json.JsonSerializer.Deserialize<TValue>(JsonElement, JsonSerializerOptions)` |
| 27 | IL2026 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Core/LifeCycle/InterProcess/BetterGenshinImpact/Handler/PipeRequestHandler.cs:19` | `System.Text.Json.JsonSerializer.Deserialize<TValue>(JsonElement, JsonSerializerOptions)` |
| 28 | IL3050 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Core/LifeCycle/InterProcess/BetterGenshinImpact/Handler/PipeRequestHandler.cs:19` | `System.Text.Json.JsonSerializer.Deserialize<TValue>(JsonElement, JsonSerializerOptions)` |
| 29 | IL2026 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Core/LifeCycle/InterProcess/FullTrust/PipeStreamExtension.cs:23` | `System.Text.Json.JsonSerializer.Deserialize<TValue>(ReadOnlySpan<Byte>, JsonSerializerOptions)` |
| 30 | IL3050 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Core/LifeCycle/InterProcess/FullTrust/PipeStreamExtension.cs:23` | `System.Text.Json.JsonSerializer.Deserialize<TValue>(ReadOnlySpan<Byte>, JsonSerializerOptions)` |
| 31 | IL2026 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Core/LifeCycle/InterProcess/FullTrust/PipeStreamExtension.cs:55` | `System.Text.Json.JsonSerializer.SerializeToUtf8Bytes<TValue>(TValue, JsonSerializerOptions)` |
| 32 | IL3050 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Core/LifeCycle/InterProcess/FullTrust/PipeStreamExtension.cs:55` | `System.Text.Json.JsonSerializer.SerializeToUtf8Bytes<TValue>(TValue, JsonSerializerOptions)` |
| 33 | IL2026 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Core/LifeCycle/InterProcess/PipeStreamExtension.cs:23` | `System.Text.Json.JsonSerializer.Deserialize<TValue>(ReadOnlySpan<Byte>, JsonSerializerOptions)` |
| 34 | IL3050 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Core/LifeCycle/InterProcess/PipeStreamExtension.cs:23` | `System.Text.Json.JsonSerializer.Deserialize<TValue>(ReadOnlySpan<Byte>, JsonSerializerOptions)` |
| 35 | IL2026 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Core/LifeCycle/InterProcess/PipeStreamExtension.cs:56` | `System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(Object, Type, JsonSerializerOptions)` |
| 36 | IL3050 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Core/LifeCycle/InterProcess/PipeStreamExtension.cs:56` | `System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(Object, Type, JsonSerializerOptions)` |
| 37 | IL2026 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Core/LifeCycle/InterProcess/ToastNotificationPipeClient.cs:24` | `System.Text.Json.JsonSerializer.Deserialize<TValue>(Stream, JsonSerializerOptions)` |
| 38 | IL3050 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Core/LifeCycle/InterProcess/ToastNotificationPipeClient.cs:24` | `System.Text.Json.JsonSerializer.Deserialize<TValue>(Stream, JsonSerializerOptions)` |
| 39 | IL2026 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Core/Scripting/ScriptContext.cs:29` | `System.Text.Json.JsonSerializer.Deserialize<TValue>(String, JsonSerializerOptions)` |
| 40 | IL3050 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Core/Scripting/ScriptContext.cs:29` | `System.Text.Json.JsonSerializer.Deserialize<TValue>(String, JsonSerializerOptions)` |
| 41 | IL2026 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Core/Scripting/ScriptContext.cs:34` | `System.Text.Json.JsonSerializer.Serialize<TValue>(TValue, JsonSerializerOptions)` |
| 42 | IL3050 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Core/Scripting/ScriptContext.cs:34` | `System.Text.Json.JsonSerializer.Serialize<TValue>(TValue, JsonSerializerOptions)` |
| 43 | IL2055 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Core/Text/Json/Annotation/JsonEnumHandlingAttribute.cs:31` | `System.Type.MakeGenericType(params Type[])` |
| 44 | IL3050 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Core/Text/Json/Annotation/JsonEnumHandlingAttribute.cs:31` | `System.Type.MakeGenericType(params Type[])` |
| 45 | IL2077 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Core/Text/Json/Annotation/JsonEnumHandlingAttribute.cs:32` | `type` |
| 46 | IL2026 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Core/Text/Json/JsonOptions.cs:15` | `System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver.DefaultJsonTypeInfoResolver()` |
| 47 | IL3050 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Core/Text/Json/JsonOptions.cs:15` | `System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver.DefaultJsonTypeInfoResolver()` |
| 48 | IL2091 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Factory/ContentDialog/ContentDialogFactory.cs:103` | `T` |
| 49 | IL2026 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Model/Entity/Database/AppDbContext.cs:17` | `Microsoft.EntityFrameworkCore.DbContext.DbContext(DbContextOptions)` |
| 50 | IL3050 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Model/Entity/Database/AppDbContext.cs:17` | `Microsoft.EntityFrameworkCore.DbContext.DbContext(DbContextOptions)` |
| 51 | IL2055 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Model/Metadata/Avatar/IdLevelParametersCollection.cs:60` | `System.Type.MakeGenericType(params Type[])` |
| 52 | IL2077 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Model/Metadata/Avatar/IdLevelParametersCollection.cs:60` | `type` |
| 53 | IL2026 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Model/Metadata/Avatar/IdLevelParametersCollection.cs:74` | `System.Text.Json.JsonSerializer.Deserialize<TValue>(ref Utf8JsonReader, JsonSerializerOptions)` |
| 54 | IL3050 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Model/Metadata/Avatar/IdLevelParametersCollection.cs:74` | `System.Text.Json.JsonSerializer.Deserialize<TValue>(ref Utf8JsonReader, JsonSerializerOptions)` |
| 55 | IL2055 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Model/Metadata/TypeValueCollection.cs:52` | `System.Type.MakeGenericType(params Type[])` |
| 56 | IL2077 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Model/Metadata/TypeValueCollection.cs:52` | `type` |
| 57 | IL2026 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Model/Metadata/TypeValueCollection.cs:65` | `System.Text.Json.JsonSerializer.Deserialize<TValue>(ref Utf8JsonReader, JsonSerializerOptions)` |
| 58 | IL3050 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Model/Metadata/TypeValueCollection.cs:65` | `System.Text.Json.JsonSerializer.Deserialize<TValue>(ref Utf8JsonReader, JsonSerializerOptions)` |
| 59 | IL2026 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/obj/x64/Debug/net11.0-windows10.0.26100.0/win-x64/XamlTypeInfo.g.cs:2127` | `Microsoft.Xaml.Interactivity.EventTriggerBehavior.EventTriggerBehavior()` |
| 60 | IL3050 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/obj/x64/Debug/net11.0-windows10.0.26100.0/win-x64/XamlTypeInfo.g.cs:2248` | `CommunityToolkit.WinUI.EnumValuesExtension.EnumValuesExtension()` |
| 61 | IL2026 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Service/Abstraction/Property/StructUsingJsonDbProperty.cs:23` | `System.Text.Json.JsonSerializer.Deserialize<TValue>(String, JsonSerializerOptions)` |
| 62 | IL3050 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Service/Abstraction/Property/StructUsingJsonDbProperty.cs:23` | `System.Text.Json.JsonSerializer.Deserialize<TValue>(String, JsonSerializerOptions)` |
| 63 | IL2026 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Service/Abstraction/Property/StructUsingJsonDbProperty.cs:28` | `System.Text.Json.JsonSerializer.Serialize<TValue>(TValue, JsonSerializerOptions)` |
| 64 | IL3050 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Service/Abstraction/Property/StructUsingJsonDbProperty.cs:28` | `System.Text.Json.JsonSerializer.Serialize<TValue>(TValue, JsonSerializerOptions)` |
| 65 | IL2091 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Service/Abstraction/RepositoryExtension.cs:21` | `TEntity` |
| 66 | IL2026 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Service/AccountInsights/AccountInsightsHistoryService.cs:155` | `System.Text.Json.Nodes.JsonArray.Add<T>(T)` |
| 67 | IL3050 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Service/AccountInsights/AccountInsightsHistoryService.cs:155` | `System.Text.Json.Nodes.JsonArray.Add<T>(T)` |
| 68 | IL2026 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Service/AccountInsights/AccountInsightsHistoryService.cs:169` | `System.Text.Json.Nodes.JsonArray.Add<T>(T)` |
| 69 | IL3050 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Service/AccountInsights/AccountInsightsHistoryService.cs:169` | `System.Text.Json.Nodes.JsonArray.Add<T>(T)` |
| 70 | IL2026 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Service/AI/AgentConversationFileProvider.cs:52` | `System.Text.Json.JsonSerializer.Deserialize<TValue>(Stream, JsonSerializerOptions)` |
| 71 | IL3050 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Service/AI/AgentConversationFileProvider.cs:52` | `System.Text.Json.JsonSerializer.Deserialize<TValue>(Stream, JsonSerializerOptions)` |
| 72 | IL2026 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Service/AI/AgentConversationFileProvider.cs:77` | `System.Text.Json.JsonSerializer.Serialize<TValue>(Stream, TValue, JsonSerializerOptions)` |
| 73 | IL3050 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Service/AI/AgentConversationFileProvider.cs:77` | `System.Text.Json.JsonSerializer.Serialize<TValue>(Stream, TValue, JsonSerializerOptions)` |
| 74 | IL2026 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Service/AI/Migration/AgentLegacyMigration.cs:123` | `System.Text.Json.JsonSerializer.Deserialize<TValue>(Stream, JsonSerializerOptions)` |
| 75 | IL3050 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Service/AI/Migration/AgentLegacyMigration.cs:123` | `System.Text.Json.JsonSerializer.Deserialize<TValue>(Stream, JsonSerializerOptions)` |
| 76 | IL2026 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Service/AI/Observables/ObservableFunctionCallContent.cs:51` | `System.Text.Json.JsonSerializer.Serialize<TValue>(TValue, JsonSerializerOptions)` |
| 77 | IL3050 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Service/AI/Observables/ObservableFunctionCallContent.cs:51` | `System.Text.Json.JsonSerializer.Serialize<TValue>(TValue, JsonSerializerOptions)` |
| 78 | IL2026 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Service/AI/Observables/ObservableFunctionResultContent.cs:38` | `System.Text.Json.JsonSerializer.Serialize<TValue>(TValue, JsonSerializerOptions)` |
| 79 | IL3050 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Service/AI/Observables/ObservableFunctionResultContent.cs:38` | `System.Text.Json.JsonSerializer.Serialize<TValue>(TValue, JsonSerializerOptions)` |
| 80 | IL2026 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Service/AI/Observables/ObservableMcpServerToolCallContent.cs:41` | `System.Text.Json.JsonSerializer.Serialize<TValue>(TValue, JsonSerializerOptions)` |
| 81 | IL3050 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Service/AI/Observables/ObservableMcpServerToolCallContent.cs:41` | `System.Text.Json.JsonSerializer.Serialize<TValue>(TValue, JsonSerializerOptions)` |
| 82 | IL2026 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Service/AI/Settings/JsonSettingsProvider.cs:312` | `System.Text.Json.JsonSerializer.Deserialize<TValue>(Stream, JsonSerializerOptions)` |
| 83 | IL3050 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Service/AI/Settings/JsonSettingsProvider.cs:312` | `System.Text.Json.JsonSerializer.Deserialize<TValue>(Stream, JsonSerializerOptions)` |
| 84 | IL2026 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Service/AI/Settings/JsonSettingsProvider.cs:371` | `System.Text.Json.JsonSerializer.Serialize<TValue>(Stream, TValue, JsonSerializerOptions)` |
| 85 | IL3050 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Service/AI/Settings/JsonSettingsProvider.cs:371` | `System.Text.Json.JsonSerializer.Serialize<TValue>(Stream, TValue, JsonSerializerOptions)` |
| 86 | IL2026 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Service/FileUnlocker/FileUnlockerService.cs:36` | `System.Text.Json.JsonSerializer.Deserialize<TValue>(String, JsonSerializerOptions)` |
| 87 | IL3050 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Service/FileUnlocker/FileUnlockerService.cs:36` | `System.Text.Json.JsonSerializer.Deserialize<TValue>(String, JsonSerializerOptions)` |
| 88 | IL2026 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Service/Game/Account/RegistryInterop.cs:69` | `System.Text.Json.JsonSerializer.Deserialize<TValue>(String, JsonSerializerOptions)` |
| 89 | IL3050 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Service/Game/Account/RegistryInterop.cs:69` | `System.Text.Json.JsonSerializer.Deserialize<TValue>(String, JsonSerializerOptions)` |
| 90 | IL2026 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Service/Game/Package/Advanced/AssetOperation/GameAssetOperation.cs:66` | `System.Text.Json.JsonSerializer.Deserialize<TValue>(String, JsonSerializerOptions)` |
| 91 | IL3050 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Service/Game/Package/Advanced/AssetOperation/GameAssetOperation.cs:66` | `System.Text.Json.JsonSerializer.Deserialize<TValue>(String, JsonSerializerOptions)` |
| 92 | IL2026 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Service/Game/Package/Advanced/PackageOperation/GamePackagePredownloadOperation.cs:38` | `System.Text.Json.JsonSerializer.SerializeAsync<TValue>(Stream, TValue, JsonSerializerOptions, CancellationToken)` |
| 93 | IL3050 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Service/Game/Package/Advanced/PackageOperation/GamePackagePredownloadOperation.cs:38` | `System.Text.Json.JsonSerializer.SerializeAsync<TValue>(Stream, TValue, JsonSerializerOptions, CancellationToken)` |
| 94 | IL2026 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Service/Game/Package/Advanced/PackageOperation/GamePackagePredownloadOperation.cs:53` | `System.Text.Json.JsonSerializer.SerializeAsync<TValue>(Stream, TValue, JsonSerializerOptions, CancellationToken)` |
| 95 | IL3050 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Service/Game/Package/Advanced/PackageOperation/GamePackagePredownloadOperation.cs:53` | `System.Text.Json.JsonSerializer.SerializeAsync<TValue>(Stream, TValue, JsonSerializerOptions, CancellationToken)` |
| 96 | IL2026 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Service/Geetest/GeetestService.cs:43` | `System.Text.Json.JsonSerializer.Deserialize<TValue>(String, JsonSerializerOptions)` |
| 97 | IL3050 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Service/Geetest/GeetestService.cs:43` | `System.Text.Json.JsonSerializer.Deserialize<TValue>(String, JsonSerializerOptions)` |
| 98 | IL2026 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Service/Geetest/GeetestService.cs:83` | `System.Text.Json.JsonSerializer.Deserialize<TValue>(String, JsonSerializerOptions)` |
| 99 | IL3050 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Service/Geetest/GeetestService.cs:83` | `System.Text.Json.JsonSerializer.Deserialize<TValue>(String, JsonSerializerOptions)` |
| 100 | IL2026 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Service/Geetest/GeetestService.cs:86` | `System.Text.Json.JsonSerializer.Deserialize<TValue>(String, JsonSerializerOptions)` |
| 101 | IL3050 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Service/Geetest/GeetestService.cs:86` | `System.Text.Json.JsonSerializer.Deserialize<TValue>(String, JsonSerializerOptions)` |
| 102 | IL2026 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Service/Geetest/GeetestService.cs:97` | `System.Text.Json.JsonSerializer.SerializeToUtf8Bytes<TValue>(TValue, JsonSerializerOptions)` |
| 103 | IL3050 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Service/Geetest/GeetestService.cs:97` | `System.Text.Json.JsonSerializer.SerializeToUtf8Bytes<TValue>(TValue, JsonSerializerOptions)` |
| 104 | IL2026 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Service/Git/BuiltInRepositoryCatalog.cs:27` | `System.Text.Json.JsonSerializer.Deserialize<TValue>(String, JsonSerializerOptions)` |
| 105 | IL3050 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Service/Git/BuiltInRepositoryCatalog.cs:27` | `System.Text.Json.JsonSerializer.Deserialize<TValue>(String, JsonSerializerOptions)` |
| 106 | IL2026 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Service/Hutao/ObjectCacheRepository.cs:27` | `System.Text.Json.JsonSerializer.Serialize<TValue>(TValue, JsonSerializerOptions)` |
| 107 | IL3050 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Service/Hutao/ObjectCacheRepository.cs:27` | `System.Text.Json.JsonSerializer.Serialize<TValue>(TValue, JsonSerializerOptions)` |
| 108 | IL2026 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Service/Hutao/ObjectCacheRepository.cs:40` | `System.Text.Json.JsonSerializer.Deserialize<TValue>(String, JsonSerializerOptions)` |
| 109 | IL3050 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Service/Hutao/ObjectCacheRepository.cs:40` | `System.Text.Json.JsonSerializer.Deserialize<TValue>(String, JsonSerializerOptions)` |
| 110 | IL2026 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Service/Metadata/LegacyMetadataService.cs:180` | `System.Text.Json.JsonSerializer.DeserializeAsync<TValue>(Stream, JsonSerializerOptions, CancellationToken)` |
| 111 | IL3050 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Service/Metadata/LegacyMetadataService.cs:180` | `System.Text.Json.JsonSerializer.DeserializeAsync<TValue>(Stream, JsonSerializerOptions, CancellationToken)` |
| 112 | IL2026 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Service/Metadata/LegacyMetadataService.cs:214` | `System.Text.Json.JsonSerializer.DeserializeAsync<TValue>(Stream, JsonSerializerOptions, CancellationToken)` |
| 113 | IL3050 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Service/Metadata/LegacyMetadataService.cs:214` | `System.Text.Json.JsonSerializer.DeserializeAsync<TValue>(Stream, JsonSerializerOptions, CancellationToken)` |
| 114 | IL2026 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Service/Metadata/MetadataService.cs:105` | `System.Text.Json.JsonSerializer.DeserializeAsync<TValue>(Stream, JsonSerializerOptions, CancellationToken)` |
| 115 | IL3050 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Service/Metadata/MetadataService.cs:105` | `System.Text.Json.JsonSerializer.DeserializeAsync<TValue>(Stream, JsonSerializerOptions, CancellationToken)` |
| 116 | IL2026 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Service/Metadata/MetadataService.cs:134` | `System.Text.Json.JsonSerializer.DeserializeAsync<TValue>(Stream, JsonSerializerOptions, CancellationToken)` |
| 117 | IL3050 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Service/Metadata/MetadataService.cs:134` | `System.Text.Json.JsonSerializer.DeserializeAsync<TValue>(Stream, JsonSerializerOptions, CancellationToken)` |
| 118 | IL2026 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Service/Notification/ToastNotificationPipeServer.cs:34` | `System.Text.Json.JsonSerializer.Serialize<TValue>(Stream, TValue, JsonSerializerOptions)` |
| 119 | IL3050 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Service/Notification/ToastNotificationPipeServer.cs:34` | `System.Text.Json.JsonSerializer.Serialize<TValue>(Stream, TValue, JsonSerializerOptions)` |
| 120 | IL2026 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Service/UIGF/AbstractUIGF3ExportService.cs:55` | `System.Text.Json.JsonSerializer.SerializeAsync<TValue>(Stream, TValue, JsonSerializerOptions, CancellationToken)` |
| 121 | IL3050 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Service/UIGF/AbstractUIGF3ExportService.cs:55` | `System.Text.Json.JsonSerializer.SerializeAsync<TValue>(Stream, TValue, JsonSerializerOptions, CancellationToken)` |
| 122 | IL2026 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Service/UIGF/AbstractUIGF40ExportService.cs:49` | `System.Text.Json.JsonSerializer.SerializeAsync<TValue>(Stream, TValue, JsonSerializerOptions, CancellationToken)` |
| 123 | IL3050 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Service/UIGF/AbstractUIGF40ExportService.cs:49` | `System.Text.Json.JsonSerializer.SerializeAsync<TValue>(Stream, TValue, JsonSerializerOptions, CancellationToken)` |
| 124 | IL2026 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Service/UIGF/UIGF42ExportService.cs:44` | `System.Text.Json.JsonSerializer.SerializeAsync<TValue>(Stream, TValue, JsonSerializerOptions, CancellationToken)` |
| 125 | IL3050 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Service/UIGF/UIGF42ExportService.cs:44` | `System.Text.Json.JsonSerializer.SerializeAsync<TValue>(Stream, TValue, JsonSerializerOptions, CancellationToken)` |
| 126 | IL2026 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Service/UIGF/UIGFService.cs:51` | `System.Text.Json.JsonSerializer.Deserialize<TValue>(String, JsonSerializerOptions)` |
| 127 | IL3050 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Service/UIGF/UIGFService.cs:51` | `System.Text.Json.JsonSerializer.Deserialize<TValue>(String, JsonSerializerOptions)` |
| 128 | IL2026 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Service/UIGF/UIGFService.cs:59` | `System.Text.Json.JsonSerializer.Deserialize<TValue>(String, JsonSerializerOptions)` |
| 129 | IL3050 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Service/UIGF/UIGFService.cs:59` | `System.Text.Json.JsonSerializer.Deserialize<TValue>(String, JsonSerializerOptions)` |
| 130 | IL2026 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Service/User/UserFingerprintService.cs:102` | `System.Text.Json.JsonSerializer.Serialize<TValue>(TValue, JsonSerializerOptions)` |
| 131 | IL3050 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Service/User/UserFingerprintService.cs:102` | `System.Text.Json.JsonSerializer.Serialize<TValue>(TValue, JsonSerializerOptions)` |
| 132 | IL2026 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Service/User/UserVerificationService.cs:27` | `System.Text.Json.JsonSerializer.Deserialize<TValue>(String, JsonSerializerOptions)` |
| 133 | IL3050 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Service/User/UserVerificationService.cs:27` | `System.Text.Json.JsonSerializer.Deserialize<TValue>(String, JsonSerializerOptions)` |
| 134 | IL2026 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Service/User/UserVerificationService.cs:30` | `System.Text.Json.JsonSerializer.Deserialize<TValue>(String, JsonSerializerOptions)` |
| 135 | IL3050 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Service/User/UserVerificationService.cs:30` | `System.Text.Json.JsonSerializer.Deserialize<TValue>(String, JsonSerializerOptions)` |
| 136 | IL2026 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Service/User/UserVerificationService.cs:42` | `System.Text.Json.JsonSerializer.Serialize<TValue>(TValue, JsonSerializerOptions)` |
| 137 | IL3050 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Service/User/UserVerificationService.cs:42` | `System.Text.Json.JsonSerializer.Serialize<TValue>(TValue, JsonSerializerOptions)` |
| 138 | IL2026 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Service/Yae/PlayerStore/PlayerStoreParser.cs:212` | `System.Text.Json.JsonSerializer.Serialize<TValue>(TValue, JsonSerializerOptions)` |
| 139 | IL3050 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Service/Yae/PlayerStore/PlayerStoreParser.cs:212` | `System.Text.Json.JsonSerializer.Serialize<TValue>(TValue, JsonSerializerOptions)` |
| 140 | IL2026 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Service/Yae/PlayerStore/PlayerStoreParser.cs:215` | `System.Text.Json.JsonSerializer.Serialize<TValue>(TValue, JsonSerializerOptions)` |
| 141 | IL3050 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Service/Yae/PlayerStore/PlayerStoreParser.cs:215` | `System.Text.Json.JsonSerializer.Serialize<TValue>(TValue, JsonSerializerOptions)` |
| 142 | IL2026 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Service/Yae/PlayerStore/PlayerStoreParser.cs:218` | `System.Text.Json.JsonSerializer.Serialize<TValue>(TValue, JsonSerializerOptions)` |
| 143 | IL3050 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Service/Yae/PlayerStore/PlayerStoreParser.cs:218` | `System.Text.Json.JsonSerializer.Serialize<TValue>(TValue, JsonSerializerOptions)` |
| 144 | IL2026 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/UI/Xaml/Control/Theme/LiquidGlassOverrideStore.cs:25` | `System.Text.Json.JsonSerializer.Deserialize<TValue>(String, JsonSerializerOptions)` |
| 145 | IL3050 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/UI/Xaml/Control/Theme/LiquidGlassOverrideStore.cs:25` | `System.Text.Json.JsonSerializer.Deserialize<TValue>(String, JsonSerializerOptions)` |
| 146 | IL2026 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/UI/Xaml/Control/Theme/LiquidGlassOverrideStore.cs:103` | `System.Text.Json.JsonSerializer.Serialize<TValue>(TValue, JsonSerializerOptions)` |
| 147 | IL3050 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/UI/Xaml/Control/Theme/LiquidGlassOverrideStore.cs:103` | `System.Text.Json.JsonSerializer.Serialize<TValue>(TValue, JsonSerializerOptions)` |
| 148 | IL2026 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/UI/Xaml/Control/Theme/LiquidGlassOverrideStore.cs:104` | `System.Text.Json.JsonSerializer.Serialize<TValue>(TValue, JsonSerializerOptions)` |
| 149 | IL3050 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/UI/Xaml/Control/Theme/LiquidGlassOverrideStore.cs:104` | `System.Text.Json.JsonSerializer.Serialize<TValue>(TValue, JsonSerializerOptions)` |
| 150 | IL2026 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/UI/Xaml/View/Dialog/LaunchGameInstallGameDialog.xaml.cs:94` | `System.Text.Json.JsonSerializer.DeserializeAsync<TValue>(Stream, JsonSerializerOptions, CancellationToken)` |
| 151 | IL3050 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/UI/Xaml/View/Dialog/LaunchGameInstallGameDialog.xaml.cs:94` | `System.Text.Json.JsonSerializer.DeserializeAsync<TValue>(Stream, JsonSerializerOptions, CancellationToken)` |
| 152 | IL2026 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/UI/Xaml/View/Window/AccountInsightsWindow.Gacha.cs:113` | `System.Text.Json.Nodes.JsonArray.Add<T>(T)` |
| 153 | IL3050 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/UI/Xaml/View/Window/AccountInsightsWindow.Gacha.cs:113` | `System.Text.Json.Nodes.JsonArray.Add<T>(T)` |
| 154 | IL2026 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/UI/Xaml/View/Window/AccountInsightsWindow.Gacha.cs:124` | `System.Text.Json.Nodes.JsonArray.Add<T>(T)` |
| 155 | IL3050 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/UI/Xaml/View/Window/AccountInsightsWindow.Gacha.cs:124` | `System.Text.Json.Nodes.JsonArray.Add<T>(T)` |
| 156 | IL2026 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/UI/Xaml/View/Window/AccountInsightsWindow.Gacha.cs:147` | `System.Text.Json.Nodes.JsonArray.Add<T>(T)` |
| 157 | IL3050 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/UI/Xaml/View/Window/AccountInsightsWindow.Gacha.cs:147` | `System.Text.Json.Nodes.JsonArray.Add<T>(T)` |
| 158 | IL2026 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/UI/Xaml/View/Window/AccountInsightsWindow.Gacha.cs:154` | `System.Text.Json.Nodes.JsonArray.Add<T>(T)` |
| 159 | IL3050 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/UI/Xaml/View/Window/AccountInsightsWindow.Gacha.cs:154` | `System.Text.Json.Nodes.JsonArray.Add<T>(T)` |
| 160 | IL2026 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/ViewModel/Achievement/AchievementViewModel.cs:36` | `CommunityToolkit.WinUI.Collections.SortDescription.SortDescription(String, SortDirection, IComparer)` |
| 161 | IL2026 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/ViewModel/Achievement/AchievementViewModel.cs:37` | `CommunityToolkit.WinUI.Collections.SortDescription.SortDescription(String, SortDirection, IComparer)` |
| 162 | IL2026 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/ViewModel/Achievement/AchievementViewModel.cs:38` | `CommunityToolkit.WinUI.Collections.SortDescription.SortDescription(String, SortDirection, IComparer)` |
| 163 | IL2026 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/ViewModel/Achievement/AchievementViewModel.cs:40` | `CommunityToolkit.WinUI.Collections.SortDescription.SortDescription(String, SortDirection, IComparer)` |
| 164 | IL2026 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/ViewModel/Achievement/AchievementViewModel.cs:41` | `CommunityToolkit.WinUI.Collections.SortDescription.SortDescription(String, SortDirection, IComparer)` |
| 165 | IL2026 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/ViewModel/AvatarProperty/AvatarPropertySortDescriptions.cs:12` | `CommunityToolkit.WinUI.Collections.SortDescription.SortDescription(String, SortDirection, IComparer)` |
| 166 | IL2026 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/ViewModel/AvatarProperty/AvatarPropertySortDescriptions.cs:13` | `CommunityToolkit.WinUI.Collections.SortDescription.SortDescription(String, SortDirection, IComparer)` |
| 167 | IL2026 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/ViewModel/AvatarProperty/AvatarPropertySortDescriptions.cs:14` | `CommunityToolkit.WinUI.Collections.SortDescription.SortDescription(String, SortDirection, IComparer)` |
| 168 | IL2026 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/ViewModel/AvatarProperty/AvatarPropertySortDescriptions.cs:15` | `CommunityToolkit.WinUI.Collections.SortDescription.SortDescription(String, SortDirection, IComparer)` |
| 169 | IL2026 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/ViewModel/AvatarProperty/AvatarPropertySortDescriptions.cs:16` | `CommunityToolkit.WinUI.Collections.SortDescription.SortDescription(String, SortDirection, IComparer)` |
| 170 | IL2026 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/ViewModel/AvatarProperty/AvatarPropertySortDescriptions.cs:17` | `CommunityToolkit.WinUI.Collections.SortDescription.SortDescription(String, SortDirection, IComparer)` |
| 171 | IL2026 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/ViewModel/AvatarProperty/AvatarPropertySortDescriptions.cs:18` | `CommunityToolkit.WinUI.Collections.SortDescription.SortDescription(String, SortDirection, IComparer)` |
| 172 | IL2026 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/ViewModel/AvatarProperty/AvatarPropertySortDescriptions.cs:19` | `CommunityToolkit.WinUI.Collections.SortDescription.SortDescription(String, SortDirection, IComparer)` |
| 173 | IL2026 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/ViewModel/AvatarProperty/AvatarPropertySortDescriptions.cs:20` | `CommunityToolkit.WinUI.Collections.SortDescription.SortDescription(String, SortDirection, IComparer)` |
| 174 | IL2026 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/ViewModel/AvatarProperty/AvatarPropertySortDescriptions.cs:21` | `CommunityToolkit.WinUI.Collections.SortDescription.SortDescription(String, SortDirection, IComparer)` |
| 175 | IL2026 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/ViewModel/Backpack/BackpackReliquaryItemView.cs:260` | `System.Text.Json.JsonSerializer.Deserialize<TValue>(String, JsonSerializerOptions)` |
| 176 | IL3050 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/ViewModel/Backpack/BackpackReliquaryItemView.cs:260` | `System.Text.Json.JsonSerializer.Deserialize<TValue>(String, JsonSerializerOptions)` |
| 177 | IL2026 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/ViewModel/Backpack/BackpackReliquaryItemView.cs:278` | `System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver.DefaultJsonTypeInfoResolver()` |
| 178 | IL3050 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/ViewModel/Backpack/BackpackReliquaryItemView.cs:278` | `System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver.DefaultJsonTypeInfoResolver()` |
| 179 | IL2026 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/ViewModel/Game/GamePackageViewModel.cs:359` | `System.Text.Json.JsonSerializer.Deserialize<TValue>(String, JsonSerializerOptions)` |
| 180 | IL3050 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/ViewModel/Game/GamePackageViewModel.cs:359` | `System.Text.Json.JsonSerializer.Deserialize<TValue>(String, JsonSerializerOptions)` |
| 181 | IL2026 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Web/Bridge/BridgeShare.cs:44` | `System.Text.Json.JsonSerializer.Deserialize<TValue>(String, JsonSerializerOptions)` |
| 182 | IL3050 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Web/Bridge/BridgeShare.cs:44` | `System.Text.Json.JsonSerializer.Deserialize<TValue>(String, JsonSerializerOptions)` |
| 183 | IL2026 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Web/Bridge/MiHoYoJSBridge.cs:439` | `System.Text.Json.JsonSerializer.Deserialize<TValue>(String, JsonSerializerOptions)` |
| 184 | IL3050 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Web/Bridge/MiHoYoJSBridge.cs:439` | `System.Text.Json.JsonSerializer.Deserialize<TValue>(String, JsonSerializerOptions)` |
| 185 | IL2026 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Web/Bridge/MiHoYoJSBridge.cs:452` | `System.Text.Json.JsonSerializer.Serialize(Object, Type, JsonSerializerOptions)` |
| 186 | IL3050 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Web/Bridge/MiHoYoJSBridge.cs:452` | `System.Text.Json.JsonSerializer.Serialize(Object, Type, JsonSerializerOptions)` |
| 187 | IL2026 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Web/Bridge/Model/DataSignV2Payload.cs:17` | `System.Text.Json.JsonSerializer.Serialize<TValue>(TValue, JsonSerializerOptions)` |
| 188 | IL3050 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Web/Bridge/Model/DataSignV2Payload.cs:17` | `System.Text.Json.JsonSerializer.Serialize<TValue>(TValue, JsonSerializerOptions)` |
| 189 | IL2026 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Web/Bridge/Model/JsParam.cs:35` | `System.Text.Json.JsonSerializer.Deserialize<TValue>(JsonElement, JsonSerializerOptions)` |
| 190 | IL3050 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Web/Bridge/Model/JsParam.cs:35` | `System.Text.Json.JsonSerializer.Deserialize<TValue>(JsonElement, JsonSerializerOptions)` |
| 191 | IL2026 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Web/Enka/EnkaClient.cs:67` | `System.Net.Http.Json.HttpContentJsonExtensions.ReadFromJsonAsync<T>(HttpContent, JsonSerializerOptions, CancellationToken)` |
| 192 | IL3050 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Web/Enka/EnkaClient.cs:67` | `System.Net.Http.Json.HttpContentJsonExtensions.ReadFromJsonAsync<T>(HttpContent, JsonSerializerOptions, CancellationToken)` |
| 193 | IL2026 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Web/Hutao/HutaoAsAService/HutaoAsAServiceClient.cs:56` | `System.Text.Json.JsonSerializer.Deserialize<TValue>(String, JsonSerializerOptions)` |
| 194 | IL3050 | `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Web/Hutao/HutaoAsAService/HutaoAsAServiceClient.cs:56` | `System.Text.Json.JsonSerializer.Deserialize<TValue>(String, JsonSerializerOptions)` |
---

## 13. 附录 D：2026-11-08 追加实验（AOT 发布根因、最小复现、workaround）

> 本节是在报告主体（§1–§12）之后做的第二轮实验，**推翻了 §3 原有的"硬阻塞"结论**。
> 全部实验在 `Debug / x64 / net11.0-windows10.0.26100.0 / win-x64` 上完成，SDK 11.0.100-rc.2.26504.105，Windows App SDK 2.6.6-experimental，CsWinRT 2.3.1。

### D.1 目标

1. 用 binlog + 引用集 dump，定位 `WMC1007: Cannot resolve metadata for WinUI types` 的确切原因；
2. 造一个最小 WinUI 3 工程，判断这是"本仓库工程配置问题"还是"WASDK 通用问题"。

### D.2 引用集 dump（定位手段）

在 `MarkupCompilePass1` 之前 dump `@(ReferencePath)`（自定义 targets，通过 `CustomAfterMicrosoftCommonTargets` 注入，不改仓库文件）：

```xml
<Project>
  <Target Name="DumpReferencePathForXaml" BeforeTargets="MarkupCompilePass1">
    <ItemGroup>
      <_DumpRef Include="@(ReferencePath)" />
    </ItemGroup>
    <WriteLinesToFile File="$(DumpDir)\refs-$(DumpTag).txt"
                      Lines="@(_DumpRef->'%(Filename)|%(Extension)')"
                      Overwrite="true" Encoding="UTF-8" />
    <Message Importance="high"
             Text="DUMP[$(DumpTag)] ReferencePath=@(_DumpRef->Count()) PublishAot=$(PublishAot) IsPublishing=$(_IsPublishing)" />
  </Target>
</Project>
```

调用方式：

```powershell
dotnet publish <proj> -c Debug -r win-x64 -p:PublishReadyToRun=false -p:SelfContained=true `
  -p:DumpDir=<abs> -p:DumpTag=aot `
  -p:CustomAfterMicrosoftCommonTargets=<abs>\DumpRefs.targets `
  -p:PublishAot=true -bl:<abs>\aot.binlog
```

### D.3 主工程结果（这是关键数据）

| 场景 | `@(ReferencePath)` | XAML 编译 | 整体 |
|---|---:|---|---|
| `dotnet build`（不发布） | 314 | 通过 | 通过 |
| `dotnet publish`，`PublishAot=false` | 314 | 通过 | 通过（产物为托管 IL） |
| `dotnet publish`，`PublishAot=true` | **0** | **`WMC1007`** | 失败 |
| `dotnet publish`，`PublishAot=true` ＋ `-p:MarkupCompilePass1DependsOn=ResolveReferences` | 314 | 通过 | 进入 ILC，产出 302 MB 原生 exe |

`refs-aot.txt` 只有 3 字节（UTF-8 BOM），`refs-noaot.txt` 有 314 行——**AOT 发布时 XAML 编译器确实一个引用都没拿到**。

原因：`MarkupCompilePass1` 的 `DependsOnTargets="$(MarkupCompilePass1DependsOn)"`（`Microsoft.UI.Xaml.Markup.Compiler.interop.targets:509`），而该属性默认只包含 `GetVCInstallPath; GetXamlCppIncludeDirectories; GetPriIndexName; GetPrecompiledHeaderFile`（同文件 250–257 行）——**不含 `ResolveReferences`**。普通 build/publish 里 `ResolveReferences` 会因别的依赖先跑；在 AOT publish 的目标图里它被排到了后面，于是 XAML 编译先执行。

> 补充：该属性在 targets 里是"自我追加"式定义（`...; $(MarkupCompilePass1DependsOn)`），所以从命令行传 `ResolveReferences` 不会丢掉默认依赖，只是把它排到最后。

### D.4 最小复现工程

位置：`tmp-aot-probe/minwinui/`（3 个源文件 + csproj + manifest，未加入仓库解决方案）

```
MinWinUI.csproj        # net11.0-windows10.0.26100.0, UseWinUI, WindowsPackageType=None
App.xaml / App.xaml.cs
MainWindow.xaml        # 一个 {x:Bind} + 一个 {Binding}
MainWindow.xaml.cs
app.manifest
```

结果：

| 场景 | `@(ReferencePath)` | 结果 |
|---|---:|---|
| `dotnet publish -p:PublishAot=false` | 213 | ✅ 成功（托管 IL 产物） |
| `dotnet publish -p:PublishAot=true` | 0 | ❌ `WMC1007` |
| `dotnet publish -p:PublishAot=true -p:MarkupCompilePass1DependsOn=ResolveReferences` | 213 | ✅ 成功，产出 **8.5 MB 原生 `MinWinUI.exe`** |

原生性验证：`MinWinUI.exe` = 8,567,808 字节，**无 `BSJB`（CLI 元数据）**，发布目录**无 `coreclr.dll` / `hostfxr.dll`**。

→ 结论：**不是本仓库的工程配置问题，是 WASDK/Windows App SDK 在 `PublishAot` 下的目标顺序缺陷，且一个属性即可绕过。**

### D.5 主工程完整 AOT 发布

套用同一属性后，主工程：

1. XAML 编译通过；
2. 托管编译通过（产出 1200+ 条 trim/AOT 分析警告，与 §4 的 194 条一致）；
3. **ILC 成功**：`bin/Debug/net11.0-windows10.0.26100.0/win-x64/publish/Snap.Hutao.Remastered.exe`
   - 302,346,240 字节（≈288 MB），**无 `BSJB`、无 `coreclr.dll`**
   - PDB 1,271,058,432 字节（≈1.18 GB）
   - 同目录 62 个 DLL（WASDK 原生依赖 + 本项目原生 DLL）
4. 最后**被本仓库自己的校验拦下**：

```
Build\ReadyToRun.targets(30,5): error : Required runtime file is missing from publish output:
  ...\publish\Snap.Hutao.Remastered.Deployment.exe.
  Check Publish metadata on its PackageReference before packaging.
```

即：R2R 的产物校验没有区分 `PublishAot`，在 AOT 产物里找不到 R2R 时代的 `Deployment.exe` 就报错。**这不是 AOT 编译失败，是仓库自己的门禁误伤。**

绕过该 target（实验阶段用的是"同名空 target 覆盖"，仅临时用）后重跑：**`EXIT=0`，发布完整成功**。正式修法不是跳过校验，而是把文件补回发布清单——见 `Build/AotPublish.targets` 的 `IncludeAotPublishRuntimePayload` / `EnsureAotPublishRuntimePayload`。

```
Snap.Hutao.Remastered -> ...\win-x64\publish\
SKIP: VerifyRequiredPublishRuntimeFiles disabled for AOT experiment
```

最终产物核对：

| 项 | 值 |
|---|---|
| `Snap.Hutao.Remastered.exe` | 288.3 MB，无 `BSJB`（原生） |
| `coreclr.dll` / `hostfxr.dll` | 均不存在 |
| 同目录 DLL | 62 个 |
| 发布目录总大小 | 1741 MB（含 1.18 GB PDB） |
| 应用自身 `.pri` / `.xbf` | **缺失**（只有 WASDK 自带的 `Microsoft.UI*.pri`） |


### D.6 这一轮新暴露的问题（都需要跟进）

| # | 问题 | 性质 | 建议 |
|---|---|---|---|
| 1 | `Build/ReadyToRun.targets` 的最终产物校验在 AOT 下误报（AOT 发布不复制 `Deployment.exe`） | 仓库自伤 | 已修：`Build/AotPublish.targets` 把该文件重新注入发布清单并在校验前兜底复制 |
| 2 | AOT 产物目录**没有 `resources.pri` / `*.xbf` 之外的 PRI**（最小样例与主工程都缺失），与 [#6394](https://github.com/microsoft/WindowsAppSDK/issues/6394) 一致 | 上游缺陷 | 打包前手工补 PRI，或等上游修 |
| 3 | AOT 产物 288 MB（R2R 约 60 MB 级），PDB 1.18 GB | 体积 | 发布时裁剪符号、评估 `IlcOptimizationPreference=Size` |
| 4 | 运行时行为**完全未验证**：最小样例在 AOT 与非 AOT 下都因 `0xC000027B`（`Microsoft.UI.Xaml.dll`）启动即崩，属于样例自身的 unpackaged/PRI 配置问题，不能拿来判断 AOT 运行时 | 未验证 | 用主工程做隔离环境冒烟（禁用真实用户数据） |
| 5 | 194 条诊断仍在（ILC 期间以 1200+ 条 warning 形式重现） | 已知债 | 按 §8 的 P1 处理 |

### D.7 复现命令

```powershell
# 1) 观察引用集差异（不真正编译 XAML）
dotnet publish src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Snap.Hutao.Remastered.csproj `
  -c Debug -r win-x64 -p:PublishReadyToRun=false -p:SelfContained=true `
  -p:DumpDir=$PWD\tmp-aot-probe\_diag -p:DumpTag=aot `
  -p:CustomAfterMicrosoftCommonTargets=$PWD\tmp-aot-probe\_diag\DumpRefs.targets `
  -p:PublishAot=true -bl:$PWD\tmp-aot-probe\_diag\aot.binlog

# 2) 带 workaround 的完整 AOT 发布
dotnet publish src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Snap.Hutao.Remastered.csproj `
  -c Debug -r win-x64 -p:PublishAot=true -p:PublishReadyToRun=false -p:SelfContained=true `
  -p:MarkupCompilePass1DependsOn=ResolveReferences

# 3) 最小复现
dotnet publish tmp-aot-probe/minwinui/MinWinUI.csproj `
  -c Debug -r win-x64 -p:PublishAot=true -p:PublishReadyToRun=false `
  -p:MarkupCompilePass1DependsOn=ResolveReferences
```

产物与日志：

| 路径 | 内容 |
|---|---|
| `tmp-aot-probe/_diag/aot.binlog` / `normal.binlog` | 两次发布的 binlog（MSBuild Structured Log Viewer 可开） |
| `tmp-aot-probe/_diag/refs-*.txt` | `@(ReferencePath)` 快照（0 vs 314 / 213） |
| `tmp-aot-probe/minwinui/` | 最小复现工程 |
| `aot-publish-forced.log` | 主工程带 workaround 的完整日志（停在 R2R 校验） |
