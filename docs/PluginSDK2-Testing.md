# SDK 2.0 验收、故障排查和 v1 迁移

先阅读 [架构与开发模型](PluginSDK2-Architecture.md)。历史构建/运行失败、原始诊断、修复提交与验证边界集中在 [开发技术记录](PluginSDK2-EngineeringNotes.md)；本页负责执行当前版本的回归测试，不用历史 Pass 替代当前 Pass。

## 1. 构建与回归

### A. Visual Studio 日常调试

1. 关闭之前的 PrototypeHost。打开 src/Snap.Hutao.Plugin.SDK/Snap.Hutao.Plugin.SDK.slnx，选择 Debug | x64，Build Solution。MIDL/投影和两个 C++ DLL 随 Build 自动生成。
2. 在 Samples 中右键 TestPlugin.Aot → Publish → NativeAot → Publish。检查 artifacts/aot-plugin/TestPlugin.Aot.dll。C# Build 不能替代此操作。
3. 在 SDK 根目录的终端打包（唯一的日常脚本）：

```powershell
pwsh -File .\build\Pack-Samples.ps1 -Configuration Debug
```

仅重新发布 C# 时可追加 -Sample CSharpAot；仅修改某个 C++ 示例则追加 -Sample CppCore 或 -Sample CppCompiled。脚本不会重新 Build/Publish。修改 C# 前关闭宿主，并确保 Publish 成功后才打包；必要时清理旧 artifacts/aot-plugin。不要把旧文件误认为新发布结果。

4. 将 PrototypeHost 设为启动项目，F5。每次换包先关闭整个进程，再启动：

| 点击按钮 | 加载包 | 测试视图 |
|---|---|---|
| C++ core | artifacts/TestPlugin.Native.Core.hutao | programmatic、loose |
| C++ compiled (experimental) | artifacts/TestPlugin.Native.hutao | main，x:Bind、Click |
| C# AOT sample | artifacts/TestPlugin.hutao | main、binding、programmatic |

样例按钮只填路径。点击 Inspect and load → Trust this package；选择所有 Plugin view 并点击控件。C# main 检查计数、编辑并 Save，binding 检查 OneWay/TwoWay 和 ICommand。选择 Exercise ABI → Run command → 对话框点 OK；确认 Command completed。随后 Disable and re-enable，Uninstall session。裸 DLL 没有验证过的资源清单，正常 UI 测试必须用 .hutao。

### B. Release 宿主矩阵

1. 在 VS 选 Release | x64，Build Solution。若 C# 插件代码有变化，重新 Publish TestPlugin.Aot（NativeAot）。
2. 在 SDK 根目录执行 pwsh -File .\build\Pack-Samples.ps1 -Configuration Release。
3. 右键 PrototypeHost → Publish → JitRelease → Publish，生成 artifacts/jit-host/PrototypeHost.exe；再选择 NativeAot → Publish，生成 artifacts/aot-host/PrototypeHost.exe。两目录都应包含 resources.pri。不要混用或覆盖两个发布目录。
4. 分别启动两个发布的 EXE，重复 A 的三个包，每次换包关闭进程。F5 Debug 不等同于 Release Native AOT。
5. 包负例：运行 build/Test-Packaging.ps1（仅维护者回归）。普通托管 DLL 诊断见下方。

测试工作区位于 LocalAppData/Snap.Hutao.Plugin.SDK.Tests，与正式应用隔离；安装 marker 为测试进程内存状态。

## 2. 核心用例与证据

| ID | 步骤 | 必须看到的结果 |
|---|---|---|
| ABI-01 | 每种宿主加载 C++ core / C# AOT | 导出、QI、OnLoad、OnInstall、OnEnable 完成；真实 UIElement 显示 |
| UI-01 | 选择 programmatic 视图、点击 Button | 原生控件文字变化、通知；不经 XAML 解析 |
| UI-02 | 选择 loose 视图、点击按钮 | manifest 中 .xaml 被加载，命名控件和 Click 回调正常 |
| UI-03 | C# Binding 视图，点击 Increment，再编辑 Message 并 Save | Counter OneWay 更新、TextBox TwoWay 更新 draft、ICommand 执行；重开视图读到已保存值 |
| ASYNC-01 | 选择 Exercise ABI 命令并点击 Run command | 设置 int/bool/string 往返；worker 无 UI access、dispatcher callback 有 access；对话框 OK；C# 两次 Task.Yield 和每阶段断言通过 |
| LIFE-01 | Disable and re-enable | OnDisable 后 UI 与贡献消失；同包同模块新实例 OnLoad/OnEnable；不重复 OnInstall |
| LIFE-02 | Uninstall session | active OnDisable/OnUninstall；不 FreeLibrary |
| LIFE-03 | 插件故意抛错、重复启停/打开关闭 | HRESULT/日志准确；不重复贡献、不遗留视图、事件、设置观察者；失败模块不能在本进程替换 |
| PKG-01 | Test-Packaging.ps1 | 正常包通过；额外文件、路径穿越、大小写碰撞、缺 manifest、篡改 DLL 拒绝 |
| PKG-02 | 增加 managed DLL、错误架构、重复 ID、错误 hash | pack/verify/安装拒绝，扫描不执行 DLL |
| SEC-01 | 拒绝信任、改包字节、管理员启动、异常退出 | 不加载未批准哈希；管理员无自动启用；running marker 触发恢复模式 |
| SET-01 | 主程序设置页修改、Reset、重启 | 缓存与持久文件一致，插件页面刷新；模拟 IO 失败不假成功 |
| UPDATE-01 | 安装 v1 测试包、暂存 v2、重启 Apply、启用 | 旧进程不替换，备份直到新版本成功；测试中途退出及 Roll back |
| GAME-01 | 验证缺失能力、未声明路径、重复声明、错误 PE 与被修改的包 | 验证器拒绝，无游戏执行 |
| GAME-02 | 在游戏允许的隔离环境验证已批准 active 包的原有 Island 启动扩展 | 仅声明的库进入当前启动流程；未批准、禁用、普通启动及 Resume 不加入；无兼容性保证（NotRun） |
| NAV-01 | 多插件贡献、跨页面导航/返回、窗口重建 | 内置导航仍正常，插件项有唯一 ID，宿主 PluginHostPage 承载，离开后关闭 view |
| BUILD-01 | 正式应用 JIT/AOT MSIX 及现有支持的 unpackaged 运行 | 安装、导航、通知、设置、退出均正常；原有发布行为不回归 |

### 宿主 × 插件完整矩阵

| 插件产物 | JIT PrototypeHost | Native AOT PrototypeHost |
|---|---|---|
| C++ Core（程序式 UI、loose .xaml） | 正向运行 | 正向运行 |
| C++ Compiled（XBF、x:Bind） | 正向实验 | 正向实验 |
| C# Native AOT | 正向运行 | 正向运行 |
| C# 普通 IL DLL（PublishAot=false，直接加载裸 DLL） | 用户确认失败，缺少入口 | 实测 EntryPointNotFoundException / Win32 127 |

前六项应分别记录真实运行结果；编译期 XAML 仍是实验。后两项是诊断用例，不代表支持普通托管插件。PrototypeHost 的裸 DLL 调试入口不进行额外 PE 预检查，以观察 Windows 原生加载器 LoadLibraryExW 或 CsWin32 GetProcAddress 的实际错误。正式 .hutao 包仍要求原生 x64 DLL，保留原有 CorHeader 校验。使用以下命令构建独立的诊断样本，不覆盖 Native AOT 发布输出：

```powershell
dotnet build .\samples\CSharpAot\TestPlugin.Aot.csproj -c Release -p:Platform=x64 -p:PublishAot=false -p:SelfContained=false -o .\artifacts\managed-fixture
pwsh -File .\build\Test-Packaging.ps1 -ManagedDll .\artifacts\managed-fixture\TestPlugin.Aot.dll
```

测试脚本将普通托管 DLL 替换合法包入口并重新计算正确的 SHA256，再要求 verify 拒绝；因此失败不能被简单归因于哈希错误。随后分别启动 JIT/AOT PrototypeHost，手动填入该裸 DLL 完整路径（不要打包），Inspect and load，确认仅加载本地自己构建的可信样本。记录 LoadLibraryExW 是否成功；若失败，记录 Win32Exception.NativeErrorCode；若映射成功但没有 HutaoPluginCreate 导出，则记录 CsWin32 GetProcAddress 的 EntryPointNotFoundException。2026-11-27 用户报告两种宿主均不能加载普通 IL DLL；提供的 AOT 日志显示 LoadLibraryExW 成功，GetProcAddress(HutaoPluginCreate) 返回 ERROR_PROC_NOT_FOUND (127)，JIT 也报告失败但未提供完整栈。这证实普通 Build 不会生成原生导出，不恢复 ALC 托管加载器。每项记录 commit、SDK 版本、宿主路径、包 SHA256、Windows build、日志及 Pass/Fail/NotRun。正式 Snap Hutao 本身仍不支持 compiled 模式。

C++ compiled 是单独实验：选择 C++ compiled (experimental)，确认 InitializeComponent deferred、绝对 URI、LoadComponent returned successfully、x:Bind 文本和 Click；然后测试主题、嵌套控件、相同名称资源、多插件、两个发布宿主。标准模板的 App 生成的 AddOtherProvider 接入亦需验证。失败不影响 core 的程序式/loose 测试。正式应用拒绝 compiled，C# compiled 尚无实现。

## 3. 当前状态与发布门槛

已报告的真实测试（旧 Debug harness）：两种 DLL 编译/发布与打包成功，两种插件加载 UI 成功；C++ compiled Page 在显式 InitializeComponent 延迟方案中成功。2026-11-27 用户随后确认 C++ Core 已正常加载（拆分前的代码）。拆分后的三项目、标准扩展名与六正两负矩阵仍需重新验证。C# Exercise ABI 曾报 Expected UI STA，随后提交的同步上下文修复尚无完整回归证据。旧 JIT/AOT Publish 宿主启动均 FailFast，随后用户引入标准 WinUI 模板。

本轮结构调整、主程序接入、三种核心 UI、Binding 示例、本地 SDK 分发、更新恢复与测试脚本是源码实现，**没有 Windows 执行环境中的新构建/运行结果**。不得把实现完成当作测试通过。正式合并/发布前必须运行上述测试；特别是标准 WinUI 生成的 metadata provider、资源发布、C# AOT Binding、主程序 DI 生成器、退出与更新故障注入。原始 Design.md 是目标规范，实际差异：更新显式重启应用、旧包先卸载保留数据再安装；编译期 XAML 未正式启用；未发布 NuGet；缺少真实 ABI 引用计数压力测试、全部事务故障注入及生产性能基线。需要后续真实 Windows 日志才能关掉这些门槛，不是隐藏的兼容承诺。

## 4. 主程序测试

打开 src/Snap.Hutao.Remastered/Snap.Hutao.Remastered.slnx，按仓库现有 AOT/non-AOT 构建和 MSIX 发布流程。不要修改主程序 TFM、Windows App SDK、CI 发布配置规避插件问题。建议先使用独立 profile（仓库支持的 SNAP_HUTAO_PROFILE_ROOT，仅 unpackaged），避免真实用户 Plugins 目录。插件管理页安装两个 core .hutao 并执行上述用例；测试管理员确认、正常关闭、模拟崩溃、重启、更新与回滚、旧包列表和数据保留。正式主程序包含核心宿主源码，PrototypeHost 通过 Compile Link 编译这些文件；不依赖 Snap.Hutao.Plugin.Host.dll。

## 5. 常见故障定位

| 错误 | 原因与检查 |
|---|---|
| MSB3073 Generate-Contracts.ps1 | 外部脚本退出码；使用 -tl:off -v:normal 查看实际 MIDL/MSVC/WinMD 错误 |
| Package entry must be native x64 DLL | C# 普通 Build、错误架构或损坏 PE；重新 Native AOT Publish |
| LoadLibrary / missing export | dumpbin /exports、/dependents；入口、VC runtime、native dependencies |
| XAML parsing failed / Cannot locate resource | 确认 ui.mode、manifest paths、资源哈希；compiled 检查绝对 URI 和 XBF，不能推断 ABI 失败 |
| Specified cast is not valid / CsWinRT1034 | Native AOT 动态 runtimeclass 投影被裁剪；对真正转换目标标注 DynamicWindowsRuntimeCast |
| Expected UI STA / 0x8001010E | worker 访问宿主服务，或者插件独立运行时的同步上下文在续体中丢失；检查 CaptureDispatcher、UiCall 与每个 await；不要去掉 CheckThread |
| 0xC000027B / 0xC0000602 | XAML stowed exception / fail-fast；查看调试器 $stowedexception、原始 HRESULT、startup-error.txt，而非最终 RaiseFailFastException 栈 |
| Debug 正常、Publish 启动崩溃 | 比对 resources.pri、发布输出及 WinUI 初始化；标准项目应含 App.xaml 与生成 InitializeComponent；PrototypeHost 额外验证 PRI 存在 |
| Plugin ID already mapped / restart required | 模块驻留或隔离策略；关闭进程，不能 FreeLibrary |
| NU1507 | NuGet Central Package Management 多源警告；按实际来源配置 Package Source Mapping，不要误判为 AOT 错误 |

## 6. v1 → v2 迁移

完整逐段代码迁移见 [Migration](PluginSDK2-Migration.md)；编译期资源、PRI、loose XAML 实例见 [Xaml](PluginSDK2-Xaml.md)。

旧 SDK 的 Snap.Hutao.Plugin.SDK 1.0.0 是 netstandard2.0 的 build-only NuGet 包（IncludeBuildOutput=false），通过 .targets/GenerateHutaoPlugin 在普通 Build 后自动生成 .hutao。旧 TestPlugin 还 ProjectReference 主程序，使用 [PluginMain]、AssemblyLoadContext 和宿主内部 API。旧 README 见删除提交 6ce3721469609913121c955c0115a01bde94dca5 的父提交。它并未提供安全 native hot reload。

| 旧 | 新 |
|---|---|
| SDK 1.0.0 build-only package | SDK 2.0 的真实 C# facade NuGet + projection NuGet |
| [PluginMain] | HutaoPluginCreate 导出 + PluginExport.Create<T> |
| ProjectReference 主程序 | 移除；引用 SDK、投影和匹配 WinAppSDK |
| Build 生成 managed DLL | PublishAot/NativeLib=Shared 生成 native DLL |
| GenerateHutaoPlugin / HutaoPluginOutputPath | Publish 后显式 staging 与 PluginPack |
| 旧 PluginScopedPage/宿主 ViewModel | 返回真实 UIElement，宿主 PluginHostPage 管理视觉树 |
| Embedded .hxaml + 运行时反射 | manifest 声明的 .xaml + LoadXaml、事件或 AOT 生成 Binding |
| 旧设置 API | OnLoad 注册 int/bool/string，PluginSetting<T>，原路径兼容同名同类型 |
| inject/ | 显式 gameLibraries + game-libraries 能力，恢复原 Island 扩展；实际游戏验收待完成 |
| Harmony patch | 没有通用替代；用公开贡献接口，缺少业务扩展单独设计 |
| ALC 卸载 | 逻辑 disable，native module 驻留到进程退出 |

卸载旧包（不要删除设置/data），重建同 ID 的 SDK2 包、重新信任，确保设置名称和类型不变。不要通过改 manifest schemaVersion 或改 DLL 文件名伪造升级。迁移完成必须以真实包、四组合及主程序回归为证据。
