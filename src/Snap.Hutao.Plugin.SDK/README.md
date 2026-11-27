# Snap Hutao Plugin SDK 2.0

SDK 2.0 是 Snap Hutao Remastered 的进程内 WinRT 原生插件开发工具。C++/WinRT 与 C# Native AOT 插件使用同一套 WinMD、同一个 DLL 入口和同一种 .hutao 格式。插件不引用主程序实现程序集。

## 架构：从插件源码到运行

```text
HutaoPlugin.idl → MIDL → Contracts.winmd
C# 服务接口 → CsWinRT Authoring → Services.winmd
                          ├─ C++/WinRT → C++ DLL
                          └─ CsWinRT → C# SDK → Native AOT DLL
原生 DLL + manifest + resources → PluginPack → .hutao
.hutao → 宿主校验/解压 → LoadLibraryExW → HutaoPluginCreate
       → IHutaoPlugin → 生命周期 / IPluginContext / UIElement
```

正式宿主提供服务、管理插件实例与界面；插件不引用主程序实现程序集。C# SDK NuGet 包只是把公共投影及辅助代码提供给插件作者编译使用，Native AOT 发布会将实际使用的代码编入插件 DLL；用户不需要 NuGet。C++ 使用 WinMD 和头文件，不使用这两个 C# NuGet 包。PrototypeHost 是独立调试和 ABI 回归程序，不是热重载器。

**开发顺序**：选择 C# 或 C++ 模板 → 引用 SDK → 实现生命周期及视图/命令 → C# Native AOT Publish 或 C++ Build → PluginPack 打包 → PrototypeHost 调试 → 正式应用安装验证。原生 DLL 修改后必须重启宿主。

新增业务服务使用 C# First：主程序提供真实业务实现，C# 接口自动生成 WinMD，C# / C++ 消费同一 ABI。核心 IDL 暂时保留。首个服务为游戏路径定位，详见 [Authoring](../../docs/PluginSDK2-Authoring.md)。

主项目直接 Authoring 迁移可通过 `-p:HutaoMainAuthoring=true` 单独构建；普通构建不启用，以便逐步修复 CsWinRT 2.x 不兼容公开 API。SourceGeneration PR 的本地测试包可使用 `build/Pack-SourceGenerationReview.ps1` 生成，主项目指定测试版本后自动补充本地 NuGet feed。详细命令见 [Authoring](../../docs/PluginSDK2-Authoring.md)。

## 文档入口

- [v1 → v2 详细迁移：项目、生命周期、设置、导航、XAML、游戏附加库的代码对照](../../docs/PluginSDK2-Migration.md)
- [XAML 开发指南：可复制示例、绑定、XBF、PRI 和路径机制](../../docs/PluginSDK2-Xaml.md)

- [架构与开发模型：为什么采用 WinRT、SDK/NuGet 的职责、编译和运行调用链](../../docs/PluginSDK2-Architecture.md)
- [历史技术记录：实际错误、根因、修复提交、复现与回归状态](../../docs/PluginSDK2-EngineeringNotes.md)

- [开发者上手：安装 SDK、C# / C++ 项目、构建、打包与调试](../../docs/PluginSDK2-GettingStarted.md)
- [API 参考：生命周期、设置、UI、命令、线程与资源所有权](../../docs/PluginSDK2-API.md)
- [包格式、发布安全与宿主行为](../../docs/PluginSDK2-Packages.md)
- [维护者测试矩阵、故障定位与旧版迁移](../../docs/PluginSDK2-Testing.md)
- [原始设计规范与未完成门槛](../../docs/Snap.Hutao.Plugin.SDK-2.0-Design.md)

## 目录和职责

```text
Snap.Hutao.Plugin.SDK/
  contracts/                      # 核心 IDL、C ABI 与 C# Authoring 业务服务
  libraries/
    Snap.Hutao.Plugin.Projection/ # C# WinRT 投影
    Snap.Hutao.Plugin.SDK/        # C# SDK facade
    Snap.Hutao.Plugin.SDK.Native/ # C++ SDK 头文件
    Snap.Hutao.Plugin.Packaging/  # 共用包校验
  samples/
    CSharpAot/                    # C# 原生 AOT 示例
    CppCore/                      # C++ 程序式/松散 XAML，无 Page/XBF
    CppCompiled/                  # C++ 编译期 Page + 内嵌 PRI
  templates/                      # 第三方 C#/C++ 独立项目模板
  tools/PluginPack/                # pack / verify
  tests/PrototypeHost/             # 独立 WinUI 调试宿主
  build/                          # 契约、构建、打包、分发、测试脚本
  artifacts/                      # 生成产物（gitignore）
```

正式宿主实现仍位于主程序 Service/Plugin，PrototypeHost 通过 Compile Link 复用核心宿主源码，不存在独立 Host 运行时程序集。三个示例都有独立入口 DLL；C++ Core 不依赖 compiled 的 Page 生成代码。新松散 XAML 使用标准 .xaml 扩展名、Build Action=None；旧 .hxaml 不受支持，必须改为 .xaml。

## 快速开始（维护者）

日常开发只使用 Visual Studio 2026，**不运行构建脚本**：

1. 打开 Snap.Hutao.Plugin.SDK.slnx，选择 Debug | x64（发布时使用 Release）。
2. Build Solution：自动生成 WinMD / C# 与 C++ 投影、构建 C++ DLL 和 PrototypeHost。C# 普通 Build 仅供编辑与编译诊断。
3. 右键 Samples/TestPlugin.Aot → Publish → 选择 NativeAot profile → Publish，输出到 artifacts/aot-plugin。只修改 C++ 时无需重新 Publish C#。
4. 在 SDK 根目录执行唯一的日常打包命令：

```powershell
pwsh -File .\build\Pack-Samples.ps1 -Configuration Debug
```

只打包一个样例时追加 -Sample CSharpAot、CppCore 或 CppCompiled。Configuration 指 C++ 的 VS 构建配置，C# 一律使用已 Publish 的原生 DLL。打包脚本不编译任何插件；若输出不存在，回到 VS 构建或发布。

生成三份包：artifacts/TestPlugin.Native.Core.hutao、artifacts/TestPlugin.hutao、artifacts/TestPlugin.Native.hutao（compiled 使用组件 DLL + 内嵌 XBF 的 PRI，不含独立 XBF；主程序已接入，待验收）。将 PrototypeHost 设置为启动项目，F5，选择对应样例按钮 → Inspect and load → 信任。测试所有视图、Exercise ABI、Disable and re-enable、Uninstall session。修改 DLL 后关闭整个宿主进程再 Build/Publish/pack；不支持热卸载。正常 UI 测试使用 .hutao，不要直接加载裸 DLL。

生成 JIT/AOT 测试宿主：右键 PrototypeHost → Publish，分别选择 JitRelease / NativeAot。完整流程及已报告结果见 [测试指南](../../docs/PluginSDK2-Testing.md)。

Generate-Contracts.ps1 是 MSBuild 自动调用的内部实现，作者无需运行；Test-Packaging.ps1 是维护者回归测试；Build-Sdk.ps1 是 SDK 分发打包，不是插件日常开发步骤。旧 Build-Samples.ps1 已移除。

本地生成 SDK（不上传 NuGet）：

```powershell
pwsh -File .\build\Build-Sdk.ps1 -Version 2.0.0-preview.1 -IncludePrototypeHost
```

输出 `artifacts\sdk\2.0.0-preview.1`；开发者可以只使用该分发目录，不需要引用 Snap Hutao 主工程。NuGet.org 发布需要独立的发布决策与验收。

**验证状态**：2026-11-27 用户确认拆分前的 C++ Core 已正常加载；新目录和独立示例仍需重新构建验收。旧 Debug 测试中两种原生插件均已加载并显示 UI，2026-11-27 用户确认新版内嵌 PRI 路径成功：命中 Files/TestPlugin.Native/PluginPage.xbf，生成 InitializeComponent 返回、两次 Click 和 Exercise ABI 成功；主程序新增接入尚待编译/运行验收；C# 后续异步修复、标准模板 Release/AOT、Binding、新正式宿主及本地 SDK 分发尚需真实 Windows 验收。未经过验收的功能不能视作已经发布。详见测试矩阵。
