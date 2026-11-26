# SDK 2.0 开发技术记录（Engineering Notes）

本文记录 2026-11-26 至 27 日 aot/plugin 分支开发中的错误、修复机制、复现与尚未验证的风险。先读 [架构](PluginSDK2-Architecture.md) 与 [上手指南](PluginSDK2-GettingStarted.md)。这里保留历史失败方案，**不是当前推荐配置**。测试状态只针对对应提交和环境：Pass 表示有实际报告，Fail 表示实际失败，NotRun 表示缺少后续实测。部分原始日志未完整留在仓库中，下面仅摘录能确认的错误，不补造 HRESULT、输出或测试结果。旧版本通过不能替代当前 HEAD 的验收。

## 1. 工具链、MIDL 与投影生成

### E01：WinMD 引用不完整或版本混用

现象：早期脚本要求手工提供 WinAppSDK WinMD 文件；仅提供 WinUI metadata 目录并不能保证其引用的其他类型都有定义。WinMD 是 Windows Runtime 元数据，不是 CsWinRT 生成的托管 DLL。Microsoft.UI.Xaml.winmd 的类型还引用 Composition、Dispatching、Input、InteractiveExperiences 等组件；MIDL、cppwinrt、cswinrt 必须使用同一套已还原依赖。

修复链：[9f9d842](https://github.com/SnapHutaoRemasteringProject/Snap.Hutao.Remastered/commit/9f9d842728904fd32b13de568a1ac5b638881028) 接收文件或目录；[3b143fe](https://github.com/SnapHutaoRemasteringProject/Snap.Hutao.Remastered/commit/3b143fe921f0c8ac3f949346aba8ec505eeff88f) 将生成挂到 VS/MSBuild；[0d8a64d](https://github.com/SnapHutaoRemasteringProject/Snap.Hutao.Remastered/commit/0d8a64d3d2679cb7661c273a698c151cfc0b4a9e) 处理版本化目录和 WebView2 WinMD。

当前实现：Projection.csproj 中 GenerateHutaoContracts 依赖 ResolvePackageAssets，在 CsWinRTGenerateProjection 前调用 build/Generate-Contracts.ps1。脚本从该项目 obj/project.assets.json 的 libraries、packageFolders 获取实际还原版本，而不是枚举整个 NuGet 缓存。对同一组件同名 WinMD，选择不超过 TargetPlatformVersion 的最高版本化 metadata，缺少时使用未版本化项；拒绝同名重复文件，拷贝到 artifacts/references。MIDL 使用 Windows SDK UnionMetadata/Windows.winmd 与这批引用生成唯一 Contracts.winmd。C++ solution build dependency 保证先构建投影项目；不要并行执行两个独立的 solution build 写同一 artifacts 目录。

验证：Debug x64 的实际构建日志显示已枚举依赖、MIDL 处理 IDL、生成 C# 与 C++ 投影；旧配置构建通过。精确的早期缺失类型错误未保留，不将推断写作具体报错。当前分支重新构建 NotRun。

### E02：MSBuild 启动 MIDL 的 C 预处理器

MIDL 调用 C/C++ 预处理器 cl.exe 处理 IDL。C# MSBuild Exec 的 PATH 不一定含 VS C++ 工具路径，所以从普通 VS 或 dotnet publish 构建不能假设已进入 Native Tools shell。

[0d8a64d](https://github.com/SnapHutaoRemasteringProject/Snap.Hutao.Remastered/commit/0d8a64d3d2679cb7661c273a698c151cfc0b4a9e) 增加 Find-MsvcCompiler：优先 VSInstallDir/VSINSTALLDIR，适用时 VCToolsInstallDir，随后 vswhere 和 PATH；读取 Microsoft.VCToolsVersion.default.txt，不写死 VS edition 或 MSVC 小版本。定位后只为脚本进程补充 cl.exe 的目录。VSInstallDir 后附 . 是避免引号前的尾部反斜杠影响参数解析。

实际日志定位 VS 18、MSVC 14.52.37025 的 Hostx64/x64/cl.exe，Windows SDK 10.0.28000.0；项目 TargetPlatformVersion 10.0.26100.0 与工具 SDK 版本是不同概念。后续 MIDL 处理成功，当前脚本未重新跑。

### E03：PowerShell 元数据解析脚本损坏

[b8c9ea0](https://github.com/SnapHutaoRemasteringProject/Snap.Hutao.Remastered/commit/b8c9ea0cdfd5ded9ac64538b03d2c207fa306aed) 修复早期 Build-Prototype.ps1 中截断的正则和重复/损坏的 resolver 段。此类错误发生于脚本解析/执行层，不代表 MIDL ABI 定义错误。审查自动文本替换时尤其注意 PowerShell 的 $Matches、正则和引号；改动后首先解析脚本，再真实运行。当前脚本是 build/Generate-Contracts.ps1，旧名称仅用于历史定位。

### E04：MSB3073 / Get-FileHash 导致 Publish 失败

实际错误：Projection.csproj 的 Exec 调用 powershell.exe -NoProfile ... Generate-Contracts.ps1，退出 1，MSBuild 显示 MSB3073。禁用终端日志器后看到 MIDL 已处理元数据，随后 Windows PowerShell 报 Get-FileHash 不是可识别的 cmdlet。

根因不是 Native AOT 编译失败，也不是 NU1507，而是非必需的诊断哈希命令在此环境不可用，ErrorActionPreference=Stop 使整个生成目标失败。修复 [cc13755](https://github.com/SnapHutaoRemasteringProject/Snap.Hutao.Remastered/commit/cc13755c3696a16e6425aefafc08de6d05a3432c) 移除契约生成末尾的哈希打印；没有取消 PluginPackage 的完整性验证。后续用户报告 Projection、SDK、TestPlugin.Aot Release Publish 成功，仍存在 CsWinRT1028/1034、NU1507 警告。Pass（该历史发布）；当前发布 NotRun。

复现/诊断（SDK 根目录）：

```powershell
dotnet publish .\TestPlugin.Aot\TestPlugin.Aot.csproj -c Release -p:Platform=x64 -p:PublishProfile=NativeAot -tl:off -v:normal
```

MSB3073 是外部命令失败的包装错误，先找其之前的第一个具体异常。不要盲改 TFM、SDK 包版本或禁用目标。Build-Sdk.ps1 的分发末尾仍在 PowerShell 7 下输出文件 SHA256；它与契约生成的 Windows PowerShell 5.1 路径不同，须分别验收。

### E05：NU1507 与 CsWinRT1028

NU1507 是 Central Package Management 多源警告，提示配置 packageSourceMapping；不是本次 Publish 失败根因。应按实际源映射，不能为了消除警告切换到不匹配的 WinAppSDK。

CsWinRT1028 提示需跨 WinRT ABI 的实现类型应标记 partial，使 AOT 优化器能生成包装元数据。历史警告涉及 PluginContext、RegistrationHandle 等实现 IDisposable 的包装对象；后续源码标记 partial。并非每个普通 CLR 对象都必须成为 WinRT 对象，需检查真实 ABI 路径，不做 blanket NoWarn。当前警告清单需新构建确认。

## 2. C++/WinRT：头文件、模块、生成代码

### E06：C3779、C2039/C2059 和 C3536

实际 Debug 构建：

- C3779：Button::Click 的 auto 返回类型在定义可见前使用；
- C2039/C2059：TestPlugin 实现类型没有相应 as 成员/模板语法解析失败；
- C3536：action 未初始化，是前述表达式编译失败的连带错误。

C++/WinRT 的 Button 继承 ButtonBase，Click 的消费实现来自 Controls.Primitives 投影。仅包含 Controls 不一定使该方法定义可见；传统头文件模式补 `<winrt/Microsoft.UI.Xaml.Controls.Primitives.h>`，模块模式 import 对应模块。Win32 GetCurrentTime 宏还可能与 Timeline 投影方法冲突，投影前 undef。

对于 `struct TestPlugin : implements<...>`，this 是实现对象指针，不是投影接口。先 `auto self = get_strong();` 得到持有引用的 com_ptr，再 `self.as<IPluginViewFactory>()` / `self.as<IPluginCommand>()` / `lifetime.as<IPluginAction>()`，通过 QI 得到对应接口。不要将 implementation::T* reinterpret_cast 为接口。协程跨挂起前持有 get_strong，避免对象提前析构。

修复：[402d640](https://github.com/SnapHutaoRemasteringProject/Snap.Hutao.Remastered/commit/402d640221db13282e75f4d004a0aa2d3d360a7d)。后续实际 Debug 构建通过；当前新增 CoreView 的 Release 构建 NotRun。

### E07：C2872 TestPlugin 歧义与 C++23 modules

从普通 DLL 改为 WinUI runtime component 模板后，IDL/XAML 投影生成 winrt::TestPlugin 命名空间。全局 struct TestPlugin 与 using namespace winrt 导入的同名 namespace 产生 C2872；不是模块本身改变了类名。

修复 [e46c420](https://github.com/SnapHutaoRemasteringProject/Snap.Hutao.Remastered/commit/e46c420a67ca8c0440ab317fcad46b85a5cf63dd)：实现重命名 TestPluginImpl，导出模板使用该实现；Page 使用完整命名空间 winrt::TestPlugin::Native::implementation::PluginPage。

项目启用 CppWinRTBuildModule、BuildStlModules、C++23，禁用传统 PCH。XamlWorkaround.h 建立 Win32/COM 文本头与投影模块的边界；XamlGeneratedWorkaround.h 为 XAML 编译器仍生成 textual include 的翻译单元提供兼容边界。MSBuild InjectModuleImportsIntoXamlGenerated 在 ComputeXamlGeneratedCompileInputs 后对 XamlTypeInfo.g、XamlTypeInfo.Impl.g、XamlMetaDataProvider、XamlLibMetadataProvider.g 设置 ForcedIncludeFiles；避免同一翻译单元重复消费 textual 与 module 投影。不要手改生成 .g.cpp。

HutaoPlugin.hpp 根据 WINRT_IMPORT_MODULE 选择 import 或 include。`instance.template as<IInspectable>()` 中 instance 类型依赖 T；template 是 C++ 消歧义关键字，告诉编译器 as 是成员模板而非小于运算。

实际用户日志：Debug x64 solution 6 succeeded、0 failed、2 up-to-date；生成的 XAML 编译单元和 DLL 链接成功。Pass（当时编译）；不证明运行或当前 Release 通过。相关参考：[OpenNet XamlWorkaround.h](https://github.com/hoshiizumiya/OpenNet/blob/master/OpenNet/XamlWorkaround.h)、[CppWinRTModulesWinUI3Example](https://github.com/YexuanXiao/CppWinRTModulesWinUI3Example/commit/43a095f8e512252aeb7559577194b482624ea8ac)。普通第三方 C++ 模板仍使用头文件，不要求 modules。

## 3. 原生 DLL、ABI 与生命周期

### E08：Build 产物不是 Native AOT DLL

C# TestPlugin.Aot 普通 Build 产生托管输入，即使项目设置 PublishAot=true 也不能视作 native 发布已完成。必须 NativeLib=Shared、win-x64、dotnet publish，检查 native PE、CorHeader 不存在、导出 HutaoPluginCreate。C++ Link 则直接产出 native DLL。

Pack-Samples.ps1 分离 managed 发布目录和 native C++ 配置目录，显式 staging，只打包插件 DLL 与必要资源。PluginPack 校验 PE Machine、DLL flag、CorHeader；不会证明导出、依赖和运行时兼容。可在 Developer PowerShell 执行：

```powershell
dumpbin /headers .\artifacts\aot-plugin\TestPlugin.Aot.dll
dumpbin /exports .\artifacts\aot-plugin\TestPlugin.Aot.dll
dumpbin /dependents .\artifacts\aot-plugin\TestPlugin.Aot.dll
```

历史发布与打包有成功报告；当前生成三包及负面 fixtures NotRun。

### E09：C ABI、COM 引用与 Native AOT 驻留

入口统一为 Cdecl HutaoPluginCreate(uint32_t abiMajor, IInspectable** result)，HRESULT 返回；初始化 result=null，ABI major 不匹配拒绝。WinRT 接口本身使用其规范 vtable 调用约定，不可把导出 Cdecl 套到所有 COM 方法。NativePluginLoader 取得导出，PluginAbi.Import 查询 IHutaoPlugin IID，释放原始拥有引用；C# PluginExport 初始化本模块 ComWrappers，包装 PluginAdapter。C++ detach_abi 交给调用者一个拥有引用。不要复制 managed 对象地址，不要从宿主和插件分别生成不同 IID。

NativePluginLoader 缓存 HMODULE 不调用 FreeLibrary，PluginResidency 按 ID/路径/版本/hash 限制并隔离失败会话。逻辑 Disable 撤销注册、关闭视图、等待受跟踪异步操作，与物理卸载不同。这个约束不是可选优化；AOT DLL 不支持卸载，WinUI 也可能缓存 metadata 与对象。真实引用计数压力、失败路径与多插件驻留测试 NotRun。参考 [.NET Native AOT libraries](https://learn.microsoft.com/dotnet/core/deploying/native-aot/libraries)。

## 4. 编译期 XAML：为什么 Page 能构造不等于能加载资源

### E10：外部 UserControl / Page 的 ms-appx 资源路径

用户引用 [microsoft-ui-xaml#6299](https://github.com/microsoft/microsoft-ui-xaml/issues/6299)：从应用包或目录之外加载 UserControl，InitializeComponent 抛 XamlParseException，Cannot locate resource from ms-appx:///Plugin/PluginUserControl.xaml。我们先改成 Page（[49c73db](https://github.com/SnapHutaoRemasteringProject/Snap.Hutao.Remastered/commit/49c73db0d1663fbe059a4cd6a37768643a7c8f67)），但实际插件仍报告 Cannot locate resource ... ms-appx:///TestPlugin.Native/PluginPage.xaml。

根因是资源解析边界，不是单纯 Page/UserControl 继承差异：XAML 编译器生成的 InitializeComponent 使用宿主包相对 URI；插件 DLL 解压到外部 workspace，LoadLibrary 并不会把该目录变成宿主应用资源根。XBF 是编译后二进制 XAML，PRI 是资源索引；WinMD/metadata provider 提供类型信息，但不能自动让资源 URI 找到正确 XBF。

### E11：make_self 自动调用 InitializeComponent，延迟加载必须显式实现

关键调用链：`make_self<implementation::PluginPage>()` 构造后会探测并调用可访问的参数为空的 InitializeComponent；此时插件 context 尚未交给 Page。仅把构造函数写成 default，无法阻止自动调用。第一次修复 [153d43f](https://github.com/SnapHutaoRemasteringProject/Snap.Hutao.Remastered/commit/153d43f5ac8a82636820009520bfed1c7934dbc7) 通过 private using 隐藏基类 loader；随后 [aefd31d](https://github.com/SnapHutaoRemasteringProject/Snap.Hutao.Remastered/commit/aefd31d147c3fbef9129ee9c0e1dbb34bf0d46e5) 改为明确的空 InitializeComponent hook，并记录断点前后日志。

```cpp
void PluginPage::InitializeComponent()
{
    // 此时还没有插件 context，不执行生成的宿主相对资源加载。
}
```

构造完成后 InitializePluginView(context) 使用已校验的 PackageDirectory，转换路径分隔符、逐段 URI 转义，形成 ms-appx:///C:/.../views/PluginPage.xaml（实际部署 XBF）；设置 _contentLoaded，调用 Application::LoadComponent(*this, uri, ComponentResourceLocation::Application)。保留生成的 Connect/GetBindingConnector，才能让 x:Bind、命名元素与 Click code-behind 工作。若加载失败，不能复用半初始化对象。

旧 Debug harness 有 Page 加载和显示成功报告；此前的相对 URI 确认失败。完整日志原文未全部保留，不能宣称所有 x:Bind/主题/模板、多实例、URI 或 MSIX 情况通过。复现时看 InitializeComponent deferred、make_self returned、LoadComponent URI、LoadComponent returned successfully，随后实际点击按钮。当前标准模板与 Release/AOT NotRun。

### E12：XAML metadata provider 与控件资源初始化

NativePluginLoader.GetMetadataProvider 从已映射 DLL 的 DllGetActivationFactory 取得 runtimeclass factory，ActivateInstance，QI IXamlMetadataProvider；分别释放临时原始引用。metadata provider 负责解析 XAML 类型及成员，必须与资源路径问题区分。

旧代码式 PrototypeApp 自己实现 IXamlMetadataProvider，初始只枚举插件 provider，缺少正常 WinUI controls provider 和 XamlControlsResources 初始化。[2d05b8f](https://github.com/SnapHutaoRemasteringProject/Snap.Hutao.Remastered/commit/2d05b8ff2b0d2668ccfd4e39fba239660cd89ffb) 补平台 metadata 和控件资源；随后标准模板取代这套手写启动流程（见 E15）。

目前 App.xaml.cs 使用生成的 AddOtherProvider 注册外部 provider，仅限实验。provider 必须驻留，拒绝同名不同路径。生成 API 可用性、生成的 schema cache 和多组件资源行为需 Windows 实测；不使用私有全局资源 hook、不注入宿主安装目录。正式主程序拒绝 compiled 模式。

## 5. C# Native AOT：动态投影、Binding、异步

### E13：CsWinRT1034 / 运行时类型转换

XamlReader.Load 返回 Object/投影对象，FindName 返回的类型不一定在 AOT 静态分析中以具体 runtimeclass 被使用。历史 Publish 出现 CsWinRT1034，运行时视图创建也曾出现 cast 问题。JIT 可以动态解析更多类型，不能由 JIT 成功推断 AOT 成功。

[153d43f](https://github.com/SnapHutaoRemasteringProject/Snap.Hutao.Remastered/commit/153d43f5ac8a82636820009520bfed1c7934dbc7) 在宿主 ParseXaml 标注 DynamicWindowsRuntimeCast(typeof(UIElement))，在插件 CreateViewCore 标注 StackPanel/Button/TextBlock/TextBox 等真实转换目标；使用 as + 具体异常消息区分哪个元素失败。该属性向 CsWinRT 生成器声明需要运行时转换的类型，不是任意 CLR 类型转换的替代，也不能解决缺失 XAML 文件问题。

旧 Debug 有 C# UI 加载报告，当前四组合 NotRun。新的 SampleViewModel 使用 GeneratedBindableCustomProperty、partial、INotifyPropertyChanged，SampleCommand 实现 ICommand；BindingPage.hxaml 通过真正 Binding、TwoWay 和 UpdateSourceTrigger 测试跨独立 runtime 的 CCW。该示例是验证用例，不是已经确认的通用 AOT Binding 支持。事件版视图与 Binding 版必须分别测试。

### E14：Expected UI STA —— 进入 UI 线程不等于 await 后仍处在 UI 线程

实际现象：C# AOT Exercise ABI 在异步链路中触发 Expected UI STA。宿主 DispatcherQueue 由 Windows 原生线程拥有，但宿主 .NET 和插件 Native AOT 的 SynchronizationContext.Current 是两个运行时各自的托管状态。宿主已设置上下文，不代表插件进入同一 OS UI 线程时其 Current 也已设置。

仅在初始 ABI callback 设置 DispatcherQueueSynchronizationContext 仍不足以保证连续 await：Post 投递的回调从 native dispatcher 进入插件时必须重新建立插件侧 Current，否则下一次 await 可能捕获 null，并在 ThreadPool 续体访问 context。

[6b7d0d7](https://github.com/SnapHutaoRemasteringProject/Snap.Hutao.Remastered/commit/6b7d0d7ab147f482c199814c550c8aa6e32c45e3) 在 HutaoPlugin.cs 的 UiCall 中实现 PluginUiSynchronizationContext，每次 Post 通过 queue.TryEnqueue 并在回调内 RunWithContext(this, ...)，finally 恢复原上下文；Send 拒绝跨线程同步调度。SDK adapter 包裹生命周期、view factory、command、action 等；外部 WinUI 事件使用 PluginContext.RunOnUiAsync。它要求已经在 UI，不是 worker 切回 UI 的方法；worker 应使用事先 CaptureDispatcher 得到的 PluginDispatcher。

最小回归思路：

```csharp
PluginDispatcher dispatcher = Context.CaptureDispatcher();
await Task.Yield();
if (!dispatcher.HasThreadAccess) throw new InvalidOperationException("first yield");
await Task.Yield();
if (!dispatcher.HasThreadAccess) throw new InvalidOperationException("second yield");
await counter.SetValueAsync(counter.Value + 1);
if (!dispatcher.HasThreadAccess) throw new InvalidOperationException("setting continuation");
```

再测试 Task.Run 内 HasThreadAccess=false，dispatcher.InvokeAsync 回调=true，返回原 command 的 await 后仍为 true，最后 dialog 完成。不要删除 CheckThread、强行使用 ConfigureAwait(false)、UI 上同步 Wait/Result 或把整个 command 放入 Task.Run。修复后完整 Windows 异步回归 NotRun；错误并非已证明解决。

## 6. PrototypeHost：Debug 能启动不代表 Publish 能启动

### E15：JIT/AOT 发布启动 FailFast、PRI 与正常 XAML 启动链

旧手写 Program.cs 直接 Application.Start，手工构造 Application/Window，未采用标准 App.xaml 生成初始化。Debug harness 能运行，两种独立发布宿主随后均报告启动 FailFast；相关异常类型包括 XAML stowed exception / fail-fast（0xC000027B / 0xC0000602）。这类异常码是最终包装/退出信号，不足以单独证明根因一定是 resources.pri。

[53e19ef](https://github.com/SnapHutaoRemasteringProject/Snap.Hutao.Remastered/commit/53e19efdc50d0dceedd90f16714aa7a58f31b151) 启用 EnableMsixTooling 以使用 PRI 发布链路（仍 unpackaged），捕获 OnLaunched 的原始异常到 %LOCALAPPDATA%/Snap.Hutao.Plugin.SDK.Tests/startup-error.txt。用户随后提交标准模板 [dde7938](https://github.com/SnapHutaoRemasteringProject/Snap.Hutao.Remastered/commit/dde7938dd4c410ce945cde48768240b0db5df499)，[e90a4ad](https://github.com/SnapHutaoRemasteringProject/Snap.Hutao.Remastered/commit/e90a4ad83d7ab67f31f672dc22c017d1c2417cc6) 将旧逻辑迁移到 App.xaml/MainWindow/MainPage，移除重复入口、模板意外覆盖的 .NET 10/ARM64/浮动包版本；后续补 ResolveReferences、GenerateProjectPriFile、发布复制及校验。

当前 PrototypeHost 使用 WindowsPackageType=None、WindowsAppSDKSelfContained=true，并让 WinUI 自身生成启动代码。插件 DLL 则不重复初始化 Windows App Runtime。检查两个真实发布目录的 resources.pri、EXE 类型、WinUI 依赖、XAML 资源，以及启动日志；用 WinDbg 分析 stowed exception 的内部 HRESULT/错误消息，不能只看 RaiseFailFastException 栈。若失败发生在 App.OnLaunched 之前，startup-error.txt 可能不会生成。

复现：按 [Testing](PluginSDK2-Testing.md) 分别执行 JitRelease 与 NativeAot 发布并启动空宿主，再加载包。旧 Publish Fail；新模板 JIT/AOT NotRun。PRI 缺失是重要检查点，不是已经证明的唯一原因。

## 7. 正式宿主、包管理、发布和当前未决项

[e90a4ad](https://github.com/SnapHutaoRemasteringProject/Snap.Hutao.Remastered/commit/e90a4ad83d7ab67f31f672dc22c017d1c2417cc6) 删除独立 Host 项目，将加载器、会话、context、设置存储、驻留移至正式 Service/Plugin；PrototypeHost 用 Compile Link 复用核心源码。后续恢复安装、导航、设置编辑、命令、更新和本地 SDK 分发。生产管理层的事务与主程序窗口/DI 生命周期未由 PrototypeHost 覆盖。

重要决策与回归风险：

| 决策 | 原因及验收 |
|---|---|
| 只用固定 ABI 和两种语言投影 | 不依赖宿主 CLR 类型；ABI 变动需要兼容测试 |
| 包哈希与解压白名单 | 防止误加载、损坏与路径冲突；不是安全沙箱；Test-Packaging.ps1 有负例，未运行 |
| 宿主预编译 PluginHostPage 承载 UIElement | 避免 Frame.Navigate 未知插件类型；测试多插件导航、回退和清理 |
| 模块驻留、逻辑禁用 | 不能安全 FreeLibrary；测试失败、重复启停、回调与对象泄漏 |
| 设置独立 JSON 文件 | 保留同 ID 同类型旧设置；后台原子写入后更新 cache；测试 IO 失败与关闭竞争 |
| 更新暂存、重启后显式 Apply | 不覆盖映射 DLL；备份直到新版本成功；异常退出/事务中断仍需故障注入 |
| C++ core 与 compiled 示例隔离 | 资源实验不应阻塞基础 ABI；当前同一 DLL 依不同 manifest ID 选择路径 |
| v1 迁移及 SDK 分发 | 不再加载旧 managed DLL；保留设置后重装；NuGet 为开发依赖、未公开发布 |

### 当前验收表（记录截至 00ebfee）

| 用例 | 历史证据 | 当前 HEAD |
|---|---|---|
| Debug x64 solution | 6 succeeded, 0 failed | NotRun |
| C# Native AOT Release Publish | Get-FileHash 修复后成功，带警告 | NotRun |
| C++ / C# 旧 Debug harness UI | 有显示成功报告 | NotRun |
| C++ compiled 外部 Page | 相对 URI Fail，显式延迟方案有成功报告 | NotRun / 实验 |
| C# command 连续 await | Expected UI STA Fail；已提交修复 | NotRun |
| JIT/AOT 旧 Publish host | 启动 FailFast | NotRun（标准模板） |
| 四种 Release ABI 组合 | 不具备完整实测 | NotRun |
| C# AOT Binding / TwoWay / ICommand | 新示例 | NotRun |
| 正式主程序 MSIX / unpackaged、更新/回滚、退出、故障注入 | 源码接入 | NotRun |
| 独立模板、NuGet 分发、包负例、引用计数与性能基线 | 源码/脚本 | NotRun |

后续记录应包含 commit、Windows build、VS/MSVC/SDK 版本、配置、宿主 publish 路径、包哈希、完整首个错误、最小复现、修复提交、验证命令和结果。先将错误定位到构建、原生映射、ABI、XAML 资源、类型投影或线程中的具体阶段，再决定修复。不要把所有问题归咎于 AOT，也不要因为 Debug 成功而跳过生产验证。
