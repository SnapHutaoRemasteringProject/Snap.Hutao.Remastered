# Snap Hutao Plugin SDK 2.0

SDK 2.0 是 Snap Hutao Remastered 的进程内 WinRT 原生插件开发工具。C++/WinRT 与 C# Native AOT 插件使用同一个 WinMD、同一个 DLL 入口和同一种 .hutao 格式。插件不引用主程序实现程序集。

## 架构：从插件源码到运行

```text
HutaoPlugin.idl → MIDL → Contracts.winmd
                          ├─ C++/WinRT → C++ DLL
                          └─ CsWinRT → C# SDK → Native AOT DLL
原生 DLL + manifest + resources → PluginPack → .hutao
.hutao → 宿主校验/解压 → LoadLibraryExW → HutaoPluginCreate
       → IHutaoPlugin → 生命周期 / IPluginContext / UIElement
```

正式宿主提供服务、管理插件实例与界面；插件不引用主程序实现程序集。C# SDK NuGet 包只是把公共投影及辅助代码提供给插件作者编译使用，Native AOT 发布会将实际使用的代码编入插件 DLL；用户不需要 NuGet。C++ 使用 WinMD 和头文件，不使用这两个 C# NuGet 包。PrototypeHost 是独立调试和 ABI 回归程序，不是热重载器。

**开发顺序**：选择 C# 或 C++ 模板 → 引用 SDK → 实现生命周期及视图/命令 → C# Native AOT Publish 或 C++ Build → PluginPack 打包 → PrototypeHost 调试 → 正式应用安装验证。原生 DLL 修改后必须重启宿主。

## 文档入口

- [架构与开发模型：为什么采用 WinRT、SDK/NuGet 的职责、编译和运行调用链](../../docs/PluginSDK2-Architecture.md)
- [历史技术记录：实际错误、根因、修复提交、复现与回归状态](../../docs/PluginSDK2-EngineeringNotes.md)

- [开发者上手：安装 SDK、C# / C++ 项目、构建、打包与调试](../../docs/PluginSDK2-GettingStarted.md)
- [API 参考：生命周期、设置、UI、命令、线程与资源所有权](../../docs/PluginSDK2-API.md)
- [包格式、发布安全与宿主行为](../../docs/PluginSDK2-Packages.md)
- [维护者测试矩阵、故障定位与旧版迁移](../../docs/PluginSDK2-Testing.md)
- [原始设计规范与未完成门槛](../../docs/Snap.Hutao.Plugin.SDK-2.0-Design.md)

## 目录和职责

| 目录 | 职责 |
|---|---|
| Snap.Hutao.Plugin.Contracts | 唯一 MIDL 契约和 C ABI 头文件 |
| Snap.Hutao.Plugin.Projection | 由 CsWinRT 生成的 C# 投影，宿主和 C# SDK 共同使用 |
| Snap.Hutao.Plugin.SDK | C# facade（HutaoPlugin、PluginContext、PluginSetting、PluginExport） |
| Snap.Hutao.Plugin.SDK.Native | C++ 入口辅助头文件 |
| Snap.Hutao.Plugin.Packaging | 宿主和打包工具共用的纯包解析、哈希、PE 校验 |
| TestPlugin.Aot / TestPlugin.Native | 可运行示例、测试资源和 manifest 模板 |
| templates | 仓库外开发的项目起点 |
| tools/PluginPack | pack/verify 命令行工具 |
| tests/PrototypeHost | 标准 WinUI 3 独立开发/回归宿主；不负责正式安装 |
| build | 生成契约、示例包、本地 SDK 分发和包验证测试 |
| ../Snap.Hutao.Remastered/.../Service/Plugin | 正式宿主实现；PrototypeHost 链接同一源码，不存在独立 Host 运行时程序集 |

## 快速开始（维护者）

Visual Studio 2026 打开 `Snap.Hutao.Plugin.SDK.slnx`，Release|x64，Rebuild Solution。然后在本目录：

```powershell
dotnet publish .\TestPlugin.Aot\TestPlugin.Aot.csproj -c Release -p:Platform=x64 -p:PublishProfile=NativeAot
pwsh -File .\build\Pack-Samples.ps1 -Configuration Release
pwsh -File .\build\Test-Packaging.ps1
```

生成三份包：`artifacts\TestPlugin.Native.Core.hutao`（C++ 程序式与松散 XAML）、`artifacts\TestPlugin.hutao`（C# AOT 程序式、松散 XAML、Binding），`artifacts\TestPlugin.Native.hutao`（C++ 编译期 Page 实验，仅 PrototypeHost）。正式应用拒绝 compiled 模式，不会自动降级。

使用 PrototypeHost 的 C++ core / C# AOT sample 按钮选择包，Inspect and load 并确认信任。下拉选择每个视图，点击控件、选择 Exercise ABI 命令并点击 Run command、完成对话框、Disable and re-enable、Uninstall session。替换 DLL 必须关闭整个宿主进程；不支持原生 DLL 热卸载或热替换。

本地生成 SDK（不上传 NuGet）：

```powershell
pwsh -File .\build\Build-Sdk.ps1 -Version 2.0.0-preview.1 -IncludePrototypeHost
```

输出 `artifacts\sdk\2.0.0-preview.1`；开发者可以只使用该分发目录，不需要引用 Snap Hutao 主工程。NuGet.org 发布需要独立的发布决策与验收。

**验证状态**：旧 Debug 测试中两种原生插件均已加载并显示 UI，C++ compiled Page 曾成功；C# 后续异步修复、标准模板 Release/AOT、Binding、新正式宿主及本地 SDK 分发尚需真实 Windows 验收。未经过验收的功能不能视作已经发布。详见测试矩阵。
