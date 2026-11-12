# Native AOT 当前状态

## 用户旧库启动失败已定位并修复

旧库有 48 条历史，AOT 登记表漏了已使用过的 `20261022144006_MakeBackpackEquippedAvatarIdNullable`，所以被拒绝。这不是 rc.3/正式版号比较的问题。
已从原 Git 提交恢复迁移与快照，生成当前 48 条清单。用户实际全部迁移 ID 已只读核对匹配；未修改个人数据库。

`__EFMigrationsHistory` 仍保留。执行迁移 SQL和写入历史在同一事务，失败一起回滚；旧 MigrationId、ProductVersion 不重写。已有第 48 条的库不重复执行；缺少它的旧库修复背包装备列可空性并保留物品、档案、索引和外键。

## 工程与依赖

- 两个数据库开发项目位于 `src/Snap.Hutao.Remastered` 并加入 slnx；独立 DatabaseAotSmoke / JsonAotSmoke 项目已按用户要求移除。
- 所有 csproj 使用根 `Directory.Packages.props` 中央管理包版本；保留原版本值及 .NET/EF 的 `11.*` 策略，交由用户审核。实际 EF 与生成产物不一致时构建报错，要求重新生成。
- SQL 仍由原 EF 迁移生成；版本元数据集中一处，避免每个 SQL 随补丁号重复变化。原迁移快照不改写。
- Fixtures 是五份生成的 JSON 回归样本，不是用户数据或迁移脚本。

用途、命令、版本规则、EF 官方 AOT 迁移取舍见 [AOT 开发说明](../src/Snap.Hutao.Remastered/AOT.md)。

## 验收

48 个已登记 EF 历史库对照通过；旧 NOT NULL/default-0、已 nullable、已执行第 48 条的 rc.2 三种变体，以及登记失败回滚、原生升级和生产 25 实体/69 查询回归通过。
移除 Smoke 前的六项目 Debug 构建通过。最终完整 Debug Native AOT 发布及交付检查通过，单元测试 274/274，两个独立原生验收程序通过。修复版目录为 `artifacts/aot/2026-11-12/app-debug-native`，日志在 `artifacts/aot/2026-11-12`，不提交产物。

## 仍需完成

原生界面、导航、账户、网络/工具 JSON、部分 WinRT 绑定和动态事件路径、安装升级尚未完整验收。具体源码入口见 [剩余问题](aot-blockers.md)。
覆盖的是当前登记的 48 条及上述真实变体，不代表 Git 历史中所有删除/改写分支。历史证据见 CODE_REVIEW.md。

本轮中央包管理和 Smoke 移除仅做静态结构检查，按用户要求未还原、构建或升级包；此前原生验收结果不能算作本轮配置已验收。

## 当前启动崩溃（29424）

CDB 确认 0xC000027B，发生于 Loading/Measure 事件链。转储有 4204 个捕获内存范围，但不包含异常参数指向的 0x87FA8F7670；五个 stowed exception 的原始消息/回溯无法读取，根因尚未确定，不能把 WinUI FailFast 当成根因。需要含堆转储。
已在 XAML 未处理异常入口增加本地原始错误记录（个人 LocalAppData/SnapHutaoRemastered/CrashLogs，独立 profile 则在该 profile 下），先于遥测/错误窗口执行，不改变异常处理策略。尚未构建或验证，未声称修复崩溃。Fixtures 及相关生成/文件基准检查已移除。

新 reattach 完整堆转储已恢复异常链，并用产物本地 PDB 解析：UserView.OverseaThirdPartyKinds 和 AnnouncementPage.RedeemCodes 的生成绑定在 ItemsControl.set_ItemsSource 中抛出 0x80070057。两者均将 ImmutableArray 直接跨 WinRT 边界传递。源码已改为普通 List；公告数据保留 ImmutableArray，新增列表适配属性及属性变更通知，并同步处理同页的活动列表绑定。参考上游 https://github.com/microsoft/CsWinRT/issues/2507 。未还原/构建/启动验证，不能写成崩溃已验收修复。

公告页后续在 BindingProxy.DataContext 的 SetValue 中报 E_INVALIDARG：源码仅有项目自己的 BindableCustomPropertyProvider，IBindableCustomPropertyImplementation 由该生成器输出，CsWinRT 同轮生成时不可见。已在 AnnouncementViewModel 输入源码显式声明该投影接口，并用 GeneratedWinRTExposedType 要求生成 AOT vtable；保留原属性提供器和 BindingProxy 的 DP/通知语义。该修正未还原、构建或运行，需原生发布复验；尚不能宣布该异常已解决。

## 当前绑定修复阶段

按 CsWinRT aot-trimming.md 检查源类型、partial 基类链、具体集合和生成 vtable。上次公告 VM 专用声明经用户复验未解决，已撤回；共用 BindingProxy 改用 FrameworkElement 自带 DataContext，所有资源代理共用此路径。
Debug --no-restore、PublishAot=true 的编译检查通过（967 warning / 0 error），不执行原生发布或启动。CsWinRT1028、CsWinRT1032、MVVMTK0050、CS8604 在本轮日志中为 0；WMC1510 独立诊断从本阶段首轮 612 降至 530，仍未解决全部绑定。测试 274/274，当前生产数据宿主托管运行 25 实体/69 查询通过；不把这些记为 Native UI 验收。
修复还包括明确集合类型、可观察基类、JSON DOM 的 JsonNode 重载和 EnumBox 的显式元数据契约。实验 API 通知、CS0436 投影冲突、外部包 AOT 警告仍保留。日志 artifacts/aot/2026-11-12/binding-audit。

## DataContext 异常的进一步证据

三份本地 CrashLogs（UTC 10:52、11:08、12:51）的 Message 均为 Scale/GetElementVisual 互斥错误，即使托管栈停在 DataContext setter。MaterialAppearance.ResetMotion 同时调用 GetElementVisual、Visual.Scale 和 UIElement.Scale，符合平台禁止混用的条件。已改为只使用 UIElement facade 动画路径，停止相同目标动画后设置 Scale/Translation；不捕获吞掉业务异常，不删材质切换。来源 https://github.com/microsoft/microsoft-ui-xaml/issues/4904 。这推翻了此前仅凭 DataContext 栈归因代理或 vtable 的结论；仍需原生重测确认是否覆盖全部异常。
SettingsModelConfigurationViewModel 的八个命令改为源码声明，官方绑定生成器可见；两个异步命令保持 CancellationToken、CanExecute 及忙状态通知。Debug --no-restore/AOT 分析编译通过（965 warning / 0 error），MVVMTK0046 未再出现。没有还原依赖、发布 Release 或运行界面。

## 首次异常确认后的纠正

用户提供首次异常：0x80070005 在 MaterialAppearance.ResetMotion 的 UIElement.Scale 直接发生，发生阶段是 Loaded。已读取 NuGet 1.3.1 对应仓库标签 v1.3.1 的 PointerMotionHelper.hpp 与 MotionAnimation.hpp：实际缩放走 GetElementVisual(...).Scale；平移按 SetIsTranslationEnabled + Visual.StopAnimation + UIElement.Translation 处理。此前改为 facade-only 的 4905f473f 不符合该库实现，现已纠正为与版本标签一致的路径，移除 UIElement.Scale。不是通过捕获异常或禁用动作绕过。
来源：https://github.com/Millennium-Science-Technology-R-D-Inst/WinUI.Composition.Hlsl/blob/v1.3.1/src/WinUI.LiquidGlass/include/MotionAnimation.hpp 。该证据确认 Scale 缺陷，但不能将另一个发生在 Loading 的 0x80070057 DataContext 异常自动认作同一问题；需要分别复验。
