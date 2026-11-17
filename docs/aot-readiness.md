# 构建、原生发布与交付说明

更新：2026-11-14。当前按用户要求只构建/发布 Debug。现状与证据见 [开发报告](aot-development-report.md)，剩余工作见 [当前阻塞项](aot-blockers.md)。

## 构建模式

默认 Debug/Release 都启用 ReadyToRun，使用 .NET 运行时，不默认裁剪或 Native AOT。
Native AOT 是显式发布模式；Release 是配置，publish 是交付阶段，不能混为一谈。
应用只声明 x64，不增加 AnyCPU 映射，不固定 SDK feature band。

```powershell
dotnet restore src/Snap.Hutao.Remastered/Snap.Hutao.Remastered.slnx
dotnet build src/Snap.Hutao.Remastered/Snap.Hutao.Remastered.slnx -c Debug -p:Platform=x64
dotnet test src/Snap.Hutao.Remastered/Snap.Hutao.Remastered.Test -c Debug --logger trx
```

同目录旧实例会锁住普通 bin，先由用户关闭或选独立编译输出；独立 OutDir 编译检查不等于已经生成完整可部署布局。
测试项目源码链接生产文件，没有引用整个 WinUI 应用，测试 PASS 不覆盖所有页面。

## Native AOT 发布

`dotnet build -p:PublishAot=true` 只更新托管编译输入和 XAML，不会替代原生发布。`bin/.../native` 中可能保留旧 EXE/PDB；源码和生成代码已经修复，并不表示该文件已经更新。请完整 publish 到新目录，并从该目录使用匹配的 EXE、PRI/XBF 和资源。

在 Windows、.NET 11 SDK、Windows SDK、VC++ 工具和 WinUI 构建工具齐备的环境执行完整发布：

```powershell
$nativeOutput = Join-Path $PWD ('artifacts/aot/publish-' + (Get-Date -Format yyyyMMdd-HHmmss))
dotnet restore src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Snap.Hutao.Remastered.csproj -r win-x64 -p:PublishAot=true -p:PublishReadyToRun=false
dotnet publish src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Snap.Hutao.Remastered.csproj -c Debug -p:Platform=x64 -r win-x64 -p:PublishAot=true -p:PublishReadyToRun=false -p:SelfContained=true -p:JsonSerializerIsReflectionEnabledByDefault=false "-p:PublishDir=$nativeOutput/"
./scripts/Test-NativePublish.ps1 -PublishDirectory $nativeOutput
```

仅先还原普通解决方案，再带 --no-restore 发布 AOT，可能没有导入原生编译器，无法产生原生 EXE。以上 AOT 参数必须同时用于还原；之后才可在相同配置下加 --no-restore。项目现在会在 PrepareForPublish 前明确拒绝未激活的原生编译器。当前用户要求不自动还原时，须由用户执行该定向还原或明确授权。

原生和普通构建的隐式包不同，不要并行还原同一项目、覆盖同一个 obj/project.assets.json 后再跳过还原。
此前 rc.3 的 ILC 目标在 --no-build 下仍调用 Compile，曾遇到 WinUI 生成项未初始化；当前继续使用完整发布，不依赖该组合。
使用新目录，避免旧 R2R/CoreCLR 文件混入交付；不跳过检查来容忍旧文件。

## 构建目标的职责

| 文件 | 职责 |
|---|---|
| Build/PatchedXamlCompiler.targets | 在私有副本修正 WinUI 编译器资源名；副本按配置/TFM/RID 隔离，避免执行中的文件互锁 |
| Build/ReadyToRun.targets | 默认 R2R 输出、最终 PE 检查与必需载荷检查 |
| Build/AotPublish.targets | AOT 引用顺序、ILC 前 PRI/XBF 准备、正确时间加入发布清单、部署辅助程序补齐、最终原生检查 |
| Build/DatabaseSchema.targets | 历史迁移源指纹及 SQL 资源接线 |
| Build/BundledImages.targets | 可选不可变图片快照 |

AOT 生成/复制使用 SDK 的真实流水线，不手补生成结果。
XBF 清单来自当前 XAML 输入，不扫描旧 publish/AppX 文件，因此已删除脚本窗口的旧缓存不会重新打包。
发布门禁检查 x64 PE、CLR 数据目录为空、没有主托管 DLL/根目录 CoreCLR 宿主，及 PRI/XBF、Assets/Resource 和原生载荷。
单独的 apphost 本来也可能没有 CLR 头，不能只看这一项就认定 Native AOT。
Cake/MSIX/安装器必须消费同一轮完整发布树，不能改用旧 AppX 副本；本轮尚未验收打包安装。

## JSON 与数据库维护

JsonSerialize 从调用方同一份 options/生成解析链取得 JsonTypeInfo；未知类型或解析器异常明确失败，不静默反射重试。
JsonOptions.Default/Create 在普通运行时与原生运行时均使用生成契约，让缺失契约在普通调试中也能暴露。
显式托管兼容入口 CreateReflection 带有裁剪/动态代码要求标记，且仍要求 IsReflectionEnabledByDefault 开启；原生发布关闭该开关。
新网络响应、元数据、AI 会话和工具类型必须补齐根与嵌套闭包，并验证旧格式跨读写。
当前会话通过 AgentConversationJsonOptions 登记应用契约；SDK 类型仍由 SDK 自身契约处理，以保留其内部审批状态字段。旧文件双向读写、24 种可观察内容、重试与待审批恢复均有验证；这不代替实际模型服务/工具执行验收。

2026-11-15 最新源码检查：完整 Debug 重编译通过，0 错误、84 个警告；绑定警告 WMC1510 剩 7 个。
构建记录为 `artifacts/aot-typed-binding-contracts.log`，分类见下表。
使用 `--no-restore`、`PublishAot=true`、`AotAnalysis=true`，未关闭裁剪或 AOT 分析器。
这是编译和分析检查，没有执行新的原生发布或启动应用。关闭默认 JSON 反射的相关回归 44/44；
旧 AI 会话格式对照单独在托管反射模式验证，3/3 通过。
本轮选择项/控件模型的属性分派、WinRT 转换元数据、集合赋值、页面 VM 属性提供器声明及 HTTP/元数据契约检查均按最终程序集复核。
GachaArchive.Uid 的异常来自 DisplayMemberPath：即使 ItemsSource 使用 x:Bind，显示属性仍通过 ICustomPropertyProvider 查找。
三个存档类型已补齐对应属性的生成访问器，且修复前程序集会被新增的分派检查拒绝。
手写源码中的转换位置使用 CsWinRT 官方 DynamicWindowsRuntimeCast 标注；编译产物检查确认 71 个目标类型具有实际 DynamicDependency 根。
泛型 NameValue<T>、LevelParameters<TLevel,TParam> 的 UI 部分显式实现 IBindableCustomPropertyImplementation，
从而不依赖不支持泛型声明的属性生成器；语言选项也保留维护来源等派生属性的分派。
20 处已确认的 NameValue 选择器改用应用范围的共享编译模板，保持原选项实例、类型化值、选择和相等性逻辑。
其余已确认的启动方案、账户、养成计划、评分预设、楼层、世界等级与技能等级显示字段均有实际编译出的属性访问器。
45 个存在转换警告的依赖属性改为显式源码声明，getter 直接标注需要保留的类型；
注册字段、默认值、回调和访问器语句与迁移前生成定义作逐 token 对照，45 项均一致。未修改生成器包或生成文件。
背景图片像素访问改为已知 IMemoryBufferByteAccess IID 的 QueryInterface 和原生槽位调用，避免传统 ComImport RCW；
4 项缓冲区回归覆盖内存读写、HRESULT 失败、空缓冲区与非法长度。文件扩展名忽略大小写，
本地文件夹变化触发刷新，过渡完成后才提交上一张图片状态，避免取消后单图不再重试；非退出时的 COM 呈现失败会记录错误。
养成编辑接口现在声明既有实现提供的属性通知及突破状态写入契约，编译绑定仍跟踪等级变更和双向编辑。
统计卡片在 DataContext 变化时刷新私有类型化入口；下载窗口使用函数绑定，避开 Window 转换器查找根被当作 FrameworkElement 的生成代码错误。
AutoSuggestTokenBox 的 ControlTemplate 上下文菜单命令改为源码声明的命令属性，并用限定范围的 `WinRT.GeneratedBindableCustomProperty` 暴露；生成结果包含两个命令访问器。控件不能直接套用仓库属性提供器：它同时遍历继承的 Control 属性，其中 nullable 值类型 setter 会产生 `Unsafe.Unbox<bool?>`（CS0453）。
数据库生成宿主同样通过 Debug --no-restore 编译，仅余 NU1507；共享 DTO 的 WinRT 标注移到应用 UI 部分，未增加宿主依赖。
当前完整重编译的实际警告如下；没有新增全局或局部警告屏蔽：

| 类别 | 数量 | 后续范围 |
|---|---:|---|
| WMC1510 | 7 | 原生菜单 TemplateSettings 宽度 1；ItemsPanel 的祖先属性路径 3；AutoSuggest 通用 DisplayMemberPath/TextMemberPath 3 |
| CsWinRT1034 / CsWinRT1035 | 0 | 71 个手写标注类型的实际裁剪根已复核 |
| CS8305 | 75 | SDK 实验性图表、窗口布局和控件 API |
| WMC1501 | 1 | SDK 实验性 XamlChartsResources |
| IDE0005 | 0 | 数据库重复导入已清理，官方查询生成与校验通过 |
| NU1507 | 1 | 保留原有包源策略 |

7 个绑定警告来自通用控件的运行时路径与原生属性通知；AutoSuggest 当前六个调用页面均提供编译的 SuggestedItemTemplate，
未删掉控件对 DisplayMemberPath/TextMemberPath 的通用支持。ItemsPanelTemplate 也未为消除警告改换视觉结构。
本次检查实际覆盖 24 类属性分派，包括五个页面/用户视图的 ViewModel 和 CultivateItemView.IsToday；
149 个生成 XAML 文件没有直接向 WinRT setter 传递 ImmutableArray；HTTP/元数据契约及 VM 声明检查通过。
本次完整重编译未报告 IL2026/IL3050，但这不证明上游要求已解决：此前记录的 EF Core 和 EventTriggerBehavior 分析要求，
以及第三方程序集、EF 生成枚举的原生链接诊断，仍须在新的原生发布中复核。没有关闭分析器，也没有为这些路径新增屏蔽。
修复依据为 [CsWinRT 绑定说明](https://github.com/microsoft/CsWinRT/blob/2.3.1.260716.1/docs/aot-trimming.md#icustompropertyprovider-type-support-on-aot-winui-binding)
和当前版本的 [转换标注生成器](https://github.com/microsoft/CsWinRT/blob/2.3.1.260716.1/src/Authoring/WinRT.SourceGenerator/DynamicWindowsRuntimeCastGenerator.cs)。
泛型属性分派使用当前版本公开的 [ICustomPropertyProvider 实现契约](https://github.com/microsoft/CsWinRT/blob/2.3.1.260716.1/src/WinRT.Runtime/Projections/ICustomPropertyProvider.net5.cs)，没有启用反射回退。

NuGet.Config 保持原配置；本轮没有执行依赖还原或修改包版本。NU1507 的包源策略警告暂不处理。

13c608180 是应用仓库的 AutoSuggestTokenBox 命令访问修复，不是生成器版本提交。
已核对 NuGet 官方索引、应用中央版本与实际 assets，SourceGeneration 均为最新已发布版 1.3.17；
本地生成器仓库从 1.3.16 快进到 9d2d256（1.3.17），该版本只修正 DP 标识符公开性，尚不包含 DP getter 的裁剪标注改造。
当前两个显式命令确已进入编译后的属性分派，先前程序集会被回归检查拒绝；不能把报告 Build=aae23dca9 当作已验证 13c608180 的运行结果。
原 obj 程序集被用户侧映射占用，本轮使用 obj/aot-menu-check 和 artifacts/menu-check-bin 独立验证，没有终止用户进程。
托盘菜单内容由 UserControl 承载九处编译绑定，Flyout 保留呈现和关闭生命周期；另有七处抽卡卡片可见性改用编译绑定。
这些是编译/契约验证，没有重新发布或启动原生程序。

2026-11-15 的模板实现保留原 ResourceDictionary 和模板视觉树。AutoSortList、ItemFlyoutContent、
AutoSuggestTokenBox/Item、ItemIcon、MaterialNative、VerticalCard 仅增加字典的 x:Class/InitializeComponent，
并在合并字典中实例化相应类型；模板绑定由生成的 IDataTemplateComponent.ProcessBindings 驱动，
没有给 ResourceDictionary 添加 Loading 生命周期，也没有重新抽取 UserControl。
页面模板通过带 x:DataType 的 ElementName Binding 直接引用页面公开的强类型 ViewModel；五个视图的 BindingProxy 全部移除，原代理类也已删除。
ViewModel 暴露既有作用域实例，DataContext 更换时发出属性通知，保留服务容器的创建、取消与释放流程；不在 XAML 中另建 VM。
五个视图使用限定为 ViewModel 属性的 GeneratedBindableCustomProperty，并检查最终程序集中的实际分派。
本机编译器对模板中的 x:Bind 根页面引用生成了错误的 WeakReference 参数（CS1503）；该路径没有修改生成文件，改为上述有类型声明及静态分派的 Binding。
ItemsRepeater 的 ElementName 跨模板问题 [#560](https://github.com/microsoft/microsoft-ui-xaml/issues/560) 已标记在 WinAppSDK 1.6 修复；本项目使用更新版本。
15 处 DisplayMemberPath 改用 11 个共享强类型文本模板，仍显示原 Name/DisplayName/Uid/WorldLevel/Floor，
保留原选项对象、类型化值、选择和相等性行为；会通知的账户名称继续 OneWay，快照选项使用 OneTime。
AutoSuggest 编辑内容使用 PreTokenStringContainer.Text 的编译双向绑定，菜单命令与删除参数直接来自模板父控件。
INamedValue 增加只读的对象值接口，NameValue<T> 显式实现它，不改变公共泛型 Value 或 JSON 字段。
四种带输出的 AI 工具结果共用只读通知接口，原 JSON 契约保持不变。
补齐 CultivateItemView.IsToday 的静态属性分派；修复 VerticalCard 不存在的 ElementName 宽度来源，
以及中文引导页引用不存在的协议属性、导致真正的下游协议确认未能更新的问题。
评分颜色转换器在明暗主题中原本完全相同，合并成一份后使用强类型绑定；其他主题相关配置保留。
托盘菜单 TemplateSettings 宽度仍用原生运行时绑定，避免 OneTime 丢失后续宽度更新。
字典实现遵循 [官方资源字典编译绑定说明](https://learn.microsoft.com/en-us/windows/apps/develop/data-binding/data-binding-in-depth#resource-dictionaries-with-xbind)。
这些变更没有改动 NuGet.Config、依赖版本、源生成器包或还原依赖；数据库查询产物由官方生成工具更新。

数据库运行时升级使用版本化 SQL；生产查询具体化并由官方工具预生成。
统一入口为 Generate-DatabaseAot.ps1，维护及回滚/边界覆盖见 [数据库实现](database-aot.md)。
不要直接对含 WinUI XAML 的主项目重走旧 Workspace 查询生成路线，设计/Workspace 依赖保留在独立工具。

## 独立数据目录与启动

非打包应用接受进程环境变量 SNAP_HUTAO_PROFILE_ROOT，必须是绝对路径。
设置、诊断设置、数据库、缓存及基于 DataDirectory 的业务文件进入该目录。
显式 profile 不读取旧设置中的个人数据目录/待删除目录；单实例锁与私有 IPC 按目录隔离，也不替换普通安装的 URI 注册。
它不会自动隔离游戏、外部辅助进程或全部系统集成。MSIX 与默认启动路径保持原行为。

```powershell
./scripts/Test-NativePublish.ps1 -PublishDirectory <发布目录> -Launch -ProfileRoot <独立目录绝对路径>
```

-Launch 启动完整应用，并把 stdout/stderr 写入 profile；它不自动证明导航、账户、数据库和主要功能通过。
用户物理 Esc 已停止本轮界面操作，未恢复前不自动启动/操控新的测试窗口。
认证、验证码和真实账户步骤由用户完成，不拿个人数据做破坏性验证。

## 当前产品范围

插件/Harmony 已在此前分支移除。用户现已授权移除 C# 脚本调试功能：菜单、窗口、上下文、执行器与测试入口均删除。
不加入 CoreCLR 脚本进程；应用无 Roslyn 脚本/Workspace/EF Design 运行时依赖，构建期分析器和数据库设计工具仍保留。
CsWinRT 维持 2.3.1；3.0 的资产问题与包适配见 [独立评估](cswinrt3-assessment.md)，不是本次 Native AOT 的前置条件。

## 2026-11-17：CI 同时交付 R2R 与 Native AOT

Alpha、Canary、Publish 和 Publish (SignPath) 的打包 job 均使用两个独立 runner 的矩阵，关闭 fail-fast，
以免 AOT 失败取消正常 R2R 产物。Alpha 的原 Debug/Release 测试任务保留。

| 模式 | Cake 参数 | MSBuild 属性 | 发布目录 |
|---|---|---|---|
| 非 AOT / R2R | `--publishAot=false`（本地默认） | PublishAot=false, PublishTrimmed=false, PublishReadyToRun=true | `bin/x64/Release/<TFM>/win-x64/publish` |
| Native AOT | `--publishAot=true` | PublishAot=true, PublishTrimmed=true, PublishReadyToRun=false | `bin/x64/Release/<TFM>/win-x64/publish-aot` |

四个 Cake 入口共享 build-runtime.cake，分别把同一模式传给还原与发布；本地仍需自行准备依赖和签名配置。
这保留了将来在非 AOT 构建恢复插件功能的构建路径，并不表示当前分支已恢复插件系统。

保留原有非 AOT MSIX、Setup、缓存图片 Setup、证书以及 SignPath 中间工件；删除独立 Store-upload 工件的生成和上传。
AOT 的工件名追加 `-AOT`，实际 MSIX/Setup 文件名也带后缀，例如 `Snap.Hutao.Remastered-<version>-AOT.msix`、
`Snap.Hutao.Remastered-<version>-AOT-Setup.exe` 和 `...-AOT-Setup-cache.exe`。Alpha/Canary 的 MSIX 继续带原频道前缀。
证书策略与 MSIX Publisher 校验保留；未签名 MSIX 仍不可旁加载。

打包清理保留 AOT 的 App.xbf，普通 R2R 清理策略不变。Test-PackageRuntime.ps1 在打包前检查：

- Native AOT：复用 Test-NativePublish.ps1，检查原生 x64 PE、无 CLR 头、无主托管 DLL/三个 CoreCLR 主机文件，以及 PRI/XBF/资源/运行时附属文件完整性。
- R2R：检查主托管 DLL 的 CLR 头及 ReadyToRun 签名，以及 CoreCLR/主应用/PRI 文件存在，避免把 native apphost 当成 AOT 证据。

SignPath 在重新解压签名 ZIP 后再次验证模式。Canary 的 prepare job 只合并一次，把确切提交通过短期保留的 git bundle
共享给两个构建任务，两者校验同一 source-sha。Alpha/Canary 的安装器复用各自 MSIX 的版本号。
R2 默认安装包上传只允许 R2R job 执行，AOT 不覆盖现有自动更新下载地址。

本轮验证：YAML 矩阵/步骤引用/旧工件名保留检查、25 个内联 PowerShell 块和门禁脚本语法检查、7 组运行时门禁接受/拒绝样本，
以及现有本地 Native AOT 发布目录的只读检查。未编译应用或 Cake、未还原、未运行签名/打包/上传、未触发 GitHub Actions；
远程 CI、签名服务对新增 AOT 文件名的接受，以及安装运行效果仍待实际构建验证。

## 2026-11-16：新发布警告与事件订阅

用户提供的原生发布日志共 119 条：CS8305 75、WMC1510 7、WMC1501 1、NU1507 1，
IL2026 7、IL3050 13、IL2104 6、IL3053 5、IL3002 4。编译与原生分析阶段的重复诊断保留原口径，
不能与此前仅托管编译的 84 条直接相减。

本轮将 13 个视图中的 32 个 EventTriggerBehavior 替换成限定 UI 事件的 StaticEventTriggerBehavior。
原行为按事件名反射订阅，SDK 明确标为不支持裁剪；新实现直接订阅 Click、QuerySubmitted、Tapped、
PointerEntered、PointerExited、PointerPressed，包含 SettingsCard 和 LiquidGlassSearchBox 的具体事件。
保留 Actions 的依赖属性集合、原事件参数，以及 Detach/更换事件名时的解绑；没有 SourceObject 或反射回退。
页面上下文、命令参数、模板结构和动作集合均未迁移。没有改依赖、包源或手写生成文件。

这些是源码修改与 XML/事件覆盖检查，未编译或运行。下一次构建应复核 EventTriggerBehavior 的应用激活器及
静态初始化诊断是否消失；当前没有新的警告总数。EF、Anthropic、DevWinUI 等原生分析诊断仍需继续处理。
日志中十条枚举动态代码警告对应的位置，当前 EF 生成源码已无直接 Enum.GetValues(Type)，
需要结合下一次实际编译输入/内联调用定位，不能据日志行号直接改生成文件。

## 2026-11-16：最新 XAML 编译阻塞与警告口径

最新用户日志在 XAML 编译阶段失败：1 条 WMC1110、31 条构建警告，另有还原阶段重复报告的 1 条 NU1507。
31 条分为 CS8305 22、WMC1510 7、WMC1501 1、NU1507 1；该次没有进入完整原生分析，不能据此推断最终 ILC 警告数。

WMC1110 是本分支上次修改的语法错误：x:Bind 函数的 Boolean 实参须写 `x:False`，裸 `false` 会被视为页面属性路径。
已按 [WinUI 函数绑定文档](https://learn.microsoft.com/en-us/windows/apps/develop/data-binding/function-bindings)修正，保留 ItemsSourceHelper 在 WinRT setter 前转换迭代器的修复。

七条 WMC1510 的当前处理（替代上次局部抑制方案）：

- 菜单模板的 TemplateSettings.KeyboardAcceleratorTextMinWidth 保留编译绑定。
- 删除全部 `x:SuppressXamlTrimWarnings`。用户实测该指令使当前 XBF 编译器报 WMC0612/0x09C4，[WinUI #12708](https://github.com/microsoft/microsoft-ui-xaml/issues/12708)记录了相同现象；该 issue 尚无维护者确认的修复，不能再以诊断文案为依据加入此属性。
- SegmentedOverride 补齐 `x:Class`、调用 InitializeComponent 的 code-behind，并在 App.xaml 原位置实例化。`ControlTemplate TargetType=Segmented` 中以 OneWay x:Bind 把 HorizontalAlignment 传给 ItemsPresenter；其 ItemsPanelTemplate 内的 EqualPanel 用原生 TemplateBinding 读取该值。
- AutoSuggestTokenBox 在其强类型 ControlTemplate 中以 OneWay x:Bind 把 TokenSpacing 写到 ItemsPresenter 的附加 DP；新 AutoSuggestTokenPanelHelper 使用现有 DependencyProperty<double> 源生成器（IsAttached=true、TargetType=ItemsPresenter）。WrapPanel 通过 TemplateBinding 读取两个间距值。ItemsPresenter 是 sealed 类型，没有派生新控件。
- 两处布局模板均不再查询 Ancestor。局部 XAML 编译器源码只为 ControlTemplate/DataTemplate 建立专用强类型绑定源，不能把 ItemsPanelTemplate 内的 Binding 直接替换为 x:Bind 再假定字典的 x:Class 能补足数据源。原生 ItemsPresenter 加载面板时将自身作为 templated parent，因而这里使用 TemplateBinding。
- 建议框的 DisplayMemberPath/TextMemberPath 字符串转发保留；相应位置本来已经使用 x:Bind/TemplateBinding。绑定字符串并不等于编译字符串所指的成员访问，这三条原有 WMC1510 可能仍存在，不以删除通用功能或抑制来隐藏。

仍保留的其他诊断是 22 条 SDK 实验 API CS8305、1 条实验图表资源 WMC1501，以及 1 条多包源集中版本管理 NU1507。
没有全局 NoWarn，未移除图表/窗口位置功能，未更改 NuGet.Config。
本轮只做源码/XML/差异检查，尚无修正后的编译或 Native AOT 发布警告总数。

## 验证口径

完整原生发布、生产数据库原生测试和最新普通构建已通过。
2026-11-11 的 Debug 全量测试 274/274、关闭 JSON 反射回归 16/16；细节记录在 CODE_REVIEW.md。

未验证：原生 GUI 启动/导航/退出、完整网络/AI/COM 路径、真实无对应运行时机器、MSIX 安装/卸载及性能。
该历史构建记录的编译警告为 84 条；最新用户失败构建口径见上节，不与原生链接诊断混算，也不写“全部原生功能完成”。
历史 JSON/扫描材料见 [归档](https://github.com/SnapHutaoRemasteringProject/Snap.Hutao.Remastered/blob/32976c2f4b7ef2f8053b34131ffd7fdbae5b5722/docs/aot-blockers.md)；R2R 旧验证保留于 CODE_REVIEW.md。
