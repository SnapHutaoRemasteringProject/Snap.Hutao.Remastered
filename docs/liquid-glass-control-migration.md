# 液态玻璃控件切换与参数设置

## 2026-10-16 页面进入异常：修正资源字典的单一所有者

运行时堆栈确认上一轮 ScopedPage.OnLoading 把已经属于旧 Resources 的材质字典再次 Add 到新 Resources，触发 Element is already the child of another element。Contains 不能充当资源对象可重新挂载的证明；此前对应源码断言也未覆盖原生单父级约束。

现在基类构造函数只创建延迟初始化器，不加载或挂载字典。派生页面 InitializeComponent 完成后，在 OnNavigatedTo、页面进入可视树前创建并挂载材质；OnLoading 仅作非 Frame 宿主的幂等兜底。PageResourceScope 对相同资源根只挂一次，根变化时通过工厂创建新 ResourceDictionary，绝不把已有子对象转移到另一根下。注册的仍是页面实际拥有的字典，保留运行时材质切换，不吞异常或关闭玻璃。Character Wiki 和三个数据页面继续共用 AcrylicBorderCardStyle/HutaoCardBrush 的表面契约。资源文件复用使用 Source 加载独立字典，参见[微软资源字典说明](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.resourcedictionary.mergeddictionaries)。

生产所有权辅助代码链接到单元测试，测试替身强制单父级，而不是使用允许重复父级的普通 List。新增六项覆盖延迟初始化、导航/Loading 重复调用、更换资源根、不同页面隔离、失败重试与重入；六项及既有十四项运行时回归全部通过，源码回归二十二项通过。结果：C:/Files/OpenNet/artifacts/hutao-page-resource-tests/page-ownership.trx。没有运行应用；单父级契约测试不等于实际 WinUI 导航实测。

本次完整 slnx 增量构建通过，0 错误，用时 5:20.89；日志 C:/Files/OpenNet/artifacts/hutao-page-resource-ownership.log。仍有既有警告（包括启动页两条 WMC1506 以及部分通知类型的 AOT 分析警告），没有声明全项目零警告。SaltConstants 由原生成器成功生成，未改盐值或生成配置。

## 2026-10-16 启用状态单向传递与独立配置窗口

绑定模式按数据流选择，不按控件数量选择：只读且不变的名称/图标可用 OneTime；异步结果、实时读数和启用状态用 OneWay；用户可编辑的选择值、开关和参数用 TwoWay。[微软的数据绑定说明](https://learn.microsoft.com/en-us/windows/apps/develop/data-binding/data-binding-in-depth)也作同样区分。本轮保留现有设置值的 TwoWay，绝不把 IsEnabled 当作应回写业务数据的输入值。

此前 OnManagedIsEnabledChanged 在玻璃控件加载时，仍将临时的 IsEnabled=false 写回原生控件，即使 UsesCompiledEnabledBinding=true 也会写入。这形成了原生→玻璃→原生的禁用反馈环，切回原生后错误的本地值仍存在。现已删除该反向回调；编译绑定继续单向读取业务控件，兼容入口也仅安装从原生到玻璃的 OneWay 绑定。不强制所有控件为 True，真实的业务禁用/命令 CanExecute 仍有效。

设置页的旧参数展开区替换为两张可点击卡片：

- LiquidGlassControlsWindow：选择全局材质、按 14 类控件启用，预览跟随当前材质和类型开关。开关使用原生 ToggleSwitch 的 TwoWay。
- LiquidGlassEffectsWindow：选择控件类型，使用原生 Slider 的 TwoWay 调整 30 项光学参数；预览始终是对应真实库控件。支持单参数重置和当前类型整体恢复默认。

两窗接入胡桃现有 XamlWindowController、标题栏、初始尺寸与 scoped service 生命周期；重复点击激活已打开窗口，关闭清除窗口引用/通知订阅。首次选择在 Loaded 后补齐，避免 ItemsSource 尚未建立时丢失 SelectedIndex。SettingAppearanceViewModel 观察 AppOptions.ControlMaterial 的通知，因此从新窗口改变材质时原设置页也能更新。

每类覆盖以 LiquidGlass.Profiles.v1.Type 键的 JSON 保存在 LocalSetting。LiquidGlassOverrideStore 对未知、非有限及越界值过滤，默认值移除覆盖；恢复默认不改变类型启用状态或其他类型的数据。旧全局光学参数保留在存储中但不再覆盖真实控件预设，避免它们继续污染独立类型的默认效果。每次刷新先恢复该实际画刷的光学快照，再应用本类型覆盖，不累乘，也不改写库拥有的 CornerRadius、BezelWidth、GlassThickness、DPI 或动效。首页实际玻璃按钮显式归类为 Card。参数刷新合并到 UI 队列，应用退出后不再触碰画刷。

增加 69 个中文/英文资源键。源码回归 18 项、加载/测量/独立配置实际单元测试 10 项通过，结果 C:/Files/OpenNet/artifacts/hutao-glass-profile-tests/glass-profiles.trx。未启动应用。

完整 slnx 增量构建尚未通过：首次构建的新窗口/XAML 阶段完成后，最终 C# 编译报 HoyolabOptions.cs 的 4 处 SaltConstants 缺失；日志 C:/Files/OpenNet/artifacts/hutao-glass-enabled-profiles.log。确认 Web/Hoyolab/SaltConstants.cs 仅有注释模板，未改动该文件、未猜测签名盐、未压制错误。等待既有常量注入配置入口后再做统一完整验证；追加的退出保护与预览文本本地化没有再次进行同样注定失败的完整构建。此前“0 错误”记录仅代表对应历史版本，不代表本轮完整构建成功。

## 2026-10-16 悬停、测量与日历视口修复

首页卡片曾依赖外层 Border 提供永久表面；移除该表面后，默认原生 Button 的 PointerOver 状态仍会把背景换成半透明状态填充。NativeHomeCardButtonTemplate 现在固定唯一的 Background/Border，只用 StateOverlay 表示悬停与按下，不再替换表面。真实 LiquidGlassButton 的库模板和光学交互仍保留，没有恢复双层背景。

SizeRestrictedContentControl 原先把父布局安排后的 ActualHeight/ActualWidth 写回子控件 MinHeight/MinWidth；滚动容器中的拉伸尺寸会被永久记成内容最小尺寸。现在只稳定 base.MeasureOverride 返回的期望尺寸，不修改子控件的尺寸属性；宽度变化时清空保留高度，允许换行内容缩回去。DesiredSizeRestriction 独立纯计算被生产控件和单元测试共同使用。

签到日历的 AwardScrollViewer 原先明确允许越界绘制，移除外层卡片框后，滚动奖品会穿出顶部/底部。现在仅对奖励视口启用 Toolkit 的 ClipToBounds，并将 CanContentRenderOutsideBounds 设为 False；页头和操作栏保持独立层级。没有对 MaterialCardButton 整体增加 InsetClip，所以玻璃外缘仍能响应指针放大。标题改为图标、可截断文本和 UID 三列，避免英文标题与 UID 重叠。微软对[滚动内容越界属性](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.scrollcontentpresenter.cancontentrenderoutsidebounds)的定义与该局部裁剪用途一致。

WMC1506 实际针对 OneWay 路径缺少通知。已确认只读的 SearchToken、NameCultureInfoValue、Complex.AvatarView 与 SplendourBuffView 模板中，53 处局部 x:Bind 改为 OneTime；不修改模板内的共享材质状态或页面 ViewModel 绑定。签到 AwardView.IsClaimed 会在签到后修改，改为 ObservableObject 的 ObservableProperty，继续使用 OneWay，而不是冻住可变状态来消除警告。

本轮增加测量、局部裁剪、悬停表面和通知契约的检查。源码检查 11 项，缓存与期望尺寸的实际单元测试 6 项；单元测试结果 C:/Files/OpenNet/artifacts/hutao-layout-tests/layout-and-loading.trx。没有启动应用，真实指针效果、日历滚动和设置页的最终布局仍需运行复测。

最终 Snap.Hutao.Remastered.slnx /t:Build 的 Debug/x64 增量构建通过：0 错误、495 警告，用时 3:22.98，日志 C:/Files/OpenNet/artifacts/hutao-hover-calendar-sizing.log。按文件、行、列去重的 WMC1506 从上一轮的 537 处降到 478 处；总警告还包括现有测试的断言参数顺序告警与 SQLite 依赖版本冲突。剩余绑定告警尚未逐类型处理完毕，不能宣称它们均无害。重新生成的 SignInCard.g.cs 已包含 IsClaimed 的 PropertyChanged 分支。没有手改生成代码，也没有 Clean/Rebuild。

## 2026-10-16 运行时绑定与加载修复

本轮针对“编译通过但页面空白/无法进入”检查了实际生成的订阅代码，而不是只按警告数量修改绑定模式。

- IAdvancedCollectionView<T> 的实现早已发送 CurrentItem/Count 的 PropertyChanged，但接口没有继承 INotifyPropertyChanged。x:Bind 按声明类型生成代码，通过该接口访问 CurrentItem 时原先没有订阅选择变化。接口现已公开通知契约并保留 setter，覆盖深渊、挑战、角色详情等使用同一接口的页面，不需要退回反射 Binding。
- 所有异步 ObservableProperty ImmutableArray 集合以 [] 初始化。深渊、剧诗统计和挑战页在接收缓存/服务结果时还调用 EmptyIfDefault；缓存及部分尚未填充的角色属性集合也有空数组初始值。默认 ImmutableArray 不是空集合，装箱后 ICollection.Count 会抛异常；直接传给 ItemsSource 会中断页面 Loading。这与 [.NET runtime #98300](https://github.com/dotnet/runtime/issues/98300) 描述的 default 状态区别一致，不是通过捕获 ItemsSource 异常来掩盖。
- InvokeCommandOnLoadedBehavior 在 Command 编译绑定晚于 Loaded 到达时，排队重试一次执行判断；同批 CommandParameter 先完成更新，仍以 executed 防止重复执行，且回调重新检查附着与加载状态。
- 首页卡片的 ViewModelSlim 生命周期在每次 Loaded 恢复 IsViewUnloaded=false，在每次 Unloaded 置 true。暂时卸载后再挂载不再永久保留禁写标志。
- StatisticsCache 的同类型并发调用现在等待同一个初始化任务。失败会通知所有等待者并移除失败记录，后续导航能够重试；原代码失败后永远不完成等待任务，并且并发 TryAdd 失败者可能提前返回。
- RuntimeOptions 的废弃包装绑定改为 HutaoRuntime 的静态 x:Bind；自定义材质控件/转换器声明 partial，满足 CsWinRT 的 AOT 生成要求。未压制 WMC1506，也没有把需要异步刷新或选择联动的绑定批量改为 OneTime。只读数据模板的通知警告不能等同于页面没有执行加载命令。

新增 tests/binding-runtime-contracts.test.mjs（6 项源码检查），以及 StatisticsCacheTest（3 项实际缓存并发/失败重试测试，链接生产源代码且不依赖 WinUI 启动）。两组检查均通过。测试结果保存在 C:/Files/OpenNet/artifacts/hutao-binding-tests/statistics-cache.trx。本轮仍只通过 slnx 增量编译，未启动应用；真实页面的导航与画面需要单独运行复测。

最终 Snap.Hutao.Remastered.slnx /t:Build 的 Debug/x64 增量构建通过：0 错误、539 警告，用时 3:24.96，日志 C:/Files/OpenNet/artifacts/hutao-binding-runtime-complete.log。只读检查重新生成的 SpiralAbyssRecordPage.g.cs 已确认 SpiralAbyssEntries 和其 CurrentItem.Floors 都新增了 PropertyChanged 订阅、解绑与 CurrentItem 分支更新。RuntimeOptions 的 CS0618 和两个自定义材质类型的 CsWinRT1028 已不再出现；剩余警告主要为 WMC1506、SQLite 版本冲突和风格检查，未被压制。未编辑生成代码内容、未 Clean/Rebuild。

## 2026-10-16 编译契约与首页卡片修复

静态命令、键位列表和语言列表直接从声明类型进行 x:Bind，不再经 ViewModel 实例访问静态成员。不可变列表和命令采用默认 OneTime；HotKeyOptions.IsInGameOnly 保留 TwoWay 和原有持久化 setter。WikiAvatarPage 的整数页数回退为 0，SophonProgressBar.HeaderContent 直接绑定控件代码后置属性，不经被解析为 UserControl 的命名根 Self。

UserView 保留 PersonPicture，以 BitmapImage.UriSource 的编译绑定接收头像 Uri，不依赖 Uri 到 ImageSource 的隐式转换。未选用户的本地化提示由 GetUserNickname 函数绑定返回，替代 x:Bind 不支持的资源标记 FallbackValue；原生与玻璃分支保持相同逻辑。

IAdvancedCollectionView<T>.CurrentItem 补齐与实现一致的 setter；LayoutSwitch.CurrentProperty、AutoSuggestTokenBox.TextProperty 使用公共 DP 标识符，供编译绑定跟踪。FilterTokens 是共享 ObservableCollection，ItemsSource 使用 OneWay：输入控件会将其包装成 InterspersedObservableCollection，不能通过 TwoWay 把包装集合赋回只读源；集合内增删仍通过同一个底层集合双向可见。

动态 ThemeResource 转换器保留普通 Binding，并为对应数据类型提供 GeneratedBindableCustomProperty；可空的筛选颜色改用非空 QualityColor 供 x:Bind 使用。CultivateEntryFlyout 的内容使用以 CultivateEntryView 为 x:DataType 的独立模板，不再误把条目字段和命令参数当成页面 ViewModel。

首页 MaterialCardButton 现在是没有额外背景、边框或圆角裁剪的 ContentControl 宿主，内部继续使用真实 LiquidGlassButton。业务 Body 只存在一份，切换时交给当前按钮。Announcement 卡片模板移除旧表面，只保留阴影宿主；原生按钮使用独立普通主题画刷，玻璃按钮和 shader 的圆角与原生卡片一致，保留库的指针与缩放动效。Loaded/Unloaded 管理材质状态回调，命令及布局属性由 DP 回调传递。

编译只通过 Snap.Hutao.Remastered.slnx 增量验证，不使用 Clean/Rebuild、不修改生成代码内容、不启动应用。先前确认为过期且阻断预编译的三个生成文件已逐文件移到 OpenNet 的 artifacts 下保留诊断备份，由正常编译重新生成；没有清空 obj/bin。运行时视觉、快速材质切换和首页卡片悬停仍需单独验证。

最终主项目目标 Snap_Hutao_Remastered 的 Debug/x64 构建通过：0 错误、553 警告，用时 5:20.06；日志为 C:/Files/OpenNet/artifacts/hutao-userview-bindings-verified.log。警告包括 SQLitePCLRaw.core 版本冲突、无通知源的 OneWay 绑定、旧属性与代码风格检查；本轮没有顺带更改依赖版本或批量重写通知语义。

## 当前入口与持久化

OpenNet 的“外观 → 液态玻璃控件与效果”和胡桃的“设置 → 外观 → 液态玻璃控件与效果”提供相同选项：

- 按类型启用：按钮、切换按钮、超链接按钮、复选框、单选按钮、Slider、文本输入、搜索输入、密码输入、ComboBox、ToggleSwitch、卡片与表面。
- 基础光学参数：总体强度、模糊、折射、色散、高光、放大，均为 0–2 倍。
- 1.2.0 高级参数：色散范围、菲涅耳范围/硬度/强度、眩光范围/硬度/强度/聚集度/反向比例/方向、整体背景缩放、模糊混合比例、亮度、色温、自然饱和度。范围与单位沿用库 API，不再统一限制为 0–2。
- 类型启用选项使用 ToggleSwitch / LiquidGlassToggleSwitch，并由 ToggleSwitchMaterial 选择实际分支；不再使用 CheckBox 充当设置开关。
- 默认 1 倍保留各控件原本的预设。总体强度作用于折射、色散、高光、局部放大以及菲涅耳和眩光强度；模糊使用独立倍数。
- 类型开关会切换实际视觉控件。总体材质为默认时，所有类型均使用原生分支。

OpenNet 存储于 AppSettingsDatabase 的 `liquid_glass` 分类；胡桃使用 LocalSetting 的 `LiquidGlass.*` 键。数值在写入 brush 前还会按库允许的范围限制。设置修改会同步到已打开的分支和已加载的玻璃控件。

高级参数使用绝对值：菲涅耳/眩光强度为 0 时关闭对应光学层；整体背景缩放为 1 时不缩放；模糊混合比例为 0 时使用原始背景、1 时使用完整模糊；亮度和色温为 0 时保持中性。眩光方向使用度，不是现有 LightAngle 的弧度。圆角、相对形状半径和 RasterizationScale 仍由控件几何与 DPI 管理，不作为全局参数覆盖。

OpenNet 的 XAML 前缀统一为 `essential`（WinUI3Package）、`wl`（WinUI.LiquidGlass）、`ouxct`（控件 Theme）；胡桃沿用自身 Toolkit 的 `cwc` 和 Theme 的 `shuxcth`，玻璃控件使用 `wl`。同一文件不再重复声明 materialcases/materialcontrols/materialtheme。

## 共用状态

OpenNet 的资源键为 `MaterialControlState`，类型为 `OpenNet.UI.Xaml.Control.Theme.MaterialControlState`。胡桃的资源键为 `ControlMaterialState`，类型为 `Snap.Hutao.Remastered.UI.Xaml.Control.Theme.ControlMaterialState`。

数值设置使用 Slider / LiquidGlassSlider，按 SliderMaterial 切换；范围、StepFrequency 和键盘步进按参数配置。右侧显示两位小数。设置页通过 LiquidGlassParameterSettingStyle 和 LiquidGlassToggleSettingStyle 中的 ControlTemplate 复用结构。每个 ContentControl 有自己的 SettingValue / SettingIsOn 附加属性，TwoWay 绑定到持久化状态；范围与大小步进分别由 SettingMinimum、SettingMaximum、SettingStep、SettingLargeStep 提供。ControlTemplate 的 TargetType 明确为 ContentControl，两支直接使用 x:Bind (ContentControl).(owner.SettingValue) / SettingIsOn 访问宿主附加属性；不再经过 Tag、TemplatedParent 和 ElementName 绕行。数值读数由编译函数绑定 FormatSettingValue 格式化。每次套用模板都会创建独立控件与 brush，不共享实例。业务页面中的命名控件仍保留独立 Presenter；不能把含有控件实例的 CaseCollection 放进共享资源作为模板复用。

每种类型都有 `ButtonEnabled`、`ButtonMaterial` 这样的属性。Enabled 是持久化 Bool；Material 是计算得到的 Int32，0 表示原生，1 表示玻璃。编译绑定通过 OpenNet 的 `ouxct:MaterialControlState.Current` 或胡桃的 `shuxcth:MaterialBindingSource.Current` 取得原有 Application 资源实例；不新建第二份设置状态，也不共享动态 brush。

```xml
<essential:SwitchPresenter
    IsTabStop="False"
    HorizontalContentAlignment="Stretch"
    Value="{x:Bind ouxct:MaterialControlState.Current.SliderMaterial, Mode=OneWay}">
    <essential:Case>
        <essential:Case.Value><x:Int32>0</x:Int32></essential:Case.Value>
        <Slider Value="{x:Bind ViewModel.Value, Mode=TwoWay}" />
    </essential:Case>
    <essential:Case>
        <essential:Case.Value><x:Int32>1</x:Int32></essential:Case.Value>
        <wl:LiquidGlassSlider
            ouxct:MaterialControlState.IsManaged="True"
            Value="{x:Bind ViewModel.Value, Mode=TwoWay}" />
    </essential:Case>
</essential:SwitchPresenter>
```

胡桃使用同样结构，将命名空间及资源键替换成自己的类型。Case.Value 必须是明确的 Int32，不能把字符串 `"0"` 当作整数。

## 业务绑定与命名控件

有 ViewModel 的普通使用点，两支绑定同一个业务属性。库 1.2.0 的玻璃 ToggleSwitch 已提供 IsOn、Header、OnContent、OffContent 等依赖属性，支持 TwoWay Binding。

原有代码直接读取、初始化或更新的命名控件，保留原生命名实例作为原有业务入口；玻璃分支通过 NativeControl 和 DP 绑定接入这个入口。原生实例位于未选中的 Case 中，不参与视觉布局。其 DataContext 和 RequestedTheme 显式跟随 Presenter，避免断开视觉树后丢失数据源或主题。

必须同时同步内容、选择值、启用状态及业务代码会改变的属性。例如多行 TextBox 还需要 AcceptsReturn、TextWrapping、MaxLength 和尺寸约束；不能只同步 Text。

ToggleSwitch 的 Toggled、Slider 的 ValueChanged 等原有处理方法只保留在业务入口，玻璃值变化通过 TwoWay 绑定进入它，避免重复执行确认弹窗或保存逻辑。Button 的 Click 仍使用同一个兼容处理方法。

命名控件的 Visibility 由外层 Presenter 绑定，以便原有代码隐藏控件时两个分支都退出布局。Presenter 本身不增加 Tab 停靠。教学提示目标应指向外层 Presenter，焦点操作应指向当前可见的输入控件。

## 独立 brush 与卸载

每个玻璃控件保持库创建的独立 GlassBrush。应用状态只是参数源，不把一个动态 brush 分配给多个控件。

`IsManaged` 在 Loaded 时取得该控件的 brush 并保存原始预设。参数更新始终从这份预设计算，而不是对上次已经缩放的结果继续相乘。Unloaded 只移除跟踪，不修改正在断开的 Composition 属性。

预设缓存按 brush 身份弱关联保存，不能随着控件 Unloaded 一起删除：切换材质或返回缓存页面可能复用同一个 brush。再次 Loaded 必须复用首次记录的基准；只有换成新 brush 才记录新的预设。已卸载控件不参与参数更新，重新加载时再应用当前设置。

已有语义卡片画刷保留原来的应用/页面资源生命周期。CardEnabled 和光学参数会应用到这些表面；窗口的 Mica、Acrylic、自定义 backdrop 类型仍由原来的窗口外观设置负责。

## 强类型属性与自定义卡片

InfoBar.ActionButton 接受 Button，不能放 SwitchPresenter。OpenNet 使用 XamlToolkit SwitchConverter，胡桃使用 ControlMaterialSelector；它们从两个 Button 实例中选择一个赋给 ActionButton。

胡桃七张首页卡片由 MaterialCardButton 宿主承载。宿主内部使用 SwitchPresenter，在两个按钮间转移同一个 Body 内容对象，先清空旧按钮的 Content 再赋给新按钮，防止重复创建 ScrollViewer、绑定、加载命令或同一元素同时有两个父级。

动态创建的控件也应使用 Presenter 工厂。OpenNet 提供 CreateTextBoxPresenter、CreateRadioButtonPresenter；胡桃提供 CreateButtonPresenter。调用者继续从原有状态实例读值并接入同一事件处理方法。

## 设置项模板用法

```xml
<ContentControl
    Style="{StaticResource LiquidGlassParameterSettingStyle}"
    ouxct:MaterialControlState.SettingMinimum="0"
    ouxct:MaterialControlState.SettingMaximum="100"
    ouxct:MaterialControlState.SettingStep="1"
    ouxct:MaterialControlState.SettingLargeStep="10"
    ouxct:MaterialControlState.SettingValue="{x:Bind ouxct:MaterialControlState.Current.RefFresnelFactor, Mode=TwoWay}" />
<ContentControl
    Style="{StaticResource LiquidGlassToggleSettingStyle}"
    ouxct:MaterialControlState.SettingIsOn="{x:Bind ouxct:MaterialControlState.Current.ButtonEnabled, Mode=TwoWay}" />
```

附加属性只是单项宿主的值桥接，不单独保存设置。实际持久化、参数验证和更新已加载画刷仍由 MaterialControlState / ControlMaterialState 完成。胡桃将上例的 ouxct、类型及资源键换成 shuxcth、ControlMaterialState。

## x:Bind 优先规则与保留项

- 材质状态、设置值、同一名称作用域内的控件属性同步优先 x:Bind。始终显式保留 OneWay / TwoWay；x:Bind 默认 OneTime 与旧 Binding 默认 OneWay 不同。
- 普通页面的 DataContext 绑定，在源码明确 InitializeDataContext / InitializeViewModel 的实际类型后，使用 `((vm:ViewModelType)DataContext).Property`。嵌套子 ViewModel 沿其已知属性路径访问，不能只根据设计时提示猜类型。
- DataTemplate 必须有 x:DataType；跨模板引用页面字段不做机械替换。参考 [WinUI #2508](https://github.com/microsoft/microsoft-ui-xaml/issues/2508)。
- ControlTemplate 可以使用 x:Bind，其 TargetType 即编译绑定的宿主类型。附加属性不能直接写成 `x:Bind (owner.Property)`，编译器将其判为非法路径；使用 `x:Bind (ContentControl).(owner.Property)`。依据：[官方模板说明](https://learn.microsoft.com/en-us/windows/apps/develop/platform/xaml/templatebinding-markup-extension)、[编译器路径语法](https://github.com/microsoft/microsoft-ui-xaml/blob/main/src/XamlCompiler/BuildTasks/BindingPath/BindingPath.g4)。
- C++ 引用命名 x:Load 元素的旧 Binding 保留，避免 [WinUI #11471](https://github.com/microsoft/microsoft-ui-xaml/issues/11471) 生成非法 .obj 表达式。该问题列于 [OpenNet #11](https://github.com/hoshiizumiya/OpenNet/issues/11)，不是所有 x:Bind 或模板绑定都受限。
- 没有 x:Class 和 InitializeComponent 的资源字典、来源类型未确定的 DataContext、部分跨模板/代理以及强类型属性槽中的对象式 Binding 仍保留运行时绑定。不要仅为了消除字样而改变其作用域或生命周期。
- 命名玻璃控件的 IsEnabled 编译桥接标记 UsesCompiledEnabledBinding=True；GetBindingExpression 无法识别 x:Bind，兼容逻辑只能为运行时工厂恢复 Binding，不能覆盖编译绑定。
- 源属性必须提供变更通知，尤其是同时存在原生/玻璃分支的可编辑值。首页卡片、游戏设置及调试选项已补齐通知；静态快捷键设置保留服务接口，由可通知的实例属性提供 UI 绑定。公告编辑使用 ObservableObject 编辑模型，在提交时生成原有 UploadAnnouncement 请求 DTO，不改变服务端 JSON 契约。
- C++ / C# 动态控件工厂中的程序式 SetBinding 仍有必要，不能在普通命令式代码中直接创建 x:Bind。

## 映射边界

当前应用中具有直接库对应类型的控件都接入上述选择方式；搜索输入使用 LiquidGlassSearchBox。AppBarButton、RadioButtons 聚合控件、NumberBox、TabView 以及 token 输入等自定义模板内部组件没有可直接替换的同契约类型，保持现有组件。不要把普通列表当成 LiquidGlassTabBar，或把模板中有名称契约的原生子控件再包成 Presenter。

新增相关控件时，同时检查两支的命令、输入值、可见性、焦点、主题和命名引用。本文所述变更只进行了源码结构及引用检查；依用户要求未执行编译、单元测试或运行烟测。

运行时仍待确认的交互包括：输入或选择后切换材质是否保留值；重复切换和导航返回后强度是否保持不变；原有命令是否只执行一次；搜索快捷键是否聚焦当前可见输入框；修改参数后已打开窗口是否同步。XML 可解析仅证明文件结构可读，不证明 XAML 编译、绑定解析或实际运行正确。
## 2026-10-16 搜索聚焦、下拉间距、页面底层与通知契约

本轮遵循显示绑定优先 OneWay、编辑/选择值使用 TwoWay 的规则；没有新增 OneTime 或屏蔽 WMC1506。角色、装备、技能、命座、背包评分、祈愿统计、培养统计、通知栏、用户资料、公告与深渊战斗等展示对象补充真实属性通知。评分/完成状态/时间格式等派生值同步通知；角色监听命座子对象变化；倒计时历史使用 ObservableCollection 并通知汇总值；TypedWishSummary 与 Wish 共用同一条通知事件。初始化或构造限定的成员保留访问限制，setter 调用 SetProperty，而非空的 INotifyPropertyChanged 实现。项目生成的 SkillGroupId/WeaponId/PromoteLevel 使用直接 SetProperty setter，避免两个源生成器相互不可见导致类型解析失败。

成就页玻璃搜索框移除普通 AutoSuggestBoxTextBoxStyle 覆盖，使用当前 WinUI.LiquidGlass 1.2.0 内置透明聚焦模板；高度 36、MinHeight 0。玻璃 ComboBox 保留自己的模板，不再复制原生 Style 中的 ControlTemplate；原生分支先 ApplyTemplate，Padding 保留竖向测量且左侧至少 12、右侧至少 4。原生主题的控件样式不变。

ScopedPage 在 InitializeComponent 后确认已注册的材质字典仍属于当前 Resources。My Characters、Backpack、Dev Plan 的面板显式使用页面作用域 HutaoCardBrush/HutaoSurfaceBrush，避免从全局样式取得回退背景。

源码回归 22 项、独立运行时回归 14 项通过；包含真实 setter 事件、同值不重复通知、基类/子类共用事件与 init 访问契约。测试结果：C:/Files/OpenNet/artifacts/hutao-notification-tests/notifications-final.trx。未启动应用。

完整编译尚未验证成功。中途一次联网生成通过后发现旧编译绑定缓存仍把 Histories 视作 List；CountdownCard 两处 ItemsSource 已显式改为 OneWay。旧 XAML 类型缓存仅移至同目录 XamlSaveStateFile.xml.before-notification-contracts 保留备份，没有 Clean/Rebuild。最新 slnx 增量构建再次仅报 SaltConstants 的 4 处缺失，日志 C:/Files/OpenNet/artifacts/hutao-notifications-verified.log；生成器的联网失败仍有波动，盐值和生成配置未修改。由于前置编译失败、生成的绑定文件尚未全部完成，本次日志没有 WMC1506 不能算告警清零；需网络可用时完整编译并实际检查聚焦/下拉/页面切换。
