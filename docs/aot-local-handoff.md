# 本地继续开发 Native AOT

> 2026-11-10 本地更新：已完成 Windows 数据宿主的普通/Native AOT 发布运行，25 生产实体及 69 查询通过；生成产物已恢复，统一生成/检查入口是 `scripts/Generate-DatabaseAot.ps1`。完整记录见 [当前进展](database-aot-progress.md) 和 [实现](database-aot.md)。下文的产物缺失及无法还原是迁移前历史记录，不能覆盖本地新证据。完整 WinUI 原生验收继续进行。

仓库：`SnapHutaoRemasteringProject/Snap.Hutao.Remastered`；分支：`feat/aot-compatible`。

本次是用户明确要求保存全部未提交工作、迁移到本地开发的 WIP 检查点，不代表第二阶段验收完成。此后恢复“非阶段性成果不提交 commit”的要求。

## 必读文档

先阅读适用的 AGENTS.md、CLAUDE.md、CODE_REVIEW.md，以及 docs/aot-readiness.md、docs/aot-blockers.md、docs/database-aot.md、docs/database-aot-progress.md 和本文件。

## 已交付基线

- `6f79a4138cc388bef899445d44c8f4c3045acfeb`：共享 JSON 源生成契约、显式元数据及原生 JSON 验证。
- `b2777ac912afbd1e577a58c5feaa9ea6c37b6650`：生产数据库不再运行时发现 EF 迁移。47 个原 EF 创建的历史数据库升级后结构一致且设置保留；独立原生 SQLite 升级器通过。
- 主 WinUI 应用尚未完成端到端 Native AOT 验收。此前 Linux 构建停在 Windows 专属 GetInstalledSDKLocations（MSB3096）；本地 Windows 环境必须实际构建、发布与启动，不能将云端平台限制继续当作最终结论。

## 本检查点包含的恢复代码

- Core/Database/AppDbQueries.cs 和 RepositoryQueries.cs：25 个真实实体根、共 69 个查询终结入口，筛选在数据库中执行。
- DatabaseCommands.cs：有限表名映射、参数化批量删除/更新和设置 UPSERT，保留调用方事务。
- 成就、背包、抽卡、养成、实时便笺、账号及战斗记录等仓库接线；通用动态 IQueryable 回调接口移除，通用实体写入仍保留。
- 9 类数据库设置写入、自动签到和延迟程序设置接入统一入口。
- 实体 UI/工厂便利成员拆为 partial 文件；Cookie 的 WebView2/登录入口拆为 Cookie.Authentication.cs。持久化代码仍是真实生产类型。
- tools/Snap.Hutao.Database.Model：直接源码链接真实上下文、实体、JSON 载荷、转换器、仓库和升级器的独立生成与验收宿主，不使用替身实体。
- QueryCompiler.cs：同进程调用官方 EF 设计时服务和 Roslyn。针对 rc.3 对 sealed 实体生成非法接口转换的问题，统一用语法树修正；不要手改生成文件。
- FixtureBuilder.cs：管理工具用反射构造真实五类 JSON 样本，并用生产 PersistenceJson 编码。原生编译排除它和 QueryCompiler.cs。
- 本地 EF CLI、应用和宿主包统一为 11.0.0-rc.3.26522.148；不要添加 global.json 固定整个 SDK。
- GetCultivateProjectIdByEntryId 已改为选择 ProjectId，而非条目 InnerId；验收包含两者不同的用例。

## 缺失产物与验证边界

云端环境切换丢失了尚未提交的生成模型/UnsafeAccessors、查询拦截器、五个 JSON 样本及日志。本检查点恢复的是实现源码；已有 Model/Entity/Database/Compiled 仍是较早的已提交模型，必须重新生成，不能直接当成 Native AOT 版本。

前一环境的真实 25 实体 CRUD、五类 JSON 和 Cookie 曾通过原生运行；扩展后的 69 个查询和业务验证曾在普通运行时通过。这些历史结果不代表本检查点已验证。恢复代码仅通过静态映射检查与 git diff --check，新环境还原依赖被平台阻止，因此未声称本检查点编译通过。

## 本地接续顺序

1. 检查工作区与分支，保留本地已有修改；确认 .NET 11、Windows SDK、Visual C++ 原生工具和 WinUI 构建工具。还原依赖及同版本 EF CLI。
2. 编译 tools/Snap.Hutao.Database.Model。生成时使用 -p:GenerateEfQueries=true 排除旧拦截器，修复真实编译错误，不用替身实体或禁用检查绕过。
3. 检查 dotnet ef dbcontext optimize --help，使用同版本 EF CLI/设计时服务，从真实模型生成 Native AOT 编译模型、UnsafeAccessors、物化及追踪代码；输出主项目 Model/Entity/Database/Compiled，命名空间 Snap.Hutao.Remastered.Model.Entity.Database.Compiled。
4. 重新编译宿主，使新模型进入程序集；运行宿主 --generate-fixtures <仓库绝对路径>、--generate-queries <仓库绝对路径>，生成样本和查询拦截器。生成顺序不能颠倒。
5. 普通构建取消 GenerateEfQueries=true，确保拦截器参与编译，运行默认验收入口。随后以 PublishAot=true、JsonSerializerIsReflectionEnabledByDefault=false 发布并运行，Windows 使用 win-x64；原生程序必须显示动态代码支持和动态代码编译均为 false。
6. 修复查询预生成、Include、枚举比较、参数捕获、JSON 和字段访问问题；保留查询根与捕获参数局部变量的设计。检查参数变化、空结果哨兵、导出严格阈值、删除包含边界、失效条目更新返回 0、事务回滚、级联删除和配置切换。
7. 补齐可复现生成入口和过期检查，验证重复生成一致。不用全局压制裁剪/AOT 警告来伪造完成。
8. 回归 47 个历史数据库升级、原生升级器、JSON 相关用例。检查遗漏业务调用，并在 Windows 实际构建和发布主 WinUI 工程，验证启动、旧库升级和主要功能；继续处理 aot-blockers.md 的其余阻塞项，直至完整应用可验收。
9. 同步功能、设计、验证命令及实际结果。阶段完成再提交。GitHub CI 是用户有意不运行的，不要等待或擅自修改工作流。

## 已知细节

- SourceGeneration 1.3.16 的 SaltConstantGenerator 会请求 internal.snapgenshin.cn/Archive/Salt/Latest，失败捕获异常。云端曾离线编译数据宿主，避免不相关请求；不要伪造生产盐，不必沿用云端网络限制。
- 宿主通过独立 globalconfig 覆盖资源生成器的 ProjectDir 输入，同时保留真实 MSBuild 项目目录；SH.resx 已真实嵌入，避免 EF 依赖查找和资源加载失败。
- 查询拦截依赖物理源文件校验，.gitattributes 已固定 *Queries.cs 为 LF。
- 破坏性验收只使用临时数据库，不接触真实用户数据。

## 可直接复制的提示词

请在本地环境继续推进 SnapHutaoRemasteringProject/Snap.Hutao.Remastered 的 feat/aot-compatible 分支，完成并验收完整 Native AOT，优先完成 EF Core 数据层。先阅读适用的 AGENTS.md、CLAUDE.md、CODE_REVIEW.md，以及 docs/aot-local-handoff.md、database-aot-progress.md、database-aot.md、aot-readiness.md、aot-blockers.md。

最新 WIP 提交是我明确要求保存的迁移检查点，不代表完成或编译通过；之后不要提交非阶段成果。现有 69 个查询入口、真实实体/仓库接线及 tools/Snap.Hutao.Database.Model 已提交，但生成模型/UnsafeAccessors、查询拦截器和 JSON 样本必须重新生成，并完成普通构建、原生发布运行及完整 Windows WinUI 应用验收。不要沿用云端历史通过结果替本提交背书，也不要只做替身模型或冒烟演示。

自主修复真实问题，保持 SQL 筛选、参数化、事务、级联、JSON 兼容和原业务语义；功能与设计同步文档。不要通过删功能、禁用检查或全局压警告伪造完成。GitHub CI 有意不跑，不要等待或修改 CI。持续工作并阶段性交付，给出实际测试证据及剩余边界，不要只分析或列计划。
