# 插件 XAML 开发指南：loose XAML、Binding、XBF 与 PRI

## 1. 先选择正确的 UI 方式

| 目标 | 选择 | 编译产物 |
|---|---|---|
| 简单 UI、排除资源问题 | programmatic | native DLL |
| 与 v1 .hxaml 页面等价 | loose | native DLL + .xaml |
| C# MVVM | loose + 生成 Binding 元数据 | native DLL + .xaml；AOT 验收待完成 |
| C++ x:Class、x:Bind、生成事件、独立 runtime class | compiled 实验 | native DLL + XBF + 对应元数据/资源 |
| C# 编译期 XAML runtime component | 尚未实现验收 | 不承诺支持 |

正式宿主接收 none、programmatic 和 loose；compiled 当前只能在 PrototypeHost 实验，不能将其写成已发布的能力。**v1 的示例本来就不是编译期 XAML。** v1 的 TestPlugin.csproj 将 TestPluginPage.hxaml 嵌入托管程序集；PluginScopedPage.LoadXamlManually 读取字符串并调用 XamlReader.Load。v2 保留这条基本路径，只将资源改为验证过的外部包文件，不需要 XBF 或插件 PRI 来实现它。

## 2. 创建一个 loose XAML 页面（完整代码）

现在松散与编译期 XAML 都使用标准 .xaml 扩展名。决定是否编译的是项目项类型（Build Action），不是文件后缀。C# 模板已关闭默认 Page 项；若添加松散 XAML，确保其 Build Action 为 None。C++ Core 项目将 CoreView.xaml 标为 None，只有 CppCompiled 的 PluginPage.xaml 是 Page。松散 XAML 不生成 XBF、代码连接或自己的 PRI；编译期 XAML 则由 XAML compiler 处理。旧 .hxaml 仅作为兼容输入。

```xml
<!-- C#：不让 WinUI 把文本资源当 Page 编译 -->
<PropertyGroup>
  <EnableDefaultPageItems>false</EnableDefaultPageItems>
</PropertyGroup>
<ItemGroup>
  <Page Remove="views\Main.xaml" />
  <None Update="views\Main.xaml" />
</ItemGroup>
```

```xml
<!-- C++：两种独立项目分别声明 -->
<None Include="CoreView.xaml" />
<!-- CppCompiled 项目中才有： -->
<Page Include="PluginPage.xaml" />
```

两种资源仍应在 pack 前复制到 staging；loose 复制 .xaml 源文件，compiled 复制编译产物 .xbf。不要将同一文件同时作为 None 和 Page。

在 [C# 模板](../src/Snap.Hutao.Plugin.SDK/templates/CSharpPlugin) 基础上创建 views/Main.xaml：

```xml
<StackPanel xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
            xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
            Spacing="8">
    <TextBlock x:Name="CountText" Text="0" />
    <Button x:Name="IncrementButton" Content="Increment" />
</StackPanel>
```

保留模板的 Exports，使用以下插件实现替换 MyPlugin 类。所需 using：

```csharp
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Snap.Hutao.Plugin.Contracts;
using Snap.Hutao.Plugin.SDK;
```

```csharp
public sealed class MyPlugin : HutaoPlugin
{
    private PluginSetting<int> counter = null!;

    public override void OnLoad(PluginContext context)
    {
        counter = context.Settings.Register("Counter", 0, "Click count");
    }

    public override void OnEnable()
    {
        Context.RegisterView("main", CreateView);
        Context.RegisterNavigationItem("main-nav", "My Counter", "\uE74C", "main");
    }

    [WinRT.DynamicWindowsRuntimeCast(typeof(FrameworkElement))]
    [WinRT.DynamicWindowsRuntimeCast(typeof(TextBlock))]
    [WinRT.DynamicWindowsRuntimeCast(typeof(Button))]
    private PluginView CreateView()
    {
        FrameworkElement root = (FrameworkElement)Context.LoadXaml("views/Main.xaml");
        TextBlock count = (TextBlock)root.FindName("CountText");
        Button button = (Button)root.FindName("IncrementButton");
        bool closed = false;
        bool busy = false;

        void Refresh() => count.Text = counter.Value.ToString();
        Refresh();
        IDisposable subscription = counter.Subscribe(Refresh);

        async void Increment(object sender, RoutedEventArgs args)
        {
            try
            {
                await Context.RunOnUiAsync(async () =>
                {
                    if (closed || busy) return;
                    busy = true;
                    try { await counter.SetValueAsync(counter.Value + 1); }
                    finally { busy = false; }
                });
            }
            catch (Exception error) { System.Diagnostics.Debug.WriteLine(error); }
        }

        button.Click += Increment;
        return new(root, () =>
        {
            closed = true;
            button.Click -= Increment;
            subscription.Dispose();
        });
    }
}
```

manifest 修改为：

```json
"ui": { "mode": "loose" },
"capabilities": ["navigation", "settings", "xaml"]
```

在模板 Build-Plugin.ps1 复制入口 DLL 后、执行 pack 前，把资源复制到 staging（或直接参照下列命令；不要在 pack 后追加文件）：

```powershell
New-Item -ItemType Directory -Force (Join-Path $stage 'views') | Out-Null
Copy-Item -LiteralPath (Join-Path $root 'views\Main.xaml') -Destination (Join-Path $stage 'views\Main.xaml')
```

最终包内文件：manifest.json、bin/x64/MyPlugin.dll、views/Main.xaml。文件名必须与 LoadXaml 参数一致。每次打开视图创建新的控件树；关闭时撤销事件和设置订阅。

LoadXaml 的宿主实现：检查 xaml 能力、active 状态和 loose 模式 → 按 manifest 相对路径解析至 PackageDirectory → 读取文本 → XamlReader.Load → 返回 UIElement。它并不调用插件程序集的 GetManifestResourceStream，也不会执行 XAML 编译器生成的代码。不要写 x:Class、x:Bind 或 Click="Increment"；事件由 C# 或 C++ 显式连接。DynamicWindowsRuntimeCast 使 AOT 保留 WinRT runtime class 的转换支持，不是绑定属性的生成器。

## 3. 使用真正的 Binding

需要把控件和业务状态分开时，使用插件自己的 ViewModel。完整参考：

- [SampleViewModel.cs](../src/Snap.Hutao.Plugin.SDK/samples/CSharpAot/SampleViewModel.cs)
- [BindingPage.xaml](../src/Snap.Hutao.Plugin.SDK/samples/CSharpAot/BindingPage.xaml)
- [TestPlugin.cs](../src/Snap.Hutao.Plugin.SDK/samples/CSharpAot/TestPlugin.cs) 的 CreateBindingView()

XAML：

```xml
<TextBlock Text="{Binding Counter, Mode=OneWay}" />
<TextBox Text="{Binding Message, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" />
<Button Content="Save message" Command="{Binding SaveCommand}" />
```

模型用 [WinRT.GeneratedBindableCustomProperty] + partial 生成 WinRT 可访问属性，INotifyPropertyChanged 通知变更，ICommand 处理按钮动作；设 root.DataContext=model，OnClosing 清空 DataContext 并 Dispose。与普通 v1 JIT Binding 不同，Native AOT 不应依赖运行时反射发现任意 CLR 属性。设置观察者在持久化完成后触发 PropertyChanged，TwoWay 的草稿在 SaveCommand 中写入设置。此示例仍需要真实 AOT host × AOT plugin 验收。

## 4. XAML 中的相对路径为什么会失败？

必须区分三种路径：

| 路径 | 由谁解析 |
|---|---|
| Context.LoadXaml("views/Main.xaml") | SDK 宿主，按该插件工作区的 manifest 路径 |
| ms-appx:///Assets/Logo.png | WinUI 包/应用资源解析器，默认是宿主资源 |
| ms-resource:///Resources/Title 或 x:Uid | MRT Core（资源管理器），依赖资源映射/PRI |

插件 DLL 被 LoadLibrary 映射到宿主进程，不意味着 WinUI 的应用资源根切换到了该 DLL 旁边。XamlReader.Load(string) 也不会自动根据 .xaml 文件在磁盘的位置，为其中的相对 Source URI 建立插件私有根。不要用宿主安装目录当作插件工作区。

对于普通图片，显式读取插件目录中的已随包分发文件。例如 C# 在 UI 线程创建位图：

```csharp
using Windows.Storage;
using Microsoft.UI.Xaml.Media.Imaging;

string path = Path.Combine(Context.PackageDirectory, "assets", "logo.png");
StorageFile file = await StorageFile.GetFileFromPathAsync(path);
using var stream = await file.OpenAsync(FileAccessMode.Read);
BitmapImage bitmap = new();
await bitmap.SetSourceAsync(stream);
// 赋给你的 Image.Source
image.Source = bitmap;
```

文件必须出现在 staging/assets/logo.png，打包后不能修改。该示例是插件直接读取自己的已打包资源，不是新增 SDK ABI。动态字符串路径应限定在插件资源根，避免路径穿越；用户数据写到 DataDirectory。对 XAML 中 ResourceDictionary.Source、x:Uid、嵌套自定义控件，不要据此推断自动资源解析已经支持。

## 5. 编译期 XAML：当前 C++ 实验到底做了什么？

参考真实源码 [TestPlugin.Native](../src/Snap.Hutao.Plugin.SDK/samples/CppCompiled)。该项目是 WinUI Runtime Component（不是独立 Application），包含 PluginPage.idl、PluginPage.xaml、PluginPage.xaml.h/.cpp。IDL 定义 runtime class，XAML 的 x:Class 连接生成代码：

```xml
<Page x:Class="TestPlugin.Native.PluginPage"
      xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <StackPanel>
        <TextBlock Text="{x:Bind TitleText, Mode=OneTime}" />
        <Button x:Name="ClickButton" Content="Click" Click="OnClick" />
        <TextBlock x:Name="StatusText" />
    </StackPanel>
</Page>
```

XAML 编译器生成 .g.h/.g.cpp、Connect/GetBindingConnector 等连接代码和 PluginPage.xbf。默认 InitializeComponent 中的逻辑 URI 类似 ms-appx:///TestPlugin.Native/PluginPage.xaml；宿主资源查找失败时曾报告 0x80004005 Cannot locate resource，最终 XamlParseException。单纯把 UserControl 改成 Page 并不能解决。

我们实际验证过的旧 Debug 实验修复：

1. PluginPage 在 implementation 类内声明空的 InitializeComponent，防止 make_self 自动调用生成的宿主相对 loader。此处不要设置 _contentLoaded。
2. make_self 返回后将已验证的 IPluginContext 传入 InitializePluginView。
3. 使用 PluginXaml.hpp 的 compiled_xaml_uri，把实际 PackageDirectory 与 logical .xaml 路径组合，保留盘符并逐段转义。
4. 调用 Application::LoadComponent，保留生成的 Connect/GetBindingConnector，而不是手写所有控件连接。

关键代码（对应仓库 PluginPage.xaml.cpp）：

```cpp
#include "PluginXaml.hpp"

void PluginPage::InitializeComponent()
{
    // 此处刻意延迟，尚无插件 context。
}

void PluginPage::InitializePluginView(IPluginContext const& context)
{
    if (_contentLoaded) throw winrt::hresult_illegal_method_call();
    m_context = context;
    auto uri = hutao::plugin::compiled_xaml_uri(context.PackageDirectory(), L"views/PluginPage.xaml");
    _contentLoaded = true;
    Application::LoadComponent(*this, uri, ComponentResourceLocation::Application);
}
```

物理文件是 views/PluginPage.xbf，传给 WinUI 的逻辑 URI 仍然是 views/PluginPage.xaml。不要把两者反过来。该 URI 形式是当前 WinUI 实验路径，不是微软对任意外部 URI、UNC 路径和所有包环境的稳定承诺。SDK helper 拒绝 ..、空路径段、冒号和反斜杠，按 local drive 生成 URI；资源必须由包验证与 staging 保证存在。

Pack-Samples.ps1 查找唯一 PluginPage.xbf 并复制到 views/PluginPage.xbf；同时保留输出目录存在的 TestPlugin.Native.winmd、TestPlugin.Native.pri、resources.pri。manifest.ui.mode=compiled、metadataProvider=TestPlugin.Native.XamlMetaDataProvider。PrototypeHost 在调用插件生命周期前，从 DLL 的 DllGetActivationFactory 激活 metadata provider，使用 App 生成的 AddOtherProvider 注册。metadata 让 XAML 引擎认识插件类型，**不是**资源根重定向。`AddOtherProvider` 是条件生成方法：PrototypeHost.csproj 必须配置 `EnableTypeInfoReflection=false` 与 `XamlCodeGenerationControlFlags=DoNotGenerateOtherProviders`，并在 `App.InitializeComponent()` 前手动注册 WinUI 控件 provider；见 [E16](PluginSDK2-EngineeringNotes.md#e16标准-prototypehost-编译-cs0103addotherprovider-不存在)。C++ 示例代码支持 x:Bind 和生成事件连接的实验；C# 编译期 XAML 不可用，不能把普通 C# WinUI class library 的 Publish 当作已支持功能。

## 6. PRI 到底在哪里，如何验证？

XBF 是二进制 XAML 内容；PRI 是 MRT 的资源映射索引，可能包含嵌入的 XBF 数据，也可能引用外部文件，并负责语言、对比度、缩放等 qualifier（资源限定符）选择。一个组件拥有 PRI 并不意味着它已经被宿主的默认 ResourceManager 发现。

目前有两个独立事项：

| 对象 | 当前实现 |
|---|---|
| PrototypeHost 自身 resources.pri | 标准 App.xaml，EnableMsixTooling，GenerateProjectPriFile 和 Publish 复制/存在性校验；新发布实际启动待验收 |
| C++ 插件自己的 PRI | Pack-Samples 保存可用 PRI 供检查；没有注册到宿主 WinUI 的全局 resource map |
| 插件 Page XBF | 通过外部绝对逻辑 URI 加载物理 XBF；旧 Debug 成功，新代码/Release/AOT 待验证 |
| 插件 x:Uid、嵌套 ResourceDictionary.Source、跨组件/语言/缩放资源 | 未证明支持，不应宣传为已修复 |

检查 PRI 内容，不要只看文件名。在 Developer PowerShell 或明确使用 Windows SDK 工具：

```powershell
$pri = Get-ChildItem .\artifacts\native\Release\compiled -Filter *.pri -Recurse -File
foreach ($file in $pri) {
    $dump = $file.FullName + '.xml'
    makepri dump /if $file.FullName /of $dump /o
    if ($LASTEXITCODE -ne 0) { throw "makepri dump failed: $($file.FullName)" }
}
```

查看 ResourceMap 的 name、Files 子树、PluginPage.xbf 的候选类型和文件路径、Resources 子树及 qualifiers。若编译器将 XBF 嵌入 PRI，外部物理 XBF 方案需要对应构建输出，不能假设每种 DisableEmbeddedXbf 设置都产生相同文件。仓库打包脚本在找不到唯一物理 XBF 时直接失败。

对插件自己的 PRI，可在代码中直接创建独立的 MRT Core ResourceManager，按 dump 得到的资源 key 读取。以下为 C# API 示例（假设你已将该 PRI 作为已声明包资源，确切 key 以实际 PRI 为准）：

```csharp
using Microsoft.Windows.ApplicationModel.Resources;

string priPath = Path.Combine(Context.PackageDirectory, "assets", "Plugin.pri");
ResourceManager manager = new(priPath);
string title = manager.MainResourceMap.GetValue("Resources/Title").ValueAsString;
```

这能让插件明确访问自己的资源映射，但不会让 WinUI 自动解析该 PRI 中的 x:Uid 或 .xbf。微软提供的 [Application.ResourceManagerRequested](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.application.resourcemanagerrequested) 能在 WinUI 线程初始化时提供自定义 IResourceManager；需要在宿主初始化阶段设计并验证按组件路由、保留默认资源、qualifier、名称冲突及生命周期。参考 [WinUI 设计说明](https://github.com/microsoft/microsoft-ui-xaml/blob/main/docs/design-notes/custom-mrt-resourcemanager.md) 和 [外部 UserControl 问题 #6299](https://github.com/microsoft/microsoft-ui-xaml/issues/6299)。这属于新的全局资源解析设计，不是拷贝一个 PRI 或改 URI 就自动解决，也不应让插件替换整个宿主资源管理器。本轮不将其引入生产宿主，避免破坏宿主正常 XAML 初始化。

## 7. 测试顺序

先通过 programmatic → loose + event → loose + Binding，再单独运行 compiled 实验。确认实际包文件、页面创建、Click、x:Bind、关闭、重新打开；再测试主题、嵌套控件、两种宿主发布和多插件。当前没有新 Windows 执行证据，所有新资源与 Binding 组合仍需按 [Testing](PluginSDK2-Testing.md) 验收。
