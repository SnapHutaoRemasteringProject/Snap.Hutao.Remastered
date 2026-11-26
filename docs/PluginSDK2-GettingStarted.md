# Snap Hutao Plugin SDK 2.0：开发者上手

本指南针对 SDK 2.0 源码及本地预发布分发目录。适用于 x64 Windows，目标 .NET 11、Windows App SDK 2.6.6-experimental、CsWinRT 2.3.1、C++/WinRT 3.0.260818.1、Windows SDK 10.0.28000.0。实际版本以仓库 Directory.Packages.props 为准。不要独立升级 WinAppSDK 或改用其他架构来解决 ABI 问题。本文中的 2.0.0-preview.1 是本地构建版本示例，不表示包已公开发布。

## 1. 先理解构建与运行边界

宿主是已经启动 WinUI 3 的 Snap Hutao Remastered.exe，插件是 Windows 原生 DLL（动态链接库）。宿主使用 LoadLibraryExW 映射 DLL，调用约定为 Cdecl 的 HutaoPluginCreate 导出，取得 IHutaoPlugin 的 WinRT 接口。C# 插件需要 Native AOT（预先把 IL 编译为机器代码），普通 dotnet build 产生的托管 DLL 不是可安装的插件入口。C++ 由 MSVC 链接器直接生成原生 DLL。两者都不需要把宿主的程序集作为编译引用。

SDK 不是宿主：SDK 提供契约和辅助实现；PrototypeHost 是轻量的独立测试程序；PluginPack 是打包和校验工具；正式宿主负责信任确认、持久化包、启用/停用、更新、UI 和设置。

```text
C# 插件源码 → SDK NuGet + CsWinRT 投影 → Native AOT Publish → native DLL
C++ 插件源码 → Contracts.winmd + C++/WinRT → Link → native DLL
native DLL + manifest + 资源 → PluginPack → .hutao
.hutao → 宿主校验/解压/加载 → IHutaoPlugin → 真实 WinRT UI 与宿主服务
```

NuGet 只负责给 C# 开发者分发编译所需的 SDK/投影。它不是运行时加载器，最终用户不需要安装 NuGet 包。仓库内 ProjectReference 可以替代 NuGet；仓库外推荐 NuGet 本地源或以后发布的公开源。C++ 使用同一个 WinMD，不用 C# SDK NuGet。详细依赖方向、ABI 调用链和各项目职责见 [架构](PluginSDK2-Architecture.md)。开发中遇到的构建和运行问题见 [技术记录](PluginSDK2-EngineeringNotes.md)。

## 2. 工具链与获取 SDK

在 Windows 安装 Visual Studio 2026、Desktop development with C++、C++ WinUI 开发组件、Windows SDK 10.0.28000.0、.NET 11 SDK、PowerShell 7。C# 也需要 C++ 链接工具才能完成 Native AOT 发布。打开 Developer PowerShell for VS 可确保 MSVC 工具可用。

从源码构建 SDK：进入 `src\Snap.Hutao.Plugin.SDK`，运行：

```powershell
pwsh -File .\build\Build-Sdk.ps1 -Version 2.0.0-preview.1 -IncludePrototypeHost
```

输出目录结构：

```text
artifacts/sdk/2.0.0-preview.1/
  nuget/
    Snap.Hutao.Plugin.SDK.2.0.0-preview.1.nupkg
    Snap.Hutao.Plugin.Projection.2.0.0-preview.1.nupkg
  native/include/HutaoPlugin.hpp
  native/include/HutaoPluginAbi.h
  native/metadata/Snap.Hutao.Plugin.Contracts.winmd
  native/metadata/HutaoPlugin.idl
  Snap.Hutao.Plugin.Native.2.0.0-preview.1.zip
  PluginPack/PluginPack.exe
  PrototypeHost/                     # -IncludePrototypeHost 时生成
  templates/                         # 仓库外项目模板
  docs/                              # 随包文档
```

投影包提供 C# 类型定义及 ABI 封送代码，SDK 包依赖投影包并封装生命周期、设置和回调。C++ 直接消费 WinMD（Windows Metadata，Windows 运行时元数据），由 C++/WinRT 生成头文件或模块。不要自己重新定义 IID（接口标识 GUID）或从宿主反编译契约。

若已有分发产物，直接使用它，无需克隆仓库。以 PowerShell 在你的开发机器上设置本地源：

```powershell
dotnet nuget add source "C:\HutaoSdk\nuget" --name HutaoPluginSDK
```

保留 nuget.org 及获取所需预览 Windows App SDK 的仓库包源。使用 Central Package Management 的项目应在自己的 Directory.Packages.props 中声明版本并按实际源设置 packageSourceMapping；不要把浮动版本写入可复现构建。

## 3. 从零创建 C# 插件

复制分发目录 templates/CSharpPlugin 到你的工作目录（源码仓库中也有）。该示例无需主程序源码。项目文件 MyPlugin.csproj 的关键配置：

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net11.0-windows10.0.26100.0</TargetFramework>
    <TargetPlatformMinVersion>10.0.19041.0</TargetPlatformMinVersion>
    <OutputType>Library</OutputType>
    <RuntimeIdentifier>win-x64</RuntimeIdentifier>
    <PlatformTarget>x64</PlatformTarget>
    <PublishAot>true</PublishAot>
    <NativeLib>Shared</NativeLib>
    <SelfContained>true</SelfContained>
    <WindowsAppSDKSelfContained>false</WindowsAppSDKSelfContained>
    <WindowsAppSdkBootstrapInitialize>false</WindowsAppSdkBootstrapInitialize>
    <WindowsAppSdkDeploymentManagerInitialize>false</WindowsAppSdkDeploymentManagerInitialize>
    <WindowsAppSdkUndockedRegFreeWinRTInitialize>false</WindowsAppSdkUndockedRegFreeWinRTInitialize>
    <CsWinRTGenerateProjection>false</CsWinRTGenerateProjection>
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Snap.Hutao.Plugin.SDK" Version="2.0.0-preview.1" />
    <PackageReference Include="Microsoft.Windows.CsWinRT" Version="2.3.1" />
    <PackageReference Include="Microsoft.WindowsAppSDK" Version="2.6.6-experimental" />
  </ItemGroup>
</Project>
```

为什么 NativeLib=Shared：发布的是 DLL，而不是 EXE。为什么关闭 WinAppSDK 初始化器：宿主已拥有 DispatcherQueue、Application 和 Windows App Runtime，插件不得再次启动应用、初始化第二个 XAML 框架或打包宿主运行时 DLL。WinUI 控件属于宿主所在的 UI 线程。SDK NuGet 包提供托管编译引用，不是运行时安装包。

Plugin.cs 完整最小示例见模板。关键入口：

```csharp
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Snap.Hutao.Plugin.Contracts;
using Snap.Hutao.Plugin.SDK;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

public static unsafe class Exports
{
    [UnmanagedCallersOnly(EntryPoint = "HutaoPluginCreate", CallConvs = [typeof(CallConvCdecl)])]
    public static int Create(uint abiMajor, nint* result) => PluginExport.Create<MyPlugin>(abiMajor, result);
}

public sealed class MyPlugin : HutaoPlugin
{
    public override void OnLoad(PluginContext context)
    {
        context.Log(NotificationKind.Information, "Loaded");
    }

    public override void OnEnable()
    {
        Context.RegisterView("main", CreateView);
        Context.RegisterNavigationItem("main", "My Plugin", "\uE74C", "main");
    }

    private PluginView CreateView()
    {
        Button button = new() { Content = "Click me" };
        void Click(object sender, RoutedEventArgs args)
        {
            Context.Notify(NotificationKind.Success, "Hello from plugin");
        }
        button.Click += Click;
        return new(button, () => button.Click -= Click);
    }
}
```

OnLoad 初始化和注册设置；OnEnable 注册贡献；CreateView 每次宿主打开页面时创建新的 UIElement；返回 PluginView 的第二个参数是关闭回调，用于解除事件订阅。不要缓存一个 Page 给两个不同的宿主位置。

构建与发布：

```powershell
dotnet publish .\MyPlugin.csproj -c Release -r win-x64 -o .\publish
```

确认 `publish\MyPlugin.dll` 是 native DLL（可用 Developer PowerShell 的 dumpbin /exports 检查 HutaoPluginCreate）。不要使用 bin\Debug 或 bin\Release 中的普通 managed DLL 作为入口。

## 4. 从零创建 C++/WinRT 插件

使用分发目录 templates/CppPlugin，或者在 VS 创建 x64 C++ DLL 项目；不需要为了程序式 UI 创建 WinUI Application。参考仓库 TestPlugin.Native.vcxproj 的工具链配置。外部模板使用普通 C++/WinRT 头文件（非 named modules），有意保持简单。若模板移到其他目录，设置 MSBuild 属性 HutaoNativeSdkRoot 指向 SDK 分发目录中的 native 文件夹。

关键配置是 CppWinRT NuGet 包、Windows App SDK NuGet 包、Windows SDK、windowsapp.lib、契约 WinMD 的 Reference/HintPath，以及 SDK include 路径。C++/WinRT 的投影生成器从 WinMD 产生 `winrt/Snap.Hutao.Plugin.Contracts.h`。传统头文件模式无需 XamlGeneratedWorkaround.h；它仅用于仓库编译期 XAML + C++20 modules 的实验项目。

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

在 Developer PowerShell：

```powershell
msbuild .\MyNativePlugin.vcxproj /restore /p:Configuration=Release /p:Platform=x64 /p:HutaoNativeSdkRoot=C:\HutaoSdk\native
dumpbin /exports .\bin\Release\MyNativePlugin.dll
```

C++ 模块模式开发者可参考 TestPlugin.Native/ModuleCommand.cpp 与 XamlWorkaround.h。不要直接编辑生成的 .g.h/.g.cpp；生成器重新执行会覆盖它们。Release DLL 需满足目标机器上的 VC 运行时依赖。Debug CRT DLL 只适合本地调试，不作为发布包。

## 5. 制作 manifest 和 .hutao 包

使用 [包格式参考](PluginSDK2-Packages.md) 中的 schemaVersion 2。C# 模板已包含与上面程序式示例一致的 manifest，id 是开发者自己唯一选择的标识，不能与其他插件重复。id 不是 DLL 文件名，入口路径由 runtime.entry 决定。

在 C# 示例项目目录，将明确需要的文件复制到 staging；下面命令假设分发工具位于 C:\HutaoSdk：

```powershell
New-Item -ItemType Directory -Force .\stage\bin\x64 | Out-Null
Copy-Item .\publish\MyPlugin.dll .\stage\bin\x64\MyPlugin.dll
& C:\HutaoSdk\PluginPack\PluginPack.exe pack .\manifest.template.json .\stage .\MyPlugin.hutao
& C:\HutaoSdk\PluginPack\PluginPack.exe verify .\MyPlugin.hutao
```

C++ 替换入口文件为 bin\Release\MyNativePlugin.dll，保持 staging 的相对路径与其 manifest 一致。只复制插件自己的依赖和资源。不要复制宿主、Windows App SDK 整套 DLL 或 Native AOT 的普通 managed Build 输出。

打包器生成根 manifest.json 中的 files SHA256 列表，校验 ZIP 路径及原生 PE DLL。不要手工改打包后的文件；任何字节改变均需要重新打包和信任确认。可以通过显式 PowerShell 脚本或项目的 Publish 后续 Target 自动化上述命令；V2 没有 v1 的 GenerateHutaoPlugin Build 自动打包语义，因为 C# Build 产物不等于 Native AOT Publish 产物。

## 6. 调试、快速迭代与安装

PrototypeHost 是正常的 WinUI 3 应用，可以从 SDK 分发目录直接启动，也可在仓库中设为启动项目。它不依赖 Snap Hutao 的数据库、登录、游戏服务或窗口结构；它链接正式宿主的会话、加载器、ABI context、设置存储源码。它允许测试包及显式信任的开发 DLL；正式主程序只接受 .hutao/.hutaodisabled 包。推荐测试打包后的真实 .hutao，避免缺少资源和依赖。

```powershell
& C:\HutaoSdk\PrototypeHost\PrototypeHost.exe (Resolve-Path .\MyPlugin.hutao).Path
```

命令行仅填入路径，不跳过确认。点击 Inspect and load，确认 ID/版本/哈希，选择下拉中的视图并测试。对于你自己的插件，在 Plugin command 下拉框选择已注册的任意命令，点击 Run command；仓库 TestPlugin 的 Exercise ABI 只是其中一个回归命令。正式插件管理页也可执行已注册的命令。使用 VS 调试器 Attach to Process；C++ 选择 Native，C# Native AOT 使用 Native 调试器和对应发布 PDB，普通 C# 托管调试器不能把 AOT DLL 当普通 IL 模块调试。PDB 单独保留，通常不打入用户包。

更新 DLL：关闭整个 PrototypeHost 进程 → 重新 Build/Publish → 清理并重建 staging → 重新 pack → 启动新进程。Disable and re-enable 只测试同一已映射模块的生命周期，不是重载。松散 XAML 的内容可以编辑后重新打包，重新启动验证；VS XAML Hot Reload 只针对其支持的调试项目和编译资源，不能承诺任意第三方 Native AOT DLL 热替换。

正式应用：打开插件管理入口 → 安装 .hutao → 检查信任对话框 → 启用 → 点击新增导航。设置管理页读取插件注册的设置，命令按钮执行注册命令。安装 .hutaodisabled 只保存包，不运行代码。编译期 XAML 包暂时只允许在 PrototypeHost 实验，不应安装为正式启用插件。

## 7. 下一步

- UI、设置、线程和回调：阅读 [API 参考](PluginSDK2-API.md)。
- 版本、包、安全和升级：阅读 [包格式参考](PluginSDK2-Packages.md)。
- 维护者完整测试步骤：阅读 [验收与迁移](PluginSDK2-Testing.md)。
