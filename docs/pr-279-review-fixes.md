# Archive - PR 279 审查修复与 Guide/退出问题

## 五条审查意见

五条均成立，按当前分支的 CLAUDE.md 和实现核实后修复：

| 问题 | 修复 |
| --- | --- |
| 材质选择/新依赖绕过生成器 | Guide、SettingAppearance 使用 ObservableProperty、变更钩子和生成构造器的 PostConstruct，保留材质应用及外部设置同步；MainWindow 使用 GeneratedConstructor 注入 AppOptions。窗口自己的关联服务作用域仍在 PostConstruct 中建立，原关闭行为服务继续从该作用域取得。 |
| 预下载徽标只在结束时刷新 | 操作上下文把主线程进度报告同时送给窗口和启动页。Reset 立即发布未完成状态，Download 字节/块报告更新进度，Finish 在状态文件关闭后发布。活动操作用内存快照，不从绑定 getter 重新获取被操作持有的文件系统租约；拒绝旧操作迟到的回调。 |
| 新辅助类型 internal | 新增的 PageResourceScope、LiquidGlassOverrideStore、LiquidGlassOptics、预览/窗口管理器及相关记录和 JSON 上下文改为 public，未关闭分析器。 |
| RadioButtons 管理的是包装器而非单选项 | 在整个 RadioButtons 外切换材质；两分支的直接项分别为原生和真实玻璃 RadioButton。玻璃父选择器的 SelectedIndex 与原来的业务选择器 TwoWay 同步，子项不再另外双向绑定 IsChecked。覆盖成就导入、下载来源、两种养成保存和关闭行为选择。 |
| 玻璃地址栏被标题栏拖动区覆盖 | Passthrough 改为稳定的 GlassPresenter_2769；加载/尺寸变化刷新区域，跳过未加载元素，区域为空时显式清空，退出时不再访问关闭中的窗口。 |

构造生成器提供 PreConstruct/PostConstruct 扩展点，见[生成器源码](https://github.com/SnapHutaoRemasteringProject/Snap.Hutao.Remastered.SourceGeneration/blob/main/src/Snap.Hutao.Remastered.SourceGeneration/Snap.Hutao.Remastered.SourceGeneration/Automation/ConstructorGenerator.cs)。区域清除使用[ClearRegionRects](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.input.inputnonclientpointersource.clearregionrects)。

## Guide

State 为生成的通知属性，内容页 VisibleState 与底栏 StepIndex 从同一个状态派生。Material 为最后一个可见步骤，按钮直接完成，不再进入空白的第九页；终态也映射到材质页。离开协议页后，协议绑定的卸载更新不再改写其他步骤的按钮状态。资源下载完成时更新同一个 State。材质编辑器用两个明确的本地化原生选项和生成属性，避免编辑器随自身材质切换及嵌套动态绑定造成空白；预览保留独立原生底色、有限滚动和局部裁剪。

## 退出 dump 与生命周期

mini dump 显示 `0xC000027B`，内部 stowed HRESULT 为 `0x80004004/E_ABORT`。匹配微软符号后，故障桶为 `STOWED_EXCEPTION_80004004_CoreMessagingXP.dll!Microsoft::UI::Dispatching::DispatcherQueue::DeferInvokeCallback`，栈经过 `UserAdapter::UninitializeWindow`。没有据此认定是 HLSL 访问越界。原始异常栈指针所指的堆内容不在此 mini dump 中，不能唯一确定最初的托管回调。

周期行为改为每次挂载独立的信号会话，停止使用 PeriodicTimer.Dispose 完成等待而不抛取消异常；不再在卸载后重建 CTS。排队执行前和 UI 回调内检查退出、挂载身份与 IsLoaded。异步命令的 Task 被观察；SafeForget 的日志/取消观察器不再将自己的续体投递回关闭中的 XAML 同步上下文。材质加载/切换和输入区域更新也有退出保护。这些是针对已确认代码竞态的修复，不等于已实测证明原生退出崩溃完全消失。

## 验证

31 项源码回归、35 项运行时回归通过，其中包括生成器模式、两种材质的单选项结构与索引同步、可编辑地址栏区域、Guide 步骤一致性、下载首块完成前的进度、周期停止/主题变更/重新挂载。运行时测试直接链接生产的状态计算和信号代码；手动测试无回归。

## 2026-10-17 跟进：Guide、重复请求和最新 dump

- Guide 原来的动态 `Binding SelectedIndex` 会受到 `Segmented.OnApplyTemplate` 内部两次赋值影响。现在内容页和底栏使用编译的 OneWay 函数绑定，直接观察同一个生成通知属性 `State`，底栏 Loaded 后校正模板恢复的初始索引。已检查生成的 GuideView.g.cs：`PropertyChanged` 的 `State` 分支更新这两个函数绑定。参见 [Segmented 官方实现](https://github.com/CommunityToolkit/Windows/blob/main/components/Segmented/src/Segmented/Segmented.cs)。
- 引导页、设置材质列表和独立控件窗口统一显示“液态玻璃（实验性）”。使用新的中性资源键，避免旧英文翻译覆盖此标记；只编辑 SH.resx。
- `AnnouncementViewModel` 原先失败重试会再次执行全部四项首页请求，并在全部成功后重建卡片。该逻辑在本次 rebase 的 dev 中也存在，不是液态玻璃分支独有。现在每项分别合并并发加载，保留本页面内的成功结果，失败至少冷却 30 秒；重试不再重建卡片。日历首次加载和用户消息共享账号/UID 对应的进行中任务，拒绝旧账号迟到结果；页面取消也传入重试任务。
- `NetworkRetryCoordinator.MarkPending` 不再即时重排失败任务，改为定期检查截止时间/网络恢复事件。失败有退避，旧注册释放或旧任务完成不能操作同键的新注册。退出时停止注册、调度和通知，令牌在构造时保存，不再在 CTS 释放后取 Token；停止期间不从已释放 DI 根创建新探测作用域。
- 普通 CachedImage 不再随主题事件重新加载，已附着的相同 URI 不再重建 BitmapImage。删除每次设置图片前等待的清空淡出动画，避免卸载中的合成动画等待；卸载取消当前来源，保留重新挂载所需的 Loaded 事件。无效 URI 的占位加载移到锁外，避免递归取得同一锁。加载行为通过 ExecuteAsync/SafeForget 观察异步命令，而不是通过 Execute 的 async-void 重新向 XAML 抛出取消。
- ViewModel 的并发等待者不再进入拥有者的 finally 解锁；取消/失败也完成初始化等待。已初始化视图重复加载不再重新发出请求。
- 剩余两项 WMC1506 是 AspectRatio 值类型整项作为删除命令参数的空路径 x:Bind。改为 OneWay DataContext 绑定，跟随 DataContext 依赖属性替换，不给不可变值类型添加虚假通知，也不改 OneTime。

最新 `Snap.Hutao.Remastered.exe.38592.dmp`（2026-10-16 23:29）也记录 `0xC000027B`，SE02 内部 HRESULT 仍为 `0x80004004/E_ABORT`。原始 25 帧 stowed 栈指针指向的内存不在 mini dump 中，仍不能定位唯一回调。日志 `C:/Files/OpenNet/artifacts/hutao-latest-stowed-record.log`、`hutao-latest-stowed-analysis.log` 保留本地分析结果。没有上传 dump，也没有依据这些栈把原因归于 .NET 版本。

用户贴出的 `DefaultHttpClientFactory.Log.TryGetLogger` 异常在[官方实现](https://github.com/dotnet/runtime/blob/main/src/libraries/Microsoft.Extensions.Http/src/DefaultHttpClientFactory.cs)中被 `catch` 捕获；它和 SslStream scavenging 的首次异常不能直接证明 DispatcherQueue FailFast 的来源。未添加全局忽略 E_ABORT/ObjectDisposedException 的处理，原生退出仍需实际复测。

原来的 XAML 未处理异常处理器在 Exiting/Exited 为 true 时直接返回，跳过日志和调试断点；这能解释退出时只见到最终 FailFast 而缺乏托管诊断的情况。现在退出路径保留原始异常/堆栈/HRESULT 的 Trace 和日志，并在调试器附加时中断；同步上下文转交 WinRT 之前也输出原始回调异常。仍未将异常标记为已处理，不会掩盖未知致命错误；完全原生回调未必经过这些托管诊断。

跟进验证：38 项源码契约和 41 项运行时回归全部通过，包含新增加的 6 项真实 AsyncLoadOperation 并发/成功复用/失败冷却/异常释放/等待者取消/重新尝试测试。最终运行结果 `C:/Files/OpenNet/artifacts/hutao-home-guide-exit-tests/home-guide-exit-final.trx`。最终增量编译成功，0 错误、182 项现有警告，日志中无 WMC1506；日志 `C:/Files/OpenNet/artifacts/hutao-home-guide-exit-verified-20261017.log`。只读检查最终 DLL 确认包含最新实例退出异常处理器。未运行应用，也未请求米哈游线上接口。

## 图片不显示回归

以下两轮早期结论只检查了代码形状，未覆盖真实 WinUI 首次加载和消息队列关闭，因此并未解决用户实测的问题。相应的正则源码检查及简单 Guide 索引测试已删除，不能再用其通过数量证明界面正常。

上一轮 CachedImage 新加的模板子图片 `IsLoaded` 判断不正确：模板初始 `Visibility=Collapsed`、`Opacity=0`，父控件 Loaded 并不保证图片子元素已 Loaded，因此会在设置 Visible/UriSource 前退出。现保留父控件和应用退出检查，移除子元素赋值前的 Loaded 门槛；ImageOpened 早于 Loaded 时直接恢复不透明度。重新挂载始终解析最新 Source，只有 URI 相同且已成功解码的 BitmapImage 才复用并恢复不透明度，避免卸载期间更新来源后仍保留旧图。

新增两项源码回归同时核对实际模板的 Collapsed/零不透明度初值和上述顺序；40 项源码回归通过。此检查不替代 WinUI 运行时显示复测。增量编译日志：`C:/Files/OpenNet/artifacts/hutao-image-visibility-20261017.log`。未清理用户图片缓存，普通图片不随主题重复加载及磁盘下载去重保持不变。

## 再次对照原实现后的修复

图片恢复原来的“模板建立后即可开始加载”，以显式 Unloaded/请求取消表示失效，而不是用 IsLoaded 丢弃首次赋值。IImageCache 是全局单例，直接从应用容器取得，不再依赖只在窗口根 Loading 时写入的 ContentIsland.AppData；旧版的清空淡出等待曾隐含延迟了这个查找，移除延迟后该时序问题暴露。模板不再以零不透明度等待合成层动画，图片显示只由 XAML Opacity 管理。读取缓存不捕获 UI 上下文，取消和退出检查通过后才切回 UI 赋值；UI 生命周期错误不再作为“缓存损坏”删除文件。解码/来源错误写入 Trace，避免缺图只表现为空白而无诊断。

窗口关闭改为接受关闭请求后，先临时取消原生关闭并清空根 Content，等待 Unloaded 及其子元素卸载回调完成，再正式 Close、停止播放器和释放窗口作用域。应用整体 Exit 同样先卸载所有窗口内容，再排队停止消息循环，不把主动退出变为逐窗口的下载/浏览器关闭否决等待。播放器卸载使用加载时保存的 messenger，不再从已释放的窗口容器解析服务；取得 Source 分配后实际懒创建的 MediaPlayer，确保释放的是持有的播放器。背景/媒体行为每次挂载使用新 CTS，卸载时取消并释放。

背景透明度过渡不再使用 Toolkit 的 CompositionScopedBatch 等待。[官方实现](https://github.com/CommunityToolkit/Windows/blob/main/components/Animations/src/Builders/AnimationBuilder.cs)使用默认 TaskCompletionSource，并且取消仅 StopAnimation，仍保留 Completed 回调。现用元素拥有的 XAML Storyboard，在取消/卸载时先移除 Completed/Unloaded 处理器再 Stop，并异步完成等待者。通知面板的 TransitionHelper 也存在同样的批次完成模式，已将其展开/折叠改为同步 Visibility 切换，移除形变动画和逐条清空延时；功能保留，关闭不再遗留这些原生动画回调。InfoBarDelayCloseBehavior 原先卸载时不取消延迟任务，现卸载时取消并解除事件，延迟完成后检查令牌再请求 UI。未添加全局忽略未处理异常。

图片 URI 赋值也恢复旧版的重要顺序：先建立 BitmapImage 并使模板子图片 Visible，只有实际 Image 子元素 Loaded 后才设置 UriSource 触发解码；先完成的缓存结果保留为待赋值来源，卸载时清除。这样用事件条件替代旧版淡出等待所隐含的接入延迟，而不是直接在模板未连入树时请求解码。

清理：删除两个只匹配源码写法的 Node 测试文件和 GuideNavigationTest，删除其测试项目链接。保留真正执行生产代码的并发加载、周期信号停止、缓存异常释放、通知和下载进度测试。这些仍不能代替 WinUI 运行时图片显示与退出复测，本轮不以数量作为修复证明。删除均可从 Git 恢复。


## 仓库列表离线回退、短重试与最新初始化约束（上一轮，重试参数已被下节修正）

按用户明确要求保留 NavigationView 对首页 IsInitialized 的禁用绑定，Main/Home 仍 await 完整初始化尝试，不提前把网络/元数据/Git 依赖标记为完成。元数据消费者只等成功信号；失败的是单次初始化尝试，由 Startup 重试负责后续恢复，退出时 ShutdownToken 取消等待。没有以释放导航栏或跳过 Git 更新来掩盖启动问题。

- 30 秒失败冷却改为 2 秒，后台重试检查也缩短；保留并发合并、成功子项复用和非递归失败重排，不反复重建主页卡片。实际 Git 操作在活动状态/UI 发布后再次明确切到后台，并将进度 UI 更新限制为约每 100ms 一次；ConfigureAwait(false) 不保证同步完成的续体不在 UI 线程执行。元数据文件的同步缓存读取/解析也明确让出 UI 线程，初始化依赖顺序不变。
- RepositoryCatalogService 单例负责实际 `/git-repository/all` 请求与结果共享：每次 3 秒期限、失败后等待 2 秒再实际请求一次，关闭时取消。只有传输/HTTP 错误或超时允许使用用户给出的完整六条内置快照；业务/JSON 错误和主动取消不选择本地回退。不用不相关的 `/ip` 探测否决实际操作的恢复。
- 本地回退时发布离线警告，主窗口后台任务旁显示重连按钮。按钮真实请求同一端点，成功才替换内置快照、退出离线并隐藏；失败继续离线并提示。元数据和内容仓库按 name 从同一结果筛选，不分别重复请求列表。Git 网络失败不再删除可用缓存仓库；首次克隆留下无 HEAD 的半成品仍允许重新克隆。
- QuartzService 原 Dispose 同步等待 startupCompleted；而启动又等待依赖后续用户初始化的任务注册。现将调度器启动与这些任务注册分开，任务注册可取消；App.Exit 在消息循环仍活着时异步中断应用任务并关闭 Quartz，关闭等待有 5 秒界限，最终 Dispose 不再 GetAwaiter().GetResult() 阻塞已退出的 UI 线程。
- FlyoutOpenStateBinding 的挂起状态移到 ConditionalWeakTable 中的托管状态。延时线程和 finally 不再访问 DependencyObject.GetValue；UI 回写用弱引用、当前请求身份及退出/取消检查，并将异常交给被观察的 Task，退出时统一取消挂起切换。
- 背景播放器改为服务明确创建并拥有，MediaPlayerElement 仅借用，卸载先解绑再释放。Stop 不再向已关闭播放器写 Source=null；异步更新检查持有者身份，避免最小化/关闭后向旧播放器赋值。初始化 owner 与网络请求链接 ShutdownToken，晚到的完成结果不再无条件捕获关闭中的 UI 上下文。

验证：一次真实端点请求成功，retcode/code 均为 0，返回 6 条；因此没有把正常连接写死为离线。实际内置 JSON 解析核对为两个仓库各 3 个镜像。6 项检查直接运行生产 RepositoryCatalogRequest，覆盖首个成功、两次传输失败、第二次恢复、业务/JSON 错误、主动取消和截止时间取消；结果 `C:/Files/OpenNet/artifacts/hutao-offline-network-tests/repository-transport-policy.trx`。最终解决方案增量编译成功（0 错误、198 项含原有测试/依赖的警告），日志 `C:/Files/OpenNet/artifacts/hutao-offline-quartz-flyout-complete-20261017.log`；只读核对 DLL 包含最后的仓库可用性判断。未运行真实窗口/退出复现，不把网络策略检查当作 CoreMessaging 崩溃已完全解决的证明。

## 8 秒仓库期限、持续离线恢复与初始化完成通知

- 删除新增的请求间 2 秒等待和首页失败冷却。`RepositoryCatalogRequest` 每次真实请求限时 8 秒，传输失败可立即进行第二次尝试；主动取消、业务响应/JSON 错误仍不选离线快照。`AsyncLoadOperation` 只合并进行中请求、保留成功子项，失败可立即重试。
- 重试协调器首次标记失败即按原有 500ms 事件合并流程调度，不增加 2–3 秒冷却。仅后台检查保留 2 秒周期，不推迟初始化首轮或手动重连。重复 MarkPending 不递归发请求，同一个任务只允许一个重试运行；互相依赖的任务独立运行，避免等待元数据的用户任务挡住生成元数据的 Startup 重试。账号初始化另加共享串行入口，避免 Main/User 恢复同时重复请求同一账号。旧注册、旧状态版本的完成不会清掉新失败。
- 离线目录使用同一个协调器持续后台连接实际目录端点，手动/自动重连共享请求；恢复后替换目录、退出离线、隐藏按钮。按钮提供普通/液态玻璃两支，通过生成通知显示“离线模式 · 重新连接”或“离线模式 · 正在重连”，按钮本体随控件材质同步。后台失败不重复弹离线提示。
- 首页初始加载与重试都明确等待元数据成功信号；保留 NavigationView 的 IsInitialized 禁用绑定，并继续 await 公告/日历等必要请求结束。修正请求自身的取消/超时被当成页面卸载而不再重试的分支；重试成功也在页面所属 UI 线程发布初始化完成通知。基类按视图代次合并加载，旧代完成不能更新重建后的页面；Loaded 行为以命令身份去重，不会把新的 DataContext 命令误当成已经执行。

验证：网络策略和并发加载检查直接执行生产代码，包含允许请求超过 3 秒但在默认 8 秒期限取消、失败后无额外等待、业务/JSON 错误不离线、并发去重、成功子项复用和取消后的再次加载；结果 `C:/Files/OpenNet/artifacts/hutao-eight-second-tests/repository-recovery.trx`。工作区外的临时验证程序直接链接生产 ViewModel/NetworkRetryCoordinator，验证并发初始化同一个完成任务、旧页面代次不能写入新代、取消后重试恢复通知、元数据生产者不被消费者阻塞及重试事件去重。该程序适配/模拟 UI 与网络平台，只验证托管逻辑，不证明真实 WinUI/HTTP 行为；未加入仓库的 Test 项目。不把这些检查当成 WinUI 实际显示/导航/退出已通过的证据，仍需用户复测真实窗口。

最终解决方案增量编译通过，0 错误、182 项原有警告；日志 `C:/Files/OpenNet/artifacts/hutao-eight-second-recovery-final-20261017.log`。新增离线按钮的生成绑定实际订阅 IsOffline、CanReconnect 和 ConnectionStatus，日志无 WMC1506。初始化发布还验证了队列拒绝回调及回调不再执行时的取消释放；没有启动真实应用或修改依赖/SDK。

## 后续调整：首页就绪条件与 InfoBar 动画恢复

按用户最新明确要求，首页的必要初始化只等待元数据/Git 成功与本地仪表盘准备。公告、兑换码、日历在后台独立加载，不再等待全部网络请求才设置首页 IsInitialized；导航栏绑定本身仍保留。上一节“首页等待全部必要请求”的策略不再适用于这些可选内容。四类可选请求分开注册重试，不让一个未返回的请求阻止另一个失败请求的后续恢复。

`Announcement/List` 实际为带排除 ID 过滤条件的只读 POST；通用重试器出于写操作安全不会重试 POST，默认 HTTP 客户端又没有超时期限。此方法现在单独限定每次 8 秒、最多两次传输尝试，重新建立请求/正文并禁用内层重试，不扩展到上传、签到等写操作。请求取消与退出不重试，永久 HTTP 4xx/业务/JSON 错误不立即重放。响应正文直接按期限读取，避开通用反序列化器包装取消后不带期限重新读取错误正文的路径。

对照 Git 中 InfoBarView 原配置，恢复 200ms Cubic EaseOut 的面板形变、标题联动和每 50ms 从末条开始的逐条清空；补齐旧配置默认左上角锚点与源 20–40%、目标 10–30% 的透明度过渡时序。亚克力背景、卡片阴影、数量徽标、滚动布局、列表 AddDeleteThemeTransitions、自动展开以及两种材质的按钮均保留。动画改由元素拥有的 XAML Storyboard 承载，完成事件在 Stop 前解绑，卸载/退出停止逐条清空计时器；修复移除通知触发布局卸载的同步重入问题。清空针对当前快照，期间新来的通知不会被误删。

验证：工作区内隔离程序直接链接生产 HTTP 方法，通过只读 POST 503 后恢复、相同正文重新发送、主动取消不重试、400 不重试、正文停滞到 8 秒后取消并立即恢复的检查，未访问线上服务器。InfoBar 生产代码通过普通/玻璃分支的形变端点与分段透明度、完成/卸载解绑、50ms 清空及清空中卸载重入检查。另直接运行首页生产 LoadOverrideAsync 的方法体与基础 ViewModel，确认元数据未就绪时不启用、元数据就绪而可选请求仍挂起时能启用。平台/服务适配均为模拟，不替代真实 WinUI 显示验证；这些临时检查未加入仓库 Test 项目。

编译状态：较早的 InfoBar 初版增量编译成功，日志 `C:/Files/OpenNet/artifacts/hutao-infobar-animation-restored-20261017.log`。之后完整编译请求被执行审批服务的额度错误拦截，命令未执行；当前最新首页/公告方法及动画参数调整尚未重新完整编译，也未运行真实窗口，不能声称完整验证通过。

## 恢复原 TransitionHelper 与资源键名清理

用户明确要求使用之前的连接动画实现后，InfoBarView 已恢复原 `cw:TransitionHelper` 资源、200ms Cubic EaseOut 参数、Body/Header 的 ScaleX 配置及附加 Id；删除中间版本手写的 CompositeTransform/Storyboard 形变代码。点击仍进入代码入口，以传入挂载/退出取消令牌、观察动画 Task、强制刷新主题分支的配对元素，并在卸载时调用原 Helper 的 Stop/Reset。提前同步准备布局，避免 Helper 在挂起的 LayoutUpdated 初始化之后才开始动画；取消时停止等待，但继续观察底层迟到结果，旧代结果不能更新新一轮呈现。该保护并不等于证明原生退出异常的唯一来源或真实退出已经完全修复。

工作区隔离程序直接链接当前 InfoBarView 生产代码，适配 Toolkit/WinUI 平台，检查原 Helper 的 Source/Target 路由、反向/替换切换取消、卸载 Stop/Reset、迟到结果隔离、50ms 清空以及清空时布局卸载重入，均通过。XAML 和资源 XML 可解析；这不是原生动画的视觉验证或完整解决方案编译。

用户后续澄清只清理资源标识符里的 `Experimental`，保留界面的“实验性预览”。现在统一使用既有 `ViewPageSettingControlMaterialLiquidGlass` 键，其值为“液态玻璃（实验性预览）”，引导、设置和控件窗口已更新引用，新增的冗余 Experimental 后缀键已删除。未编辑其他语言资源。

## 点击交互也恢复 XAML 声明

进一步按用户要求恢复展开/收起按钮的原 `mxi:Interaction.Behaviors → EventTriggerBehavior(Click) → cw:StartTransitionAction`，普通与液态玻璃共四处，删除两个对应 code-behind Click 处理器。上一节由代码启动所有点击动画的中间实现已撤回。

后台只管理已有的集合驱动自动展开/空列表自动收起、首次加载状态、50ms 清空和卸载/退出 Stop/Reset。手动交互不再依赖后台 expanded 缓存，也不由自动动画的迟到完成强制覆盖显隐；自动完成先核对当前 Helper 的 Source/Target。检查已安装 DLL 的 Execute IL，原 StartTransitionAction 的确调用不带令牌的 StartAsync 并丢弃 Task，这个组件原行为保留，不声称手动 Action 获得了与托管自动动画完全相同的任务观察保护。

XML 检查通过四处原 Action 的层级、两个材质的正反向绑定以及无重复 Click 处理器。直接链接生产 InfoBarView 的托管隔离检查通过自动/手动方向替换、卸载停止、迟到结果、清空计时器与布局卸载重入；平台 Action/Helper 为适配模型，不代替真实 XAML Interactivity 执行、原生动画复测或完整解决方案编译。
