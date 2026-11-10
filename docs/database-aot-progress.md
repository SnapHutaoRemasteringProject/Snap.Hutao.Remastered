# EF Native AOT 工作状态（2026-11-10）

## Windows 本地验收进展

本地 `feat/aot-compatible` 头为 `5f2f874e3`，与交接中的 `ed39397e` 为不同历史，不回退已有诊断/界面修改。初始工作区干净。
当前 Windows / .NET SDK 与 EF rc.3 上已重新生成 53 个 Native AOT 模型/访问器文件、两份官方查询拦截器和五类非空嵌套 JSON 样本。

- 普通数据宿主构建包含拦截器，0 warning / 0 error；全部 69 个查询及真实仓库验收通过。
- Windows Native AOT 实际运行通过：动态代码支持/编译与 JSON 反射均为 false；25 个真实实体 CRUD、五类 JSON/Cookie、Include、参数切换、边界、回滚和级联通过。
- 重新运行 47 个原 EF 创建的历史数据库升级对照通过；完整 Debug 测试项目 268/268 通过。
- Windows 原生升级器与共享 JSON 程序发布运行通过；二者未新增编译/ILC 警告。
- WinUI Debug/x64 完整构建成功，257 个警告、0 错误；Release 原生发布和启动正在后续验收，不将数据宿主结果替代完整应用。

可重复生成、过期检查和原生验收统一入口：`scripts/Generate-DatabaseAot.ps1`，检查用 `-Check`，检查并原生发布运行用 `-Check -Native`。
原生发布仍有 EF 上游实验性诊断及待清理项目诊断，不声称零警告。详细实现与验收边界见 [数据库实现](database-aot.md)。

下面保留的是迁移到本地前的历史 WIP 说明，已不代表当前编译和数据层运行状态。

## 已交付基线

- `6f79a4138cc388bef899445d44c8f4c3045acfeb`：共享 JSON 源生成契约和原生验证。
- `b2777ac912afbd1e577a58c5feaa9ea6c37b6650`：运行时不再发现 EF 迁移；47 个历史数据库升级结构对照和原生升级器验证已通过，见 [数据库实现](database-aot.md)。

## 第二阶段：用户要求保存的 WIP 迁移检查点

当前提交恢复了 25 个生产实体、69 个具体查询终结入口、数据库命令、业务仓库接线、9 类设置写入、实体 partial 拆分和 tools/Snap.Hutao.Database.Model。EF CLI 已与应用包统一为 11.0.0-rc.3.26522.148。

前一环境中真实 25 实体 CRUD、五类 JSON 和 Cookie 转换通过了 Linux Native AOT 运行；扩展后的 69 个查询及业务验证在普通运行时通过。环境切换后，未提交目录、依赖缓存、新生成的模型/UnsafeAccessors、拦截器、样本和日志未保留。恢复源码不能沿用历史通过结果作本提交验收。

新环境 NuGet 还原被平台阻止访问 api.nuget.org:443；离线缓存也缺少 EF、SQLite、Roslyn 和项目源生成器依赖。当前恢复版本仅通过静态映射检查及 git diff --check，不声称编译通过。主 WinUI 应用尚未完成端到端原生验收。

用户随后明确要求把全部未提交工作提交到 GitHub，以迁移到本地开发。因此本次保存 WIP 检查点，不按已完成阶段记账；此后继续执行“非阶段成果不提交”的要求。GitHub CI 按用户安排不作为验收条件。

生成模型/访问器、查询拦截器和五个 JSON 样本必须重新生成，再普通编译、原生发布运行，并在 Windows 验证完整应用。完整状态、生成顺序、已知工具细节和可直接复制的提示词见 [本地开发交接](aot-local-handoff.md)。

恢复时曾保存未提交 Git tree `3dec5c9c839acc8c0088f6411f4dd42f062c7531`（源码）及 `487fbadd625ec3e67b4a3ecd0f6102d0955a1c94`（含此前状态说明）。本次提交以该完整源码快照为基础，加上本交接文档；无需在本地手工恢复这些对象。
