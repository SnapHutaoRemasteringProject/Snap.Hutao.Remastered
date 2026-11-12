# Native AOT 当前阻塞项

更新：2026-11-12。分支：`feat/aot-compatible`。当前只做 Debug。证据及用户决策见 [开发报告](aot-development-report.md)。

## 当前结论

完整 WinUI 应用已完成本机原生发布及交付检查，C# 脚本已移除。
**完整应用端到端 Native AOT 验收尚未完成**，下一步是实际启动和功能测试。

## 已解决

| 范围 | 本地验证 |
|---|---|
| JSON 公共入口、共享解析链及历史 AI 文件 | Windows 原生共享 JSON 程序通过；缺失契约明确失败；本轮关闭反射回归 16/16 |
| 静态数据库升级 | 恢复已使用但被删除的第 48 条迁移，48 个登记历史对照通过；额外覆盖旧背包 NOT NULL 变体和已应用 rc.2 历史，以及历史登记失败回滚 |
| EF 原生模型和查询 | 25 个真实实体、69 查询、Include、参数变化、JSON/Cookie、事务/级联及真实仓库原生运行通过 |
| WinUI 编译/资源发布 | 引用、PRI/XBF 生成和复制顺序、辅助载荷已接通；原生门禁能拒绝 R2R/CoreCLR 输出 |
| C# 脚本阻塞 | 经用户授权移除入口、窗口、上下文和测试命令；未做 CoreCLR 辅助进程；应用无 Roslyn/EF Design 运行时包 |
| 按名称排序 | 15 个描述改用 SortDescription<T> 保留属性元数据，原字段、方向与优先级保持 |
| 当前 AI 设置 | 生成契约、配置选择、枚举、凭据排除与严格数字兼容有回归；Enka 根类型已登记 |
| 当前 AI 会话存储 | 生成应用契约并保留 SDK 契约；旧格式双向一致、24 种内容、会话/重试/待审批恢复原生通过 |
| .NET 11 包 | 应用与宿主共享 11.*；实际 EF 与生成产物版本不一致时构建拒绝，负例已通过；当前解析 11.0.0 |
| 应用泛型成员保留 | 数据库 UI 包装/扩展和对话框工厂传递完整契约；最终 Debug 原生发布无 IL2091 |

## P0：原生应用交互验收

物理 Esc 已停止界面自动操作。普通/R2R 版只到达独立数据目录的首次启动向导，**没有将它计作原生运行通过**。
后续恢复界面操作前须取得恢复授权，不通过命令行操作窗口绕过停止。

恢复后，使用最新原生目录与新的 SNAP_HUTAO_PROFILE_ROOT：

1. 启动、首次向导、主窗口、导航、退出、重启；核对进程模块没有 CoreCLR。
2. 在独立目录复制旧数据库/设置升级，不碰个人数据。
3. 成就、抽卡导入/导出、背包、养成、实时便笺和战斗记录；对照 UI 与实际持久化结果。
4. 用户/验证码、WebView2、通知、AI 流式对话。认证/CAPTCHA 由用户完成。

## P1：JSON 与第三方运行路径

- 网络响应、元数据根类型、实际 AI 工具返回类型和远端流式/会话恢复仍须验证；会话文件读写通过不代替这些路径。
- 本轮原生诊断给出的应用源码入口（尚未修复）：AccountInsightsHistoryService.CreateAnonymousJson 使用 JsonArray.Add<JsonObject>，应选择 JsonNode 重载并对照匿名导出；EnumBox.ResolveTypeInfo 仍访问 JsonSerializerOptions.Default，须保持既有调用方契约并移除原生反射依赖。
- 六个 [ObservableObject] 混入类型触发 MVVMTK0050：ReliquaryComposedSubProperty、Team、CultivateEntryView、StatisticsItem、SummaryItem、AnnouncementListWrapper；逐个检查既有基类，改为可由 CsWinRT 生成互操作代码的继承结构并验收相关绑定。
- 测试页两处 EnumValuesExtension 已改为具体枚举 x:Bind，最终原生发布已无此诊断；EventTriggerBehavior 动态事件接线仍待处理。
- DevWinUI CompositionExpressionEngine 等类型扫描诊断仍在，须验证真实路径或修复源实现。
- EF 构造/映射、Spatialite/DependencyModel 等实验性诊断仍有归属，不代表所有 EF 用法均安全。
- 不全局压警告，不用全表内存筛选、替身实体或删减未授权功能作为解决办法。

## P2：交付与维护

- MSIX/安装/卸载、没有对应 .NET 运行时的机器、资源交付和主要功能尚未验收；本机安装有 SDK。
- 启动时间、内存等需同场景对照，不能凭编译成功给加速百分比。
- 新迁移的创作流程需先验证原快照和生产模型的基线；不要用 Schema 工具的最小上下文生成新实体迁移。
- CsWinRT 3 是独立迁移项，当前继续 2.3.1。八包、WinMD 和具体适配见 [评估](cswinrt3-assessment.md)。

## 历史证据

[历史扫描与实验](https://github.com/SnapHutaoRemasteringProject/Snap.Hutao.Remastered/blob/32976c2f4b7ef2f8053b34131ffd7fdbae5b5722/docs/aot-blockers.md) 保留 194 条基线及 JSON 解析器早期记录，仅用于追溯。
