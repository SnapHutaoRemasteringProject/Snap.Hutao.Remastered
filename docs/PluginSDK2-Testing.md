# SDK 2.0 验收、故障排查和 v1 迁移

先阅读 [架构与开发模型](PluginSDK2-Architecture.md)。历史构建/运行失败、原始诊断、修复提交与验证边界集中在 [开发技术记录](PluginSDK2-EngineeringNotes.md)；本页负责执行当前版本的回归测试，不用历史 Pass 替代当前 Pass。

## 1. 构建与回归

在干净 checkout，VS 2026 打开 src/Snap.Hutao.Plugin.SDK/Snap.Hutao.Plugin.SDK.slnx，Release|x64，Rebuild。C++ 和 C# 投影由同一 Contracts.winmd 生成；不要并行启动两个独立构建过程同时写 artifacts/metadata/references。C# AOT 发布：

```powershell
cd src\Snap.Hutao.Plugin.SDK
dotnet publish .\TestPlugin.Aot\TestPlugin.Aot.csproj -c Release -p:Platform=x64 -p:PublishProfile=NativeAot -tl:off
pwsh -File .\build\Pack-Samples.ps1 -Configuration Release
pwsh -File .\build\Test-Packaging.ps1
```

Pack-Samples 默认 Release，生成 C++ core、C++ compiled、C# AOT 三个明确区分的包。C++ core 与 compiled 包使用同一示例 DLL，但不同 manifest ID 及资源清单；示例代码显式按 ID 选择工厂，不是 SDK 的自动降级机制。正式应用测试使用 Core 包。

重建发布目录：

```powershell
Remove-Item .\artifacts\jit-host,.\artifacts\aot-host -Recurse -Force -ErrorAction SilentlyContinue
dotnet publish .\tests\PrototypeHost\PrototypeHost.csproj -c Release -p:Platform=x64 -p:PublishProfile=JitRelease -tl:off
dotnet publish .\tests\PrototypeHost\PrototypeHost.csproj -c Release -p:Platform=x64 -p:PublishProfile=NativeAot -tl:off
Get-ChildItem .\artifacts\jit-host\resources.pri,.\artifacts\aot-host\resources.pri
```

每个宿主启动一个干净进程、选择 C++ core，重复 C# AOT；切换包时关闭进程。不要以 Debug Build 替代 Release Publish。PrototypeHost 使用标准 App.xaml/MainWindow.xaml/MainPage.xaml，普通 .NET 与 Native AOT 都走生成应用初始化代码，并启用 PRI 发布基础设施。测试宿主的工作区和设置在 LocalAppData/Snap.Hutao.Plugin.SDK.Tests，与正式应用数据隔离；install marker 在测试宿主为内存状态，不是生产安装器。

## 2. 核心用例与证据

| ID | 步骤 | 必须看到的结果 |
|---|---|---|
| ABI-01 | 每种宿主加载 C++ core / C# AOT | 导出、QI、OnLoad、OnInstall、OnEnable 完成；真实 UIElement 显示 |
| UI-01 | 选择 programmatic 视图、点击 Button | 原生控件文字变化、通知；不经 XAML 解析 |
| UI-02 | 选择 loose 视图、点击按钮 | manifest 中 .hxaml 被加载，命名控件和 Click 回调正常 |
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
| NAV-01 | 多插件贡献、跨页面导航/返回、窗口重建 | 内置导航仍正常，插件项有唯一 ID，宿主 PluginHostPage 承载，离开后关闭 view |
| BUILD-01 | 正式应用 JIT/AOT MSIX 及现有支持的 unpackaged 运行 | 安装、导航、通知、设置、退出均正常；原有发布行为不回归 |

四种 Release 组合应独立记录：JIT host × C++ core、JIT × C# AOT、AOT host × C++ core、AOT × C# AOT。每个记录 commit、SDK 版本、宿主路径、包 SHA256、Windows build、测试日志。Result 仅可写 Pass / Fail / NotRun，不以源码推断 Pass。

C++ compiled 是单独实验：选择 C++ compiled (experimental)，确认 InitializeComponent deferred、绝对 URI、LoadComponent returned successfully、x:Bind 文本和 Click；然后测试主题、嵌套控件、相同名称资源、多插件、两个发布宿主。标准模板的 App 生成的 AddOtherProvider 接入亦需验证。失败不影响 core 的程序式/loose 测试。正式应用拒绝 compiled，C# compiled 尚无实现。

## 3. 当前状态与发布门槛

已报告的真实测试（旧 Debug harness）：两种 DLL 编译/发布与打包成功，两种插件加载 UI 成功；C++ compiled Page 在显式 InitializeComponent 延迟方案中成功。C# Exercise ABI 曾报 Expected UI STA，随后提交的同步上下文修复尚无完整回归证据。旧 JIT/AOT Publish 宿主启动均 FailFast，随后用户引入标准 WinUI 模板。

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

旧 SDK 的 Snap.Hutao.Plugin.SDK 1.0.0 是 netstandard2.0 的 build-only NuGet 包（IncludeBuildOutput=false），通过 .targets/GenerateHutaoPlugin 在普通 Build 后自动生成 .hutao。旧 TestPlugin 还 ProjectReference 主程序，使用 [PluginMain]、AssemblyLoadContext 和宿主内部 API。旧 README 见删除提交 6ce3721469609913121c955c0115a01bde94dca5 的父提交。它并未提供安全 native hot reload。

| 旧 | 新 |
|---|---|
| SDK 1.0.0 build-only package | SDK 2.0 的真实 C# facade NuGet + projection NuGet |
| [PluginMain] | HutaoPluginCreate 导出 + PluginExport.Create<T> |
| ProjectReference 主程序 | 移除；引用 SDK、投影和匹配 WinAppSDK |
| Build 生成 managed DLL | PublishAot/NativeLib=Shared 生成 native DLL |
| GenerateHutaoPlugin / HutaoPluginOutputPath | Publish 后显式 staging 与 PluginPack |
| 旧 PluginScopedPage/宿主 ViewModel | 返回真实 UIElement，宿主 PluginHostPage 管理视觉树 |
| Embedded .hxaml + 运行时反射 | manifest 声明的 .hxaml + LoadXaml、事件或 AOT 生成 Binding |
| 旧设置 API | OnLoad 注册 int/bool/string，PluginSetting<T>，原路径兼容同名同类型 |
| inject/ 或 Harmony patch | 无对应 SDK2 服务；不是通过改命名空间即可迁移 |
| ALC 卸载 | 逻辑 disable，native module 驻留到进程退出 |

卸载旧包（不要删除设置/data），重建同 ID 的 SDK2 包、重新信任，确保设置名称和类型不变。不要通过改 manifest schemaVersion 或改 DLL 文件名伪造升级。迁移完成必须以真实包、四组合及主程序回归为证据。
