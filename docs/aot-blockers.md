# Native AOT 当前阻塞项

更新：2026-11-10。分支：`feat/aot-compatible`。证据、dev 差异及用户决策见 [开发报告](aot-development-report.md)。

## 当前结论

完整 WinUI 应用已完成本机原生发布及交付检查，C# 脚本已移除。
**完整应用端到端 Native AOT 验收尚未完成**，下一步是实际启动和功能测试。

## 已解决

| 范围 | 本地验证 |
|---|---|
| JSON 公共入口、共享解析链及历史 AI 文件 | Windows 原生共享 JSON 程序通过；缺失契约明确失败；关闭反射专门用例 21/21 |
| 静态数据库升级 | 47 个原 EF 历史库结构/数据对照通过；原生升级器覆盖回滚、外键、并发启动与非法历史 |
| EF 原生模型和查询 | 25 个真实实体、69 查询、Include、参数变化、JSON/Cookie、事务/级联及真实仓库原生运行通过 |
| WinUI 编译/资源发布 | 引用、PRI/XBF 生成和复制顺序、辅助载荷已接通；原生门禁能拒绝 R2R/CoreCLR 输出 |
| C# 脚本阻塞 | 经用户授权移除入口、窗口、上下文和测试命令；未做 CoreCLR 辅助进程；应用无 Roslyn/EF Design 运行时包 |
| 按名称排序 | 15 个描述改用 SortDescription<T> 保留属性元数据，原字段、方向与优先级保持 |
| 当前 AI 设置 | 生成契约、配置选择、枚举、凭据排除与严格数字兼容有回归；Enka 根类型已登记 |

## P0：原生应用交互验收

物理 Esc 已停止界面自动操作。普通/R2R 版只到达独立数据目录的首次启动向导，**没有将它计作原生运行通过**。
后续恢复界面操作前须取得恢复授权，不通过命令行操作窗口绕过停止。

恢复后，使用最新原生目录与新的 SNAP_HUTAO_PROFILE_ROOT：

1. 启动、首次向导、主窗口、导航、退出、重启；核对进程模块没有 CoreCLR。
2. 在独立目录复制旧数据库/设置升级，不碰个人数据。
3. 成就、抽卡导入/导出、背包、养成、实时便笺和战斗记录；对照 UI 与实际持久化结果。
4. 用户/验证码、WebView2、通知、AI 流式对话。认证/CAPTCHA 由用户完成。

## P1：JSON 与第三方运行路径

- 网络响应、元数据根类型、当前 AI 会话和工具内容的契约闭包仍须验证；不能由旧 AI 文件或设置测试代替。
- EnumValuesExtension、EventTriggerBehavior 仍有 ILC 诊断，需按实际位置改为有限枚举/静态事件接线或取得可验证上游实现。
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
