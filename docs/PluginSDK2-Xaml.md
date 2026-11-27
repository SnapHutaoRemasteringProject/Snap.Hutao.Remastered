# 插件 XAML 开发指南：loose XAML、Binding、XBF 与 PRI

## 1. 选择页面构建方式

| 目标 | 选择 | 打包内容 |
|---|---|---|
| 用代码创建控件、排查资源问题 | `programmatic` | 原生 DLL |
| C# 或 C++ 的 `x:Class`、`x:Bind`、XAML 事件 | `compiled` | 原生 DLL + 内嵌 XBF 的同名 PRI |
| 维护已采用运行时解析的 V2 页面 | `loose` | 原生 DLL + 包内文本 `.xaml` |

### C# 编译型页面

从 SDK 的 C# 项目模板开始，先打开默认页面并验证点击通知，再添加自己的页面。普通应用与插件使用同一套 WinUI XAML 编译器。插件由胡桃已有的 WinUI 应用实例打开，因此项目输出 DLL，由宿主管理窗口及线程。

#### 1. 把页面加入编译

例如项目的 `RootNamespace` 为 `Example.MyPlugin`。在 `PluginPage.xaml` 声明页面类型和按钮：

```xml
<Page x:Class="Example.MyPlugin.PluginPage"
      xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <StackPanel Spacing="8">
        <TextBlock Text="{x:Bind Title}" />
        <Button x:Name="ClickButton" Content="发送通知" />
    </StackPanel>
</Page>
```

项目文件中的 `<Page Include="PluginPage.xaml" />` 把它交给 XAML 编译器。编译器根据 `x:Class` 生成同名 partial class，提供 `InitializeComponent`、`x:Bind` 访问代码和 `ClickButton` 字段。你在同一个命名空间中补上自己的 partial class：

```csharp
using Microsoft.UI.Xaml.Controls;

namespace Example.MyPlugin;

public sealed partial class PluginPage : Page
{
    public PluginPage() => InitializeComponent();

    public string Title => "My Plugin";

    internal Button ActionButton => ClickButton;
}
```

这里的 `x:Bind Title` 默认读取一次页面自己的属性。修改为运行时会变化的值时，按需要设置 `Mode=OneWay` 并为源提供属性变更通知。传统 `{Binding ...}` 按名字从 `DataContext` 找属性。在 Native AOT 下，自定义绑定源需要 `[WinRT.GeneratedBindableCustomProperty]` 和 `partial`，让 CsWinRT 在编译时生成 XAML 所需的属性查询。两种方式可以在同一页面上使用，仓库的 `samples/CSharpAot/BindingPage.xaml` 展示了后一种。

#### 2. 注册页面并绑定生命周期

模板在 `OnEnable` 里注册视图工厂，然后把导航项指向它。每次打开都调用工厂，构造独立的控件树。下面是 `Plugin.cs` 中的核心代码：

```csharp
private PluginView CreateView()
{
    PluginPage page = new();

    void Click(object sender, RoutedEventArgs args)
    {
        Context.Notify(NotificationKind.Success, "Hello from plugin");
    }

    page.ActionButton.Click += Click;
    return new(page, () => page.ActionButton.Click -= Click);
}
```

`InitializeComponent` 在构造时加载页面资源。宿主关闭页面后会调用 `PluginView` 的清理回调。新增第二页时，再添加一个 `Page` 项并注册不同的视图键。同一个插件的两个页面共享这份 PRI 和元数据提供程序。

#### 3. 核对元数据名称与两阶段编译

模板配置 `XamlProjectName=Plugin`，所以 WinUI 生成的提供程序完整类型名是 `Example.MyPlugin.Plugin_XamlTypeInfo.XamlMetaDataProvider`。这个提供程序实现 `IXamlMetadataProvider`，把 XAML 类型名、属性和激活行为交给宿主。将该完整名称填在 `manifest.template.json` 的 `ui.metadataProvider`，并保持 `ui.mode=compiled`。改命名空间时，同时改页面的 `x:Class`、C# 文件和 manifest。Visual Studio 模板会替换这些位置。

插件有两个原生导出。宿主先用 `HutaoPluginCreate` 取得插件实例；编译型 UI 再使用 `DllGetActivationFactory` 取得上述元数据提供程序。模板的 `XamlExports.cs` 通过 SDK 的 `PluginXamlExport.GetActivationFactory<T>` 转发到 WinUI 生成的类型。项目先从默认编译项中移除此文件，由 SDK 的 `CompiledPluginXaml.targets` 在 `MarkupCompilePass2` 后加入。第一阶段先生成供 XAML 编译器读取的托管元数据，第二阶段补齐最终生成代码。导出跟随第二阶段编译，引用的是最终提供程序。

#### 4. 发布、检查资源、打开页面

在 Developer PowerShell 中使用模板的 `NativeAot` 配置：

```powershell
dotnet publish .\MyPlugin.csproj -c Release -p:Platform=x64 -p:PublishProfile=NativeAot -r win-x64
dumpbin /exports .\publish\MyPlugin.dll
makepri dump /if .\publish\MyPlugin.pri /of .\plugin-pri.xml /dt detailed /o
pwsh -File .\Pack-Plugin.ps1 -SdkDirectory C:\HutaoSdk
```

`DisableEmbeddedXbf=false` 让生成的 XBF 作为二进制数据进入 PRI（包资源索引）。在 `plugin-pri.xml` 中找到 `Files/MyPlugin/PluginPage.xbf` 对应的 `EmbeddedData`。XBF 是编译后的页面数据，包内保留 `bin/x64/MyPlugin.dll` 和 `bin/x64/MyPlugin.pri`。宿主加载 DLL，并把该组件的资源查询路由到 PRI。

用 PrototypeHost 打开 `artifacts/MyPlugin.hutao`，依次检查页面加载、标题和按钮点击。最终在普通版及 Native AOT 版主程序重复安装、启用和打开页面。若加载失败，先检查包中 DLL/PRI 是否同名，再对照清单的完整提供程序类型名与生成的 `XamlTypeInfo.g.cs`，最后检查 PRI 的 XBF 是否确实为内嵌数据。

## 2. 维护使用 loose XAML 的 V2 插件

现在松散与编译期 XAML 都使用标准 .xaml 扩展名。决定是否编译的是项目项类型（Build Action），不是文件后缀。C# 模板已关闭默认 Page 项；若添加松散 XAML，确保其 Build Action 为 None。C++ Core 项目将 CoreView.xaml 标为 None，只有 CppCompiled 的 PluginPage.xaml 是 Page。松散 XAML 不生成 XBF、代码连接或自己的 PRI；编译期 XAML 则由 XAML compiler 处理。SDK 2.0 不兼容旧 .hxaml 文件；迁移时重命名为 .xaml，并同步修改项目项、打包路径和 LoadXaml 参数。

```xml
<!-- C#：不让 WinUI 把文本资源当 Page 编译 -->
<PropertyGroup>
  <EnableDefaultPageItems>false</EnableDefaultPageItems>
</PropertyGroup>
<ItemGroup>
  <Page Remove="resources\views\Main.xaml" />
  <None Update="resources\views\Main.xaml" />
</ItemGroup>
```

```xml
<!-- C++：两种独立项目分别声明 -->
<None Include="CoreView.xaml" />
<!-- CppCompiled 项目中才有： -->
<Page Include="PluginPage.xaml" />
```

两种资源仍应在 pack 前复制到 staging；loose 复制 .xaml 源文件，compiled 复制包含 XBF 的组件 PRI（不复制独立 .xbf）。不要将同一文件同时作为 None 和 Page。

以下示例针对已使用 `ui.mode=loose` 且声明 `xaml` 能力的旧 V2 项目。创建 `resources/views/Main.xaml`：

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
        FrameworkElement root = (FrameworkElement)Context.LoadXaml("resources/views/Main.xaml");
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

推荐直接把 Main.xaml 放在模板项目的 resources/views/Main.xaml，Pack-Plugin.ps1 将整个 resources 目录复制到 staging，保留 resources/views/Main.xaml 路径。不需要编辑脚本。手工 staging 时，等价命令如下（不要在 pack 后追加文件）：

```powershell
New-Item -ItemType Directory -Force (Join-Path $stage 'resources\views') | Out-Null
Copy-Item -LiteralPath (Join-Path $root 'resources\views\Main.xaml') -Destination (Join-Path $stage 'resources\views\Main.xaml')
```

最终包内文件：manifest.json、bin/x64/MyPlugin.dll、resources/views/Main.xaml。文件名必须与 LoadXaml 参数一致。每次打开视图创建新的控件树；关闭时撤销事件和设置订阅。

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
| Context.LoadXaml("resources/views/Main.xaml") | SDK 宿主，按该插件工作区的 manifest 路径 |
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

旧 Debug 实验通过绝对 URI 加载独立 XBF，证明了外部 Page 与 Metadata Provider 的基本链路。新版取消这种部署方式：组件项目明确设置如下属性，仍然使用正常 WinUI 编译流程：

```xml
<EnablePriGenTooling>true</EnablePriGenTooling>
<AppxGeneratePriEnabled>true</AppxGeneratePriEnabled>
<DisableEmbeddedXbf>false</DisableEmbeddedXbf>
```

插件开发者不需要拼接插件工作区绝对 URI。C++ 示例保留延迟初始化，仅用于先设置插件 context，然后调用编译器原生生成的方法：

```cpp
void PluginPage::InitializeComponent()
{
    // make_self 的初始调用延迟，等待插件 context。
}

void PluginPage::InitializePluginView(IPluginContext const& context)
{
    if (_contentLoaded) throw winrt::hresult_illegal_method_call();
    m_context = context;
    PluginPageT<PluginPage>::InitializeComponent();
}
```

生成的方法继续使用原来的 ms-appx:///TestPlugin.Native/PluginPage.xaml，以及原来的 Connect、GetBindingConnector 与 x:Bind 代码。无需手工编写 LoadComponent URI；旧 PluginXaml.hpp 已删除。

### 宿主怎样识别 PRI？

原生 DLL、Metadata Provider 和 PRI 各自解决不同问题。PrototypeHost 在 App 构造函数中、InitializeComponent 前订阅 Application.ResourceManagerRequested，把一个普通 MRT Core ResourceManager 交给 WinUI：

```csharp
ResourceManagerRequested += (_, args) =>
{
    pluginResources ??= new PluginPriResources();
    args.CustomResourceManager = pluginResources.Manager;
};
```

PluginPriResources 内部订阅真实 ResourceManager 的 ResourceNotFound。普通宿主资源仍走原有 PRI；只有宿主查找失败且名称属于已注册的插件组件时，才查询插件自己的 PRI。注册发生在已批准包加载后、OnLoad 前（与 Metadata Provider 注册一起）。关键逻辑为：

```csharp
ResourceContext context = component.Manager.CreateResourceContext();
// 实际实现将宿主的 qualifier 值复制到该组件的 context。
ResourceCandidate? candidate = component.Manager.MainResourceMap.TryGetValue(key, context);
if (candidate?.Kind == ResourceCandidateKind.EmbeddedData)
    args.SetResolvedCandidate(candidate);
```

完整代码见 [PluginPriResources.cs](../src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Service/Plugin/PluginPriResources.cs)。MRT Context 属于创建它的 Manager，因此不能把宿主 context 原样传给另一个 PRI；需复制语言、缩放、主题等限定值。组件路由使用 DLL 文件名（不含 .dll）作为唯一资源前缀，示例 TestPlugin.Native.dll 对应 Files/TestPlugin.Native/...；组件自己的 PRI 可以使用此前缀或相对组件根路径。编译器生成的 URI 前缀须与 DLL 名一致，不能与宿主或其他组件重复。默认宿主查找优先，不覆盖已有资源。不处理 FilePath 候选、不写出临时 XBF、不修改宿主 PRI。资源映射与已驻留的 DLL 一起保留到进程退出。

Metadata Provider 仍是必须的：manifest.ui.mode=compiled、metadataProvider=TestPlugin.Native.XamlMetaDataProvider；两个宿主通过 DllGetActivationFactory 获取 provider。PrototypeHost 仍用已验证的 App.AddOtherProvider；主程序不能关闭现有控件库自动注册，故 App.PluginXaml.cs 显式实现 IXamlMetadataProvider，优先查询编译器生成的完整宿主 provider，再查询插件 provider。PreConstruct 在源生成构造函数 InitializeComponent 之前订阅 ResourceManagerRequested。两者共享 PluginPriResources，不改变宿主 PRI 文件。C# 编译型项目也通过该导出和相同的宿主 PRI 路由加载。主程序的普通版、AOT 版及 C# 新路径仍待 Windows 验收。

## 6. PRI 与 XBF：部署及验证

XBF 是实际运行时二进制界面数据；PRI 可以存放数据（EmbeddedData）或文件路径（FilePath）。DisableEmbeddedXbf=false 允许把 XBF 数据嵌入 PRI，**不表示构建目录不再产生 .xbf**。独立 .xbf 只可能作为工具链构建中间文件存在，不是 SDK 2.0 compiled 包的部署文件。

示例包结构：

```text
TestPlugin.Native.hutao
  manifest.json
  bin/x64/
    TestPlugin.Native.dll
    TestPlugin.Native.pri
```

PRI 路径必须与 runtime.entry 同名同目录，仅扩展名改为 .pri，并列入 files 哈希清单。Pack-Samples.ps1 从组件 Build 输出读取 PRI，统一命名为 TestPlugin.Native.pri；不存在就失败，不从 obj 搜索 XBF。包校验拒绝缺少对应 PRI 或包含独立 .xbf 的 compiled 包。PRI 存在并不证明已经嵌入数据，最终由 MRT 的 EmbeddedData 候选和实际 Page 初始化验证。

检查方法（仅故障诊断，不是日常开发必做步骤）：在 Developer PowerShell 执行：

```powershell
$pri = Get-ChildItem .\artifacts\native\Debug\compiled -Filter *.pri -File
foreach ($file in $pri) {
    makepri dump /if $file.FullName /of ($file.FullName + '.xml') /o
    if ($LASTEXITCODE -ne 0) { throw "makepri dump failed: $($file.FullName)" }
}
```

在 XML dump 中查找 PluginPage.xbf 的资源名称与候选类型，确认实际为内嵌数据而不是 Path。运行 PrototypeHost 时确认 [PluginPRI] Last embedded resource 显示该插件资源名（不能是 none），且 Generated InitializeComponent returned、x:Bind 文本和 Click 均成功。使用不含独立 XBF 的新包测试；不能沿用旧绝对 URI 的 Pass。

上游依据： [ResourceManagerRequested](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.application.resourcemanagerrequested)、[MRT ResourceMap.cpp](https://github.com/microsoft/WindowsAppSDK/blob/main/dev/MRTCore/mrt/Microsoft.Windows.ApplicationModel.Resources/src/ResourceMap.cpp)、[WinUI ModernResourceProvider.cpp](https://github.com/microsoft/microsoft-ui-xaml/blob/main/dxaml/xcp/components/mrt/ModernResourceProvider.cpp)、[MRTResource.cpp](https://github.com/microsoft/microsoft-ui-xaml/blob/main/dxaml/xcp/components/mrt/MRTResource.cpp)。TryGetValue 失败也会触发 ResourceNotFound，WinUI 能直接消费 EmbeddedData。参考 [WinUI #10308](https://github.com/microsoft/microsoft-ui-xaml/issues/10308) 关于 XBF 重复部署的报告。

2026-11-27 用户已验证 PrototypeHost PRI-only 页面初始化、内嵌资源命中、Click 和异步命令。x:Uid 使用的 TryGetSubtree 路径不触发上述回调，故本实现不承诺自动本地化属性包；普通文件候选、跨组件资源、嵌套字典、多线程、多插件和 JIT/AOT 宿主仍需独立验证。主程序现已接入同一 PRI 路由，JIT/AOT 编译、启动与运行仍待验收。

## 7. 测试顺序

先通过 programmatic → loose + event → loose + Binding，再单独运行 compiled 实验。确认实际包文件、页面创建、Click、x:Bind、关闭、重新打开；再测试主题、嵌套控件、两种宿主发布和多插件。除上述用户实测通过项目外，其余资源与 Binding 组合仍需按 [Testing](PluginSDK2-Testing.md) 验收。
