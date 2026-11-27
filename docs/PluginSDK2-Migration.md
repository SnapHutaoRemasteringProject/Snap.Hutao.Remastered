# 从 Plugin SDK 1.0 迁移到 2.0

如果你有一个旧的 .hutao 插件，**不要只改 manifest 的版本号，也不要把旧的托管 DLL 放进新版包。** 先用新版模板建立项目，再迁移业务代码和资源。新版保留插件的生命周期、设置、通知、导航、视图、命令、游戏 DLL 声明等业务能力；它改变的是宿主和插件之间的调用边界。

旧版基准：[2.0.2 TestPlugin](https://github.com/SnapHutaoRemasteringProject/Snap.Hutao.Remastered/tree/2.0.2/src/Snap.Hutao.Plugin.SDK/TestPlugin)。下文的 v1 代码来自该示例或明确标出的缩减示意；不假设旧代码使用编译期 XAML。

## 1. 哪些功能需要改写？

| v1 行为 | v2 迁移方式 | 当前状态 |
|---|---|---|
| 普通 C# DLL + AssemblyLoadContext | C# Native AOT shared DLL，或 C++/WinRT DLL | 原生入口已实现；旧 managed DLL 不兼容 |
| PluginMain attribute / HutaoPlugin | HutaoPluginCreate 导出 + SDK HutaoPlugin | 已实现 |
| 引用主程序 csproj / IServiceProvider | SDK NuGet + 固定 WinRT 服务 | 已实现公开服务；不暴露任意内部服务 |
| OnLoad/Install/Enable/Disable/Uninstall | 同名生命周期 | 已实现；调用时序更严格 |
| Reflection 找 MainView、插入 NavigationViewItem | RegisterView + RegisterNavigationItem | 主程序正式导航已接入，待 Windows 回归 |
| PluginScopedPage + .hxaml + XamlReader.Load | PluginView + LoadXaml / ParseXaml | 对应行为已实现 |
| Binding、ViewModel、ICommand | 插件自己的 ViewModel + AOT 生成属性元数据 | 有完整测试示例，AOT 回归待验证 |
| 设置 int/bool/string | Context.Settings.Register + PluginSetting<T> | 同 ID、同名同类型的存储兼容 |
| IMessenger / InfoBarMessage | Context.Notify | 已实现 |
| ITaskContext | CaptureDispatcher + InvokeAsync / RunOnUiAsync | 已实现，连续 await 待回归 |
| inject/ 目录 | manifest 显式 gameLibraries 和 game-libraries 能力 | 已恢复现有 Island 启动链路；实际游戏兼容性未验证 |
| Harmony、私有字段与任意宿主 API | 使用公开 API；缺少业务能力时单独设计契约 | 没有通用替代，不兼容 |
| 用户脚本（动态编译 C#） | 将所需功能改写为原生插件 | 不恢复旧脚本运行时 |
| ALC 卸载或替换 DLL | 逻辑禁用；重启进程后替换 | 不支持热卸载 |
| GenerateHutaoPlugin | VS Build/Native AOT Publish + Pack-Plugin.ps1：staging→pack→verify | 已提供脚本，本地验收待完成 |
| 编译期 .xaml / x:Bind | C++ 或 C# 编译型 XAML + 内嵌 XBF 的 PRI | C++ PrototypeHost 已实测。C# 与主程序接入待 Windows 验收。 |

“已实现”只表示有代码路径，不是所有 Windows/AOT 组合的 Pass。

## 2. 迁移项目文件

v1 示例包含这些项目项：

```xml
<GenerateHutaoPlugin>true</GenerateHutaoPlugin>
<PackageReference Include="Snap.Hutao.Plugin.SDK" Version="1.0.0" />
<EmbeddedResource Include="TestPluginPage.hxaml">
  <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
</EmbeddedResource>
<ProjectReference Include="..\..\Snap.Hutao.Remastered\Snap.Hutao.Remastered\Snap.Hutao.Remastered.csproj" />
```

删除 GenerateHutaoPlugin、旧 SDK 引用和主程序 ProjectReference。复制新版 templates/CSharpPlugin，修改项目名称、插件 ID 和 manifest。新版项目的关键项如下（完整配置见模板）：

```xml
<PropertyGroup>
  <TargetFramework>net11.0-windows10.0.26100.0</TargetFramework>
  <OutputType>Library</OutputType>
  <RuntimeIdentifier>win-x64</RuntimeIdentifier>
  <PublishAot>true</PublishAot>
  <NativeLib>Shared</NativeLib>
  <SelfContained>true</SelfContained>
  <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
</PropertyGroup>
<ItemGroup>
  <PackageReference Include="Snap.Hutao.Plugin.SDK" Version="2.0.0-preview.1" />
  <PackageReference Include="Microsoft.Windows.CsWinRT" Version="2.3.1" />
  <PackageReference Include="Microsoft.WindowsAppSDK" Version="2.6.6-experimental" />
</ItemGroup>
```

**C# 插件必须使用 Native AOT Publish。** 即使宿主没有启用 AOT、即使你的 C# 项目已经设置 PublishAot=true，普通 dotnet build 产生的 IL DLL 也不能直接安装；PublishAot=false 的普通 Publish 同样不支持。原因是宿主使用 Windows 原生 DLL 加载器和 HutaoPluginCreate 导出，不再使用 AssemblyLoadContext 加载 CLR 程序集。SDK 不会在运行时将托管 DLL 编译成原生代码。

C++ 插件无需 Native AOT（它本来就由编译器、链接器生成原生代码）。使用 CppPlugin 模板和相同的 Contracts.winmd。

## 3. 生命周期和设置迁移

v1 TestPlugin.OnLoad：

```csharp
pluginService = context.ServiceProvider.GetRequiredService<IPluginService>();
Counter = new PluginSetting<int>(pluginService, nameof(Counter), 0);
Counter.Register(Manifest.Id, "Counter setting for Test Plugin");
messenger.Send(InfoBarMessage.Success("Hello Test Plugin"));
```

v2（插件实例字段，而不是 static 宿主类型）：

```csharp
private PluginSetting<int> counter = null!;
private PluginSetting<string> message = null!;

public override void OnLoad(PluginContext context)
{
    counter = context.Settings.Register("Counter", 0, "Counter setting");
    message = context.Settings.Register("Message", "Default Message", "Message setting");
    context.Notify(NotificationKind.Success, "Hello Test Plugin");
}
```

实际更新必须等待持久化完成：

```csharp
await counter.SetValueAsync(counter.Value + 1);
await message.SetValueAsync("New message");
await counter.ResetAsync();
```

OnLoad 注册默认值；在 active 状态读写设置。在 OnInstall 需要迁移自己的文件时使用 Context.DataDirectory，确保幂等。保留 manifest ID、设置名称和类型，旧的 JSON 设置才有机会被读取；不要在安装时把所有值无条件重置。

## 4. 导航：不再寻找主窗口，也不需要 Harmony

v1 示例在 OnEnable 中调用 SetupPage，使用反射寻找 MainView、遍历 VisualTree，再执行：

```csharp
navigationView!.MenuItems.Add(testPluginItem);
```

点击时直接替换 Frame.Content；窗口重建时用 Harmony patch 再添加。

v2 在 OnEnable 进行两次注册（完整可编译入口及项目文件见 [上手指南](PluginSDK2-GettingStarted.md)）：

```csharp
public override void OnEnable()
{
    Context.RegisterView("counter", CreateCounterView);
    Context.RegisterNavigationItem("counter-nav", "Counter", "\uE74C", "counter");
}

private PluginView CreateCounterView()
{
    Button button = new() { Content = "Show counter" };
    void Click(object sender, RoutedEventArgs args)
    {
        Context.Notify(NotificationKind.Information, $"Counter: {counter.Value}");
    }
    button.Click += Click;
    return new(button, () => button.Click -= Click);
}
```

manifest 的 capabilities 必须包含 navigation、notifications，ui.mode 使用 programmatic（若调用 LoadXaml 则改为 loose 并添加 xaml）。第一个 ID counter 是视图工厂 ID；counter-nav 是菜单项 ID，最后一个参数 counter 引用视图。多个菜单项可以引用不同视图；局部 ID 在各自插件内唯一。Glyph 是字体图标字符，不是图片路径。

在正式 Snap Hutao 中启用成功后，PluginService.Navigation 读取 active session 贡献，ServiceRecipientNavigationViewBehavior.RefreshPluginNavigation 创建真正的 NavigationViewItem 并加入主程序 MenuItems；点击后通过 PluginHostPage 承载 CreateView 返回的 UIElement。插件不需要也不能拿到 MainView 实例。禁用时撤销贡献并移除菜单；窗口重新构建时从服务重新生成。注意：当前接口只允许在 OnLoad / OnEnable 注册，不允许任意时刻直接修改宿主菜单；新导航行为须通过宿主实际运行测试。

C++ 对应实现（在实现 IHutaoPlugin 与 IPluginViewFactory 的类中）：

```cpp
void OnEnable()
{
    auto contributions = context.Contributions();
    viewRegistration = contributions.RegisterView(L"counter", get_strong().as<IPluginViewFactory>());
    navigationRegistration = contributions.RegisterNavigationItem(
        L"counter-nav", L"Counter", L"\uE74C", L"counter");
}
```

参见完整 [C++ 模板](../src/Snap.Hutao.Plugin.SDK/templates/CppPlugin/MyPlugin.cpp)，其中包含 CreateView、IPluginView、OnClosing 与 Revoke。C# facade 自动保存注册 token；C++ 自己保存和撤销。

## 5. UI、XAML 与 ViewModel

v1 TestPluginPage.hxaml.cs 的核心：

```csharp
public TestPluginPage(HutaoPlugin plugin) : base(plugin)
{
    LoadXamlManually("TestPluginPage.hxaml");
}
```

旧 PluginScopedPage.LoadXamlManually 先调用 Assembly.GetManifestResourceStream 读取 EmbeddedResource，找不到就尝试 Assembly.Location 旁的文件；再 XamlReader.Load(string)，将结果赋给 Page.Content。这不是 XAML 编译器生成的 InitializeComponent，也没有使用该插件自己的 XBF/PRI。

v2 把松散 XAML 改为标准 .xaml，作为 None 项而不是 Page，放到 staging/views/，调用：

```csharp
[WinRT.DynamicWindowsRuntimeCast(typeof(FrameworkElement))]
private PluginView CreateCounterView()
{
    FrameworkElement root = (FrameworkElement)Context.LoadXaml("views/Counter.xaml");
    CounterViewModel model = new(counter);
    root.DataContext = model;
    return new(root, () =>
    {
        root.DataContext = null;
        model.Dispose();
    });
}
```

此处 CounterViewModel 是你自己的类，不引用主程序 ViewModel 基类。最小的可绑定实现如下（用于 TextBlock Text="{Binding Counter}"）：

```csharp
[WinRT.GeneratedBindableCustomProperty]
public sealed partial class CounterViewModel : System.ComponentModel.INotifyPropertyChanged, IDisposable
{
    private readonly PluginSetting<int> counter;
    private readonly IDisposable subscription;
    public CounterViewModel(PluginSetting<int> counter)
    {
        this.counter = counter;
        subscription = counter.Subscribe(() => PropertyChanged?.Invoke(this, new(nameof(Counter))));
    }
    public int Counter => counter.Value;
    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
    public void Dispose() => subscription.Dispose();
}
```

可直接复制仓库 samples/CSharpAot/SampleViewModel.cs 和 BindingPage.xaml 学习 OneWay、TwoWay、ICommand、INotifyPropertyChanged、GeneratedBindableCustomProperty；完整代码与资源布局见 [XAML 指南](PluginSDK2-Xaml.md)。不要把旧 XAML 的宿主内部控件、markup extension 或反射式绑定视为公共 API。

## 6. 命令、通知和线程

v1 的 IMessenger 发送 InfoBarMessage 改成 Context.Notify。无需获取宿主的 IMessenger。注册一个可在插件管理页与 PrototypeHost 运行的命令：

```csharp
public override void OnEnable()
{
    Context.RegisterCommand("increment", "Increment counter", async () =>
    {
        await counter.SetValueAsync(counter.Value + 1);
        Context.Notify(NotificationKind.Success, "Counter updated");
    });
}
```

manifest 增加 commands、settings、notifications。异步命令经 SDK adapter 执行；普通 UI Click 回调则显式调用 Context.RunOnUiAsync(Func<Task>)，而不是直接使用宿主的 ITaskContext。后台任务先在 UI 捕获 dispatcher，worker 中只处理普通数据，通过 dispatcher.InvokeAsync 回 UI。详见 API 与 Engineering Notes E14。

## 7. 原有游戏 DLL 扩展

v1 的 [GameIslandInterop](https://github.com/SnapHutaoRemasteringProject/Snap.Hutao.Remastered/blob/2.0.2/src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Service/Game/Island/GameIslandInterop.cs) 从已启用插件的 inject/ 目录枚举 DLL。v2 恢复了此宿主扩展点，但不再根据目录中出现的任意文件自动执行：包需显式声明 gameLibraries 与 game-libraries 能力，用户批准包时可以看到具体请求。包验证、已启用会话、完整性检查均由宿主处理，不需要插件自己调用 Windows 进程操作 API。

迁移原有游戏附加组件时，参照 [包规范](PluginSDK2-Packages.md) 的声明格式，保留合法分发的第三方组件并重新制作 v2 包。仍需有效的 v2 原生插件入口；旧版纯托管插件不能直接复用。仅覆盖原有 Island 新游戏启动路径；普通启动、已经运行的游戏、Resume 不属于该功能。PrototypeHost 不启动游戏，也不执行这类贡献。第三方组件的合法性、游戏规则、反作弊兼容、账号安全和配置写入位置须独立确认，ReShade 尚未验证。

## 8. 生成新包、安装并验证

按照 [Getting Started](PluginSDK2-GettingStarted.md) 生成或取得 SDK 分发，在 VS Build/Publish 后运行模板 Pack-Plugin.ps1，检查产物 .hutao。正式应用中卸载旧插件（保留 Plugins/<id> 数据），重新安装同 ID 的新包，确认来源和哈希。依次测试：启用、导航项可见、视图打开、命令、设置读写、禁用、重新启用、重启、卸载。若有游戏附加组件，只在允许且受控的测试环境验证其兼容性；不能由包验证通过推断游戏运行正常。

不要修改主程序安装文件、恢复 Harmony/ALC、使用未公开的内部对象或绕过游戏安全机制。缺少必要业务接口时，提交最小复现和期望公共服务，而不是依赖内部实现。
