# Snap Hutao Plugin SDK 2.0：开发者上手

本指南针对 SDK 2.0 源码及本地预发布分发目录。适用于 x64 Windows，目标 .NET 11、Windows App SDK 2.6.6-experimental、CsWinRT 2.3.1、C++/WinRT 3.0.260818.1、Windows SDK 10.0.28000.0。实际版本以仓库 Directory.Packages.props 为准。不要独立升级 WinAppSDK 或改用其他架构来解决 ABI 问题。本文中的 2.0.0-preview.1 是本地构建版本示例，不表示包已公开发布。

如果需要访问胡桃真实业务服务而不只是开发自己的 UI，请阅读 [C# First Authoring 与业务服务调用](PluginSDK2-Authoring.md)。当前新增 IHutaoGamePathService；C# SDK 和 C++ WinMD 均提供该接口。

## 0. 最短路径：先让你的第一个插件出现一个导航项

你只需要 SDK 分发目录，不需要引用整个主程序。假设分发已解压到 C:\HutaoSdk（没有分发时按第 2 节从仓库生成）。

### 从 Visual Studio 的「创建新项目」创建插件

1. 把 `nuget/Snap.Hutao.Plugin.SDK.<版本>.nupkg` 当作 ZIP 打开，取出 `tools/VisualStudio/` 下需要的模板 ZIP。`Snap.Hutao.Plugin.CSharp.zip` 是 C# Native AOT，`Snap.Hutao.Plugin.Cpp.zip` 是传统 C++/WinRT 头文件版，`Snap.Hutao.Plugin.CppModules.zip` 是 C++/WinRT 3.0 命名模块版。将 ZIP 原样复制到 Visual Studio 设置里的「用户项目模板位置」。默认是 `文档/Visual Studio <版本>/Templates/ProjectTemplates`。
2. 重启 Visual Studio，在「创建新项目」中搜索 **Snap Hutao Plugin**。项目名会用于 DLL 文件名和 manifest 中的默认 ID。默认 ID 只接受 ASCII 字母、数字、点、下划线和连字符。发布前请把 `manifest.template.json` 中的示例 ID 和作者改成自己的信息。
3. C# 模板通过 NuGet 引用同版本的 SDK。设置包含 SDK 和 Projection 两个包的源，使用项目自带的 `NativeAot` 发布配置。C++ 模板把项目的 `HutaoNativeSdkRoot` 指向从同一个 NuGet 包解出的 `tools/native`，或完整分发中的 `native`。该目录应包含 `include` 和 `metadata`。两个 C++ 模板都需要 C++ 桌面开发工具和项目指定的 Windows SDK。命名模块版使用 VS 2026 18.7、v145 工具集及 C++/WinRT 3.0，在项目中生成并导入 `winrt.*` 模块。头文件版通过 `#include` 使用同版本的 WinRT 投影。
4. 在项目目录运行 `Pack-Plugin.ps1`，用 `-SdkDirectory` 指向含 `PluginPack/PluginPack.exe` 的完整 SDK 分发目录。

Visual Studio 会扫描用户模板目录，不需要安装 VSIX（Visual Studio 扩展）。NuGet 的项目依赖安装发生在项目创建之后，因此项目模板 ZIP 需要手动复制。

### 复制源码模板

**C#：** 复制 templates/CSharpPlugin 到 C:\MyPlugin。在 Visual Studio 的 Tools → NuGet Package Manager → Package Manager Settings → Package Sources 添加 C:\HutaoSdk\nuget（保留 Windows App SDK 所需源）。打开 MyPlugin.csproj，右键项目 → Publish → NativeAot → Publish，输出 `publish/MyPlugin.dll` 和 `publish/MyPlugin.pri`。然后在项目目录运行：

```powershell
pwsh -File .\Pack-Plugin.ps1 -SdkDirectory C:\HutaoSdk
```

**C++：** 复制 templates/CppPlugin 到 C:\MyNativePlugin。打开 MyNativePlugin.vcxproj，把 HutaoNativeSdkRoot 属性设为 C:\HutaoSdk\native，选择 Release | x64，Build 项目。然后在项目目录运行：

```powershell
pwsh -File .\Pack-Plugin.ps1 -SdkDirectory C:\HutaoSdk -Configuration Release
```

这两个打包脚本只消费已有构建产物，不调用 dotnet publish 或 MSBuild。预期包分别为 artifacts/MyPlugin.hutao、artifacts/MyNativePlugin.hutao。打开 PrototypeHost → 填入包路径 → Inspect and load → 信任 → 点击视图按钮；正式主程序安装后会创建真实导航项。脚本保留可选的 `resources/` 目录。C# 页面作为 `Page` 在构建时编译，随 DLL 同名的 PRI 部署。原生 DLL 修改后必须重启宿主。C# 必须 Native AOT Publish，普通 Build 仅供编译检查。旧插件参见 [迁移指南](PluginSDK2-Migration.md)，UI 参见 [XAML 指南](PluginSDK2-Xaml.md)。

示例源码位于 `samples/`，共享契约位于 `contracts/`，SDK 项目位于 `libraries/`。

## 1. 先理解构建与运行边界

宿主是已经启动 WinUI 3 的 Snap Hutao Remastered.exe，插件是 Windows 原生 DLL（动态链接库）。宿主使用 LoadLibraryExW 映射 DLL，调用约定为 Cdecl 的 HutaoPluginCreate 导出，取得 IHutaoPlugin 的 WinRT 接口。C# 插件需要 Native AOT（预先把 IL 编译为机器代码），普通 dotnet build 产生的托管 DLL 不是可安装的插件入口。C++ 由 MSVC 链接器直接生成原生 DLL。两者都不需要把宿主的程序集作为编译引用。

SDK 不是宿主：SDK 提供契约和辅助实现；PrototypeHost 是轻量的独立测试程序；PluginPack 是打包和校验工具；正式宿主负责信任确认、持久化包、启用/停用、更新、UI 和设置。

```text
C# 插件源码 → SDK NuGet + CsWinRT 投影 → XAML 编译 + Native AOT 发布 → 原生 DLL + 组件 PRI
C++ 插件源码 → Contracts.winmd + C++/WinRT → Link → native DLL
native DLL + manifest + 资源 → PluginPack → .hutao
.hutao → 宿主校验/解压/加载 → IHutaoPlugin → 真实 WinRT UI 与宿主服务
```

NuGet 只负责给 C# 开发者分发编译所需的 SDK/投影。它不是运行时加载器，最终用户不需要安装 NuGet 包。仓库内 ProjectReference 可以替代 NuGet；仓库外推荐 NuGet 本地源或以后发布的公开源。C++ 使用同一个 WinMD，不用 C# SDK NuGet。详细依赖方向、ABI 调用链和各项目职责见 [架构](PluginSDK2-Architecture.md)。开发中遇到的构建和运行问题见 [技术记录](PluginSDK2-EngineeringNotes.md)。

## 2. 工具链与获取 SDK

在 Windows 安装 Visual Studio 2026、Desktop development with C++、C++ WinUI 开发组件、Windows SDK 10.0.28000.0、.NET 11 SDK、PowerShell 7。C# 也需要 C++ 链接工具才能完成 Native AOT 发布。打开 Developer PowerShell for VS 可确保 MSVC 工具可用。

### 从宿主仓库构建一份 SDK 分发

在仓库根目录的 Developer PowerShell 运行。已有同名版本输出时，换一个预览版本号以免混用不同构建。

```powershell
pwsh -File .\src\Snap.Hutao.Plugin.SDK\build\Build-Sdk.ps1 -Version 2.0.0-preview.1 -IncludePrototypeHost
```

脚本先对 Projection 和 SDK 项目分别运行 `dotnet pack`，把同一个 `-Version` 传给两个项目。Projection 编译消费 WinRT 契约所需的 C# 投影。SDK 包引用这一版本的 Projection，并在 `build/Snap.Hutao.Plugin.SDK.targets` 提供 C# 编译型插件的 MSBuild 规则，在 `tools/VisualStudio/` 放入三份 VS 模板，在 `tools/native/` 携带 C++ 需要的 WinMD 和头文件。脚本随后发布打包器 `PluginPack.exe`，复制文档和源码模板。

从 `src/Snap.Hutao.Plugin.SDK` 看，输出结构如下。

```text
artifacts/sdk/2.0.0-preview.1/
  nuget/
    Snap.Hutao.Plugin.SDK.2.0.0-preview.1.nupkg
    Snap.Hutao.Plugin.Projection.2.0.0-preview.1.nupkg
  native/include/HutaoPlugin.hpp
  native/include/HutaoPluginAbi.h
  native/metadata/Snap.Hutao.Plugin.Contracts.winmd
  native/metadata/Snap.Hutao.Plugin.Services.winmd
  native/metadata/HutaoPlugin.idl
  Snap.Hutao.Plugin.Native.2.0.0-preview.1.zip
  PluginPack/PluginPack.exe
  PrototypeHost/                     # -IncludePrototypeHost 时生成
  templates/                         # 仓库外项目模板
  docs/                              # 随包文档
```

`Build-Sdk.ps1` 只写本地分发目录。插件作者从 `nuget/` 还原使用这两个开发依赖，编译后用 `PluginPack` 生成用户安装的 `.hutao`。C++ 直接消费 WinMD（Windows 元数据），由 C++/WinRT 生成头文件或模块。

### 把分发目录加入项目的包源

若已有分发产物，直接使用它，无需克隆仓库。在 Visual Studio 的 Tools → NuGet Package Manager → Package Manager Settings → Package Sources 添加 C:\HutaoSdk\nuget，名称可设为 HutaoPluginSDK。命令行等价操作如下。

```powershell
dotnet nuget add source C:\HutaoSdk\nuget --name HutaoPluginSDK
```

保留 nuget.org 及获取所需预览 Windows App SDK 的仓库包源。使用 Central Package Management 的项目应在自己的 Directory.Packages.props 中声明版本并按实际源设置 packageSourceMapping；不要把浮动版本写入可复现构建。

## 3. 从零创建 C# 插件

从 VS 模板创建或复制 `templates/CSharpPlugin`。首先打开 `PluginPage.xaml` 修改按钮文案，保留 `x:Class` 与 `x:Name`。WinUI 编译器用前者把页面连接到 `PluginPage.xaml.cs` 的 partial class，用后者让 C# 代码能访问按钮。然后在 `Plugin.cs` 找到这两行：

```csharp
Context.RegisterView("main", CreateView);
Context.RegisterNavigationItem("main", "My Plugin", "\uE74C", "main");
```

第一行把创建页面的函数登记为 `main`；第二行告诉宿主点击这一导航项时调用哪个页面工厂。工厂中的 `new PluginPage()` 会执行 `InitializeComponent` 并读取编译好的页面，关闭页面时用 `PluginView` 的清理回调解除事件订阅。完整代码与构建链见 [C# 编译型页面教程](PluginSDK2-Xaml.md#c-编译型页面)。

在 Developer PowerShell 中运行，或在 VS 中选择项目自带的 `NativeAot` 发布配置：

```powershell
dotnet publish .\MyPlugin.csproj -c Release -p:Platform=x64 -p:PublishProfile=NativeAot -r win-x64
pwsh -File .\Pack-Plugin.ps1 -SdkDirectory C:\HutaoSdk
```

从 VS 模板创建时，用实际项目文件名替换 `MyPlugin.csproj`。确认 `publish/` 下同时有同名的 `.dll` 和 `.pri`。DLL 由 Native AOT（提前编译为机器代码）发布，PRI（包资源索引）保存 XAML 编译出的 XBF。用 `dumpbin /exports .\publish\MyPlugin.dll` 检查 `HutaoPluginCreate` 和 `DllGetActivationFactory`。打开 PrototypeHost，加载 `artifacts/MyPlugin.hutao`，选择页面并点击按钮，预期出现插件发出的通知。

## 4. 从零创建 C++/WinRT 插件

使用分发目录 templates/CppPlugin，或者在 VS 创建 x64 C++ DLL 项目；不需要为了程序式 UI 创建 WinUI Application。参考仓库 samples/CppCore/TestPlugin.Native.Core.vcxproj 的工具链配置。复制源码模板时默认使用头文件；可以通过 Visual Studio 的命名模块项目模板直接选择模块版。若模板移到其他目录，设置 MSBuild 属性 HutaoNativeSdkRoot 指向 SDK 分发目录中的 native 文件夹。

关键配置是 CppWinRT NuGet 包、Windows App SDK NuGet 包、Windows SDK、windowsapp.lib、契约 WinMD 的 Reference/HintPath，以及 SDK include 路径。C++/WinRT 的投影生成器从 WinMD 产生 `winrt/Snap.Hutao.Plugin.Contracts.h`。传统头文件模式无需 XamlGeneratedWorkaround.h；它仅用于仓库编译期 XAML + C++20 modules 的实验项目。

两个 VS C++ 模板共用 `MyNativePlugin.vcxproj` 源文件。打包时分别写出独立项目，传统模板的 `CppWinRTBuildModule` 是 `false`，模块模板是 `true`：

| 选项 | MSBuild 行为 | `MyPlugin.cpp` 入口 |
|---|---|---|
| 头文件 | C++/WinRT 生成头文件 | `#include <winrt/Microsoft.UI.Xaml.Controls.h>` |
| 命名模块 | C++/WinRT 生成并编译模块接口，并启用 STL 模块与 `WINRT_IMPORT_MODULE` 宏 | `import winrt.Microsoft.UI.Xaml.Controls;` |

两种投影使用同一套 WinMD（Windows 元数据）和插件 ABI（二进制接口）。模块模板需要 VS 2026 18.7 和 v145。若从源码复制后切换选项，先清理项目的 `obj` 中间输出再重新构建，避免复用旧模式的生成文件。

最小导出：

```cpp
#include <windows.h>
#include <winrt/Windows.Foundation.h>
#include <winrt/Microsoft.UI.Xaml.h>
#include <winrt/Microsoft.UI.Xaml.Controls.h>
#include <winrt/Snap.Hutao.Plugin.Contracts.h>
#include "HutaoPlugin.hpp"

using namespace winrt;
using namespace Snap::Hutao::Plugin::Contracts;

struct MyPlugin : implements<MyPlugin, IHutaoPlugin>
{
    void OnLoad(IPluginContext const& context)
    {
        context.Log(NotificationKind::Information, L"Loaded");
    }

    void OnInstall() {}
    void OnEnable() {}
    void OnDisable() {}
    void OnUninstall() {}
};

extern "C" __declspec(dllexport) HRESULT __cdecl HutaoPluginCreate(uint32_t abiMajor, ::IInspectable** result)
{
    return hutao::plugin::create<MyPlugin>(abiMajor, result);
}
```

完整的程序式 Button 和 IPluginViewFactory 示例在 templates/CppPlugin/MyPlugin.cpp。若改成纯生命周期示例，manifest 的 ui.mode 应为 none，移除 navigation、xaml 等不使用的能力。

在 VS 编辑 MyNativePlugin.vcxproj 中的 HutaoNativeSdkRoot 指向 C:\HutaoSdk\native，选择 Release | x64 后右键 Build。产物为 bin/Release/MyNativePlugin.dll。必要时在 Developer PowerShell 用 dumpbin /exports 检查入口。

C++ 模块模式开发者可参考 samples/CppCore/ModuleCommand.cpp 与 WinRTModules.h。不要直接编辑生成的 .g.h/.g.cpp；生成器重新执行会覆盖它们。Release DLL 需满足目标机器上的 VC 运行时依赖。Debug CRT DLL 只适合本地调试，不作为发布包。

## 5. 制作 manifest 和 .hutao 包

使用 [包格式参考](PluginSDK2-Packages.md) 中的 schemaVersion 2。C# 模板已包含声明 `ui.mode: compiled` 和页面元数据提供程序的 manifest，id 是开发者自己唯一选择的标识，不能与其他插件重复。id 不是 DLL 文件名，入口路径由 runtime.entry 决定。

日常只需使用第 0 节的 Pack-Plugin.ps1。以下为脚本等价的手动打包过程，供自定义包结构时参考（无需重复执行）：

```powershell
New-Item -ItemType Directory -Force .\stage\bin\x64 | Out-Null
Copy-Item .\publish\MyPlugin.dll, .\publish\MyPlugin.pri .\stage\bin\x64
& C:\HutaoSdk\PluginPack\PluginPack.exe pack .\manifest.template.json .\stage .\MyPlugin.hutao
& C:\HutaoSdk\PluginPack\PluginPack.exe verify .\MyPlugin.hutao
```

C++ 替换入口文件为 bin\Release\MyNativePlugin.dll，保持 staging 的相对路径与其 manifest 一致。只复制插件自己的依赖和资源。不要复制宿主、Windows App SDK 整套 DLL 或 Native AOT 的普通 managed Build 输出。

打包器生成根 manifest.json 中的 files SHA256 列表，校验 ZIP 路径及原生 PE DLL。不要手工改打包后的文件；任何字节改变均需要重新打包和信任确认。模板 Pack-Plugin.ps1 已封装上述操作，不需要自己编写构建脚本。V2 不在普通 C# Build 后自动打包，因为其产物不等于 Native AOT Publish 产物。

## 6. 调试、快速迭代与安装

PrototypeHost 是正常的 WinUI 3 应用，可以从 SDK 分发目录直接启动，也可在仓库中设为启动项目。它不依赖 Snap Hutao 的数据库、登录、游戏服务或窗口结构；它链接正式宿主的会话、加载器、ABI context、设置存储源码。它允许测试包及显式信任的开发 DLL；正式主程序只接受 .hutao/.hutaodisabled 包。推荐测试打包后的真实 .hutao，避免缺少资源和依赖。

```powershell
& C:\HutaoSdk\PrototypeHost\PrototypeHost.exe (Resolve-Path .\MyPlugin.hutao).Path
```

命令行仅填入路径，不跳过确认。点击 Inspect and load，确认 ID/版本/哈希，选择下拉中的视图并测试。对于你自己的插件，在 Plugin command 下拉框选择已注册的任意命令，点击 Run command；仓库 TestPlugin 的 Exercise ABI 只是其中一个回归命令。正式插件管理页也可执行已注册的命令。使用 VS 调试器 Attach to Process；C++ 选择 Native，C# Native AOT 使用 Native 调试器和对应发布 PDB，普通 C# 托管调试器不能把 AOT DLL 当普通 IL 模块调试。PDB 单独保留，通常不打入用户包。

更新 DLL：关闭整个 PrototypeHost 进程 → 重新 Build/Publish → 清理并重建 staging → 重新 pack → 启动新进程。Disable and re-enable 只测试同一已映射模块的生命周期，不是重载。松散 XAML 的内容可以编辑后重新打包，重新启动验证；VS XAML Hot Reload 只针对其支持的调试项目和编译资源，不能承诺任意第三方 Native AOT DLL 热替换。

正式应用：打开插件管理入口 → 安装 .hutao → 检查信任对话框 → 启用 → 点击新增导航。设置管理页读取插件注册的设置，命令按钮执行注册命令。安装 .hutaodisabled 只保存包，不运行代码。编译期 XAML 包可以安装并启用（组件 DLL + 同名 PRI）。C# 编译型模板和样例已接上这条链路，Windows 上的发布、安装与打开页面仍需实测。

## 7. 下一步

- UI、设置、线程和回调：阅读 [API 参考](PluginSDK2-API.md)。
- 版本、包、安全和升级：阅读 [包格式参考](PluginSDK2-Packages.md)。
- 维护者完整测试步骤：阅读 [验收与迁移](PluginSDK2-Testing.md)。
