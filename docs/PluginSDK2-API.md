# SDK 2.0 API 参考

本参考以 HutaoPlugin.idl、C# PluginContext.cs / PluginSettings.cs / HutaoPlugin.cs 和正式宿主 PluginHostContext.cs 为准。SDK 产品版本 2.0 与 ABI major 1 / minor 0 是不同的概念；ABI 是 DLL 与宿主二进制接口，不能因 C# 或 C++ 项目重构而改变既有 IID 和 vtable 方法顺序。

先阅读 [架构与开发模型](PluginSDK2-Architecture.md) 了解 ABI、NuGet、宿主和插件的关系；[开发技术记录](PluginSDK2-EngineeringNotes.md) 解释线程、封送、XAML 与构建方面的历史问题。

## 1. 生命周期与线程

| 回调 | 何时执行 | 可做什么 |
|---|---|---|
| OnLoad(context) | 每次新实例，导出成功后 | 保存 context，注册默认设置、读取设置、日志、注册临时贡献；通知缓存到启用 |
| OnInstall() | 首次成功 OnLoad 且该包哈希没有安装完成标记 | 初始化私有数据，幂等迁移；不注册贡献 |
| OnEnable() | OnLoad/可选 OnInstall 成功后 | 注册视图、导航、命令；宿主校验 viewId、提交后可见 |
| OnDisable() | 禁用、正常退出、启用部分失败 | 撤销 token、解绑事件、取消工作、释放对象；不新建视图或对话框 |
| OnUninstall() | 卸载当前 active 实例，在 OnDisable 后 | 业务清理；保留用户数据是宿主默认策略；禁用状态卸载不重新执行 DLL |

生命周期不是 Windows DLLMain。所有回调由宿主在主 UI STA（Single-Threaded Apartment）调用。OnInstall 成功后即记录 marker；如果进程崩溃，不能保证第三方文件/网络副作用 exactly-once，安装操作必须幂等。回调失败不跨原生边界抛出 C++/CLR 异常；C++/WinRT 与 CsWinRT 把失败转换为 HRESULT，宿主记录并清理。

View 生命周期：RegisterView 仅注册工厂；导航时调用 CreateView(viewId)，读取 Element，挂入宿主的 ContentControl；离开时先调用 OnClosing，再解除视觉树；同一插件会话同时最多一个宿主承载视图。不要在 OnLoad 直接访问尚不存在的 XamlRoot，也不要把 UIElement 同时放入多个父控件。

## 2. C# facade

`HutaoPlugin` 是插件作者继承的抽象类，`Context` 在 OnLoad 开始后可用。它不跨 ABI 传递。`PluginExport.Create<T>(uint, nint*)` 负责初始化插件侧 ComWrappers、实例化 T、导出 IHutaoPlugin；T 必须具有公共无参构造函数。一个 DLL 必须导出 HutaoPluginCreate，返回成功时 result 是拥有一个引用的 IInspectable；宿主负责释放。

### PluginContext

| 成员 | 返回值 / 参数 | 约束与用途 |
|---|---|---|
| PluginId, PluginName, PluginVersion | string | manifest 元数据 |
| PackageDirectory | string | 已校验解压工作区；视为只读；不是持久化目录 |
| DataDirectory | string | 插件私有持久化数据目录 |
| ApiMinor | uint | 当前宿主 ABI minor=0 |
| Settings | PluginSettings | 需要 settings capability |
| HasThreadAccess | bool | UI 回调内可用；后台使用事先 CaptureDispatcher 得到的对象 |
| Log(kind,message) | void | UI STA 日志；无需 notifications 权限 |
| Notify(kind,message) | void | notifications；激活时暂存，active 时宿主显示通知 |
| LoadXaml(relativePath) | UIElement | xaml + loose；必须是 manifest 声明的 .hxaml 文件 |
| ParseXaml(markup) | UIElement | xaml + loose；宿主 XamlReader.Load 解析，最多 1 MiB |
| ShowMessageAsync(title,message,primaryText="OK",secondaryText="Cancel") | Task<DialogResult> | dialogs；active；宿主已有 XamlRoot，返回用户选择 |
| RegisterView(id,factory) | IDisposable | navigation；注册 Func<PluginView>，viewId 局部唯一 |
| RegisterNavigationItem(id,title,iconGlyph,viewId) | IDisposable | navigation；注册项引用已注册 viewId；iconGlyph 一个 Unicode scalar 或空 |
| RegisterCommand(id,title,execute) | IDisposable | commands；Func<Task>，不应启动未被等待的长期工作 |
| CaptureDispatcher() | PluginDispatcher | 只在 UI 线程调用，获得可带到 worker 的调度器 |
| InvokeAsync(Action) | Task | 从当前 UI 调用，进入 dispatcher 回调；后台必须使用已经捕获的 PluginDispatcher |
| RunOnUiAsync(Func<Task>) | Task | 从插件 UI 事件调用；安装插件自己的同步上下文，让后续 await 回到 UI；不是 worker → UI 切换 API |
| Dispose() | void | SDK 内部也负责清理注册；作者不要提前 Dispose 宿主 context |

NotificationKind：Information=0、Success=1、Warning=2、Error=3。DialogResult：None=0、Primary=1、Secondary=2。取消、关闭对话框不等于 Primary。单个插件同时只能有一个 SDK 对话框。

### 设置

仅支持 int（Int32）、bool（Boolean）、string（String）。在 OnLoad 注册默认值：

```csharp
private PluginSetting<int> counter = null!;
private PluginSetting<string> message = null!;

public override void OnLoad(PluginContext context)
{
    counter = context.Settings.Register("Counter", 0, "Number of clicks");
    message = context.Settings.Register("Message", "Default Message", "Saved message");
}
```

PluginSetting<T>：Name 为名称；Value 同步读取缓存；SetValueAsync(T) 等待写盘、更新缓存与通知；ResetAsync() 恢复注册默认值；Subscribe(Action) 订阅持久化后的变更并返回 IDisposable。写入只允许 active，不能在 OnInstall/OnLoad 随意写入；需要私有安装迁移数据时使用 DataDirectory。设置名在插件内大小写不敏感，重复注册失败。注册上限 128 项，观察者上限 32 个。

```csharp
IDisposable subscription = counter.Subscribe(() => countText.Text = counter.Value.ToString());
await counter.SetValueAsync(counter.Value + 1);
await counter.ResetAsync();
// 页面关闭时：
subscription.Dispose();
```

持久化位置是 Plugins/<id>/<name>.json，每个文件包含原始 JSON 基本类型，不是字符串化的 JSON 对象。旧同名、同类型设置会被读取。类型不符使用默认值并记日志。Reset 写空文件作为兼容旧版的清除标记。写入在宿主后台执行，成功前 Value 不变。不要从插件直接修改设置文件绕过缓存、观察者和串行化。

### 后台工作和 UI 线程

两份独立的 Native AOT DLL / 宿主拥有各自 .NET 运行时；Windows DispatcherQueue 是原生对象，可共享，但 SynchronizationContext.Current 属于各自托管运行时。SDK 在从 ABI 回调进入 C# 时建立插件侧上下文，并为每个 Post 回调重新设置。不要在需要继续访问宿主服务的路径使用 ConfigureAwait(false)。

```csharp
private async Task ExecuteAsync()
{
    PluginDispatcher dispatcher = Context.CaptureDispatcher();
    string dataDir = Context.DataDirectory; // UI 获取普通字符串
    await Task.Run(async () =>
    {
        // worker：仅处理普通数据、IO；不要访问 Context、控件、Settings。
        string text = await File.ReadAllTextAsync(Path.Combine(dataDir, "message.txt"));
        await dispatcher.InvokeAsync(() => Context.Log(NotificationKind.Information, text));
    });
    // 这里仍应是 UI STA（SDK 同步上下文负责续体）。
    await Context.ShowMessageAsync("Plugin", "Completed");
}
```

Dispatcher.InvokeAsync(Action) 保证 callback 在 UI 执行，不保证调用它的任意 worker 之后的代码变成 UI。后台线程使用 captured dispatcher.HasThreadAccess，不能从 worker 访问 Context.Dispatcher 属性获取服务。所有 SDK 服务默认 UI STA；错误线程返回 RPC_E_WRONG_THREAD 0x8001010E。不要通过删除线程检查、锁住 UI、同步 Wait/Result 或获取内部指针规避。

WinUI 自身的 Click、Command 等事件未必经 SDK ABI adapter，异步事件应使用 Context.RunOnUiAsync 并在其中 catch 异常。页面关闭时解除 handler、撤销观察者、取消自有工作。SDK 不能强行取消任意第三方 Task；作者要让工作响应关闭，并在完成时检查页面是否已经关闭。

## 3. UI 的三种正式模式和一项实验

| ui.mode | 允许的 UI | 必须注意 |
|---|---|---|
| none | 不注册导航或视图 | 可注册命令、设置等 |
| programmatic | new Button / StackPanel 等内置控件 | 无 xaml capability |
| loose | 程序式控件、LoadXaml/ParseXaml | xaml capability；松散 XAML 不含 x:Class 或编译事件 |
| compiled | 独立 WinUI runtime component + XBF | 仅 PrototypeHost 实验；正式宿主明确拒绝 |

### 松散 XAML（.hxaml）

```xml
<StackPanel xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
            xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <Button x:Name="RunButton" Content="Run"/>
</StackPanel>
```

放在 staging/views/Main.hxaml，使用 Context.LoadXaml("views/Main.hxaml")。XamlReader.Load 返回原生控件，经 WinRT ABI 跨运行时投影为 UIElement。C# Native AOT 可能裁剪掉动态目标类型的投影，必须在发生转换的方法上标注目标类型：

```csharp
[WinRT.DynamicWindowsRuntimeCast(typeof(FrameworkElement))]
[WinRT.DynamicWindowsRuntimeCast(typeof(Button))]
private PluginView CreateView()
{
    FrameworkElement root = (FrameworkElement)Context.LoadXaml("views/Main.hxaml");
    Button button = (Button)root.FindName("RunButton");
    void Click(object sender, RoutedEventArgs e) => Context.Notify(NotificationKind.Success, "Run");
    button.Click += Click;
    return new(root, () => button.Click -= Click);
}
```

DynamicWindowsRuntimeCast 是 CsWinRT 为裁剪保留运行时类转换元数据的属性，不是普通 C# cast 的替代。不要压制 CsWinRT1034。x:Name/FindName 工作于相同名称作用域；不能在 loose XAML 中依赖 XAML 编译器生成的 Click="Handler" 或 x:Bind。

### C# AOT Binding / MVVM

Binding 通过 ICustomPropertyProvider 读取 C# 属性，不同于 x:Bind 的编译代码。C# AOT 自定义 ViewModel 必须 partial 并标注 [WinRT.GeneratedBindableCustomProperty]；实现 INotifyPropertyChanged 用于属性通知。ICommand 实现也应 partial，使 CsWinRT 生成相应 ABI 包装器。不要依赖 reflection 发现属性。仓库 TestPlugin.Aot/SampleViewModel.cs 与 BindingPage.hxaml 给出完整示例（Counter OneWay、Message TwoWay UpdateSourceTrigger=PropertyChanged、Increment/Save 命令、清理）。TwoWay 编辑只改变草稿，Save 显式持久化。

```csharp
[WinRT.GeneratedBindableCustomProperty]
public sealed partial class Model : System.ComponentModel.INotifyPropertyChanged
{
    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
    private string message = "";
    public string Message
    {
        get => message;
        set
        {
            if (message == value) return;
            message = value;
            PropertyChanged?.Invoke(this, new(nameof(Message)));
        }
    }
}
```

`root.DataContext = model` 时，原生 WinUI 使用插件侧 CCW（COM Callable Wrapper）访问 Model，宿主不需要知道 Model CLR 类型。此实现需要跨独立 AOT 运行时的真实验证，当前不能承诺所有 Binding、转换器、模板都支持。事件方式是独立基线，不是 Binding 通过的证据。

### 编译期 XAML 实验

C++ runtime component 的 Page 有 IDL、x:Class、x:Bind、XAML 编译器生成的 Connect/GetBindingConnector、XBF（二进制 XAML）与可能的 PRI（资源索引）。C++/WinRT make/make_self 会调用可访问的 InitializeComponent。默认 ms-appx:///Component/Page.xaml URI 在包外插件无法直接定位。仓库 PluginPage.xaml.cpp 使用显式 InitializeComponent 只延迟加载，随后 InitializePluginView 根据已校验 PackageDirectory 构建绝对 ms-appx URI。PrototypeHost 通过直接调用组件 DllGetActivationFactory 创建 IXamlMetadataProvider 并接入标准 App 的生成 provider。该路径曾在旧 Debug harness 成功，但新模板、AOT、主题、嵌套控件、PRI 和多插件冲突仍需验证。不使用 Detours、私有 XAML API、偏移或写入宿主安装目录。正式主程序当前拒绝 compiled 包；C# compiled runtime component 未提供。

## 4. C++/WinRT ABI 参考

MIDL 的 14 个接口及 IID 在 HutaoPlugin.idl 中。所有公共参数使用 WinRT String/Guid/Boolean/Int32/UInt32/IInspectable、UIElement 或 WinRT 异步类型。不要把 std::string、C++ 指针、.NET Task、GCHandle、托管对象地址直接跨 ABI 传递。

| 接口 | 方法/属性 | 作用 |
|---|---|---|
| IHutaoPlugin | OnLoad/OnInstall/OnEnable/OnDisable/OnUninstall | 入口实例生命周期 |
| IPluginContext | PluginId/Name/Version、PackageDirectory/DataDirectory、AbiMajor/ApiMinor、Settings/Notifications/Dispatcher/Dialogs/Contributions/Ui、Log、GetService(Guid) | 宿主提供的范围受限上下文 |
| IPluginSettings | RegisterInt32/Boolean/String、GetInt32/Boolean/String、SetInt32/Boolean/StringAsync、ResetAsync、Subscribe | 设置；Set/Reset 返回 IAsyncAction |
| IPluginNotifications | Show(kind,message) | 通知 |
| IPluginDialogs | ShowMessageAsync(title,message,primary,secondary) | IAsyncOperation<DialogResult> |
| IPluginDispatcher | HasThreadAccess、InvokeAsync(IPluginAction) | worker → UI 的唯一 ABI 入口 |
| IPluginAction | Invoke() | 调度同步回调 |
| IPluginContributions | RegisterCommand、RegisterNavigationItem、RegisterView | 返回 IPluginRegistration |
| IPluginRegistration | Revoke() | UI STA 幂等撤销 |
| IPluginViewFactory | CreateView(viewId) | 返回 IPluginView |
| IPluginView | Element、OnClosing | 真实 UIElement 与关闭清理 |
| IPluginCommand | ExecuteAsync(commandId) | 返回 IAsyncAction |
| IPluginSettingObserver | OnChanged(name) | 已持久化设置观察者 |
| IPluginUi | LoadXaml(relativePath)、ParseXaml(markup) | 松散 XAML |

GetService 只识别上述允许服务的 IID，未知 Guid 返回 null。获取服务不代表可以跳过 capability 与 phase 验证。禁止查询主程序 IServiceProvider、Window、ViewModel、IMessenger、数据库或游戏注入接口。WinRT QI（QueryInterface）不是安全边界；宿主在调用时再次验证权限。

```cpp
auto settings = context.Settings();
settings.RegisterInt32(L"Counter", 0, L"Clicks");
// active：
co_await settings.SetInt32Async(L"Counter", settings.GetInt32(L"Counter") + 1);
auto dispatcher = context.Dispatcher();
winrt::apartment_context ui;
co_await winrt::resume_background();
// action 是实现 IPluginAction 的 agile WinRT 对象
co_await dispatcher.InvokeAsync(action);
co_await ui;
auto result = co_await context.Dialogs().ShowMessageAsync(L"Plugin", L"Done", L"OK", L"Cancel");
```

完整 coroutine、IPluginAction、注册 token 与 get_strong 的例子见 TestPlugin.Native/TestPlugin.cpp。协程跨挂起前持有自身生命周期，worker 不使用 Context；回调及 Dispatcher 必须支持 IAgileObject（可跨 COM apartment 的对象）或已验证的封送方案。不要在 UI 上阻塞等待 co_await。

## 5. 错误处理与约束

- 14 个固定 IID：以 IDL 为唯一来源；新增接口需新 IID，不能重排原接口。
- HRESULT 错误：入口参数错误、ABI mismatch、权限、线程、资源等由生成投影转换为语言异常。C++ 使用 hresult_error / check_hresult，C# 使用异常与 Task；在插件边界和事件中保留原始消息。
- 贡献：每个插件 16 个视图、16 导航、64 命令；局部 ID 最大 64 字符（ASCII，字母/数字起始，允许 ._-），大小写不敏感。标题 1–128 字符；导航 glyph 单个 Unicode scalar 或空。
- 模块驻留：同一进程同 ID 只允许同一版本、路径、哈希；禁用后撤销贡献而非卸载 DLL。故障模块隔离到下次进程启动，不声称 native code 安全沙箱。
- 权威源：源码 HutaoPlugin.idl、HutaoPlugin.hpp、SDK C# facade；本文不新增未实现的宿主服务。
