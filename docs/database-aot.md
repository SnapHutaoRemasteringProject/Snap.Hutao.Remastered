# 数据库 Native AOT 实现

## 2026-11-12：真实旧库兼容与项目整理

- 只读检查用户报告的库得到 48 条记录，最后是 20261022144006_MakeBackpackEquippedAvatarIdNullable / EF 11.0.0-rc.2.26504.105。旧登记表只有 47 条，导致错误拒绝；不是 EF 小版本号比较造成的。未修改该用户库。
- 从原提交 7a7a03a7e 恢复该迁移及原快照，生成当前 48 条登记表；用户实际全部迁移 ID 已只读核对完全匹配。旧库已有该条时执行零条迁移；没有该条的旧库修复背包装备列可空性并保留数据。
- 对照全部 48 条 EF 历史，以及旧 NOT NULL/default-0、改写过的 nullable 和已执行 rc.2 第 48 条三种真实历史变体。检查物品/档案/索引/外键、null/0/装备角色值、旧 EF 版本记录及级联回滚；新增登记失败也必须回滚表重建。
- 两个开发项目已移至 src/Snap.Hutao.Remastered 并和两个原生验收项目加入 slnx。用途、实际命令及官方迁移替代方案见 [AOT 开发说明](../src/Snap.Hutao.Remastered/AOT.md)。
- .NET/EF 包统一用 11.*；还原到的具体 EF 版本需与生成产物相符。版本仅在生成清单中保存一次，运行时与 DDL 同事务参数化登记历史，避免每份 SQL 因补丁版本变化而重复修改。原迁移快照及旧库 ProductVersion 保留原值。

下面的 2026-11-10/11 数字是当时的覆盖范围，不代表覆盖了所有曾被删除或改写的迁移。未知历史仍拒绝；错误信息现在包含实际/目标条数和迁移 ID。

## 2026-11-11 的阶段记录

2026-11-11：应用、两个数据库生成宿主、升级验收项目及 dotnet-ef 已从 rc.3 统一到 **11.0.0**，本机 SDK **11.0.100**。
重新生成模型/访问器、样本、查询拦截器及 SQL；47 个历史库对照、普通包含拦截器的构建和生产数据层 Debug 原生运行通过。
SQL 的变化为新执行迁移所登记的 ProductVersion；已执行历史不重写。当前只使用 Debug，生成脚本默认亦为 Debug。

Schema 和 Model 两个 EXE 是开发工具：前者把历史 EF 迁移生成升级 SQL，后者从生产实体/查询生成原生模型和拦截器并验收。主应用不启动这些工具，也没有新增后台服务。
生成/校验失败会打印原因并返回 1；调用脚本必须继续判为失败。2026-11-11 修正了工具未处理异常导致 Windows 留下崩溃转储的问题，缺失 SQL/模型两项负例已确认失败退出但无新增转储；未关闭检查或系统转储设置。

## 已落地：运行时升级不再执行 EF 迁移

`AddDatabase` 在创建 EF 上下文池前调用生产 `DatabaseSchema.Upgrade`。普通构建与 Native AOT 使用同一个入口，不以跳过升级或新建空库代替旧库兼容。

迁移的事实来源是 `Migrations` 下当前保留/恢复的 48 个 EF 迁移及其目标快照。`src/Snap.Hutao.Remastered/Snap.Hutao.Database.Schema` 是仅用于生成的可移植宿主：

- 直接编译原迁移与快照，使用相同版本 EF 11 / SQLite 生成器。
- 提供同名的设计时 `AppDbContext` 来关联历史迁移，不依赖 WinUI、应用实体或用户数据库。这个上下文不用于应用查询，也不用于声称应用实体模型已通过 AOT。
- 按 `IMigrationsSqlGenerator` 的命令边界输出 SQL，补齐自定义命令之间的分号。直接拼接 `IMigrator.GenerateScript` 在历史自定义 SQL 没有结尾分号时会出现 `near INSERT` 错误。
- 生成 48 个 SQL 文件、静态登记表和原迁移源文件的指纹清单。生成文件不手改；没有 DDL/数据操作的迁移允许空脚本，但仍在事务内登记历史。
- 历史默认评分配置迁移曾调用 `Guid.NewGuid()`。生成器只对该迁移的一条初始种子使用固定 ID，使重复生成可核对；已执行过该迁移的旧库不会重新插入或更改原 ID，EF 对照验证同时检查这一点。

运行时仅使用 `Microsoft.Data.Sqlite` 执行已登记的嵌入 SQL，不扫描迁移类型或动态构建 EF 模型。原有 `__EFMigrationsHistory`、迁移 ID 和版本记录格式保留。

## 升级的一致性和失败行为

1. 升级器打开自己的非池化连接，在事务外关闭该连接的外键执行。SQLite 的表重建需要这个条件；事务内改变 `foreign_keys` 不会生效。
2. 先取得立即写事务，再读取迁移历史。两个进程同时启动时，后一个在取得写锁后重新读取历史，避免重复升级。
3. 历史必须是已知迁移序列的完整前缀。未知、缺失中间版本、更新版本、无历史但非空的数据库均拒绝修改；不降级、不删库重建。
4. 校验每份嵌入 SQL 的 SHA-256 后执行。全部待执行迁移与历史记录处于同一事务内。
5. 提交前执行 `foreign_key_check`。任何 SQL、完整性或外键失败均由事务回滚，原有数据和历史保持原状。关闭连接后不会把禁用外键的连接交给应用池。

`DatabaseSchema.targets` 对迁移源指纹及文件列表进行构建检查。修改、增加或删除迁移后必须重新生成；指纹按 LF 计算，兼容 Windows CRLF 检出。生成器宿主自身不运行这个门禁，避免“必须先生成才能构建生成器”的循环。

## 维护与本地验证

在仓库根目录运行，不需要 Windows 或应用启动：

```powershell
dotnet run --project src/Snap.Hutao.Remastered/Snap.Hutao.Database.Schema -- .
dotnet run --project src/Snap.Hutao.Remastered/Snap.Hutao.Database.Schema -- . --check
dotnet run --project src/Snap.Hutao.Remastered/Snap.Hutao.Database.Schema -- . --verify
```

`--check` 比较生成结果；`--verify` 用原 EF 迁移逐一创建历史版本，交给生产升级器升级，再对照 EF 最终数据库的表、列、索引及外键，并检查旧设置值保留。

独立 AotSmoke 项目已按用户要求移除。历史升级对照使用 Schema 的 --verify，生产 EF 原生运行使用 Generate-DatabaseAot.ps1 -Check -Native；见 [当前命令](../src/Snap.Hutao.Remastered/AOT.md)。以下是移除前的历史验收记录。

## 2026-11-10 的实际结果

- SDK 11.0.100-rc.3.26522.148，Linux x64：生成与源指纹门禁通过。
- EF 对照：**47 / 47** 个由原 EF 创建的历史数据库升级后结构一致，旧设置保留。
- 独立数据库原生发布：**0 warning / 0 error**，实际运行 **PASS**。
- 原生路径覆盖 47 个历史版本、25 个应用表、重复启动、SQL/DDL 回滚、外键失败回滚、未知/较新/缺失历史/无历史数据库拒绝以及两个并发启动。
- 主工程本地还原成功；构建实际停在 `MSB3096: GetInstalledSDKLocations`，该任务仅支持 Windows。没有把这个结果写成应用已经通过编译或启动。

## EF 模型与查询：Windows 原生运行已通过

2026-11-10 在 Windows x64 / SDK 与 EF `11.0.0-rc.3.26522.148` 上重新生成并运行验收，未沿用云端日志。
`src/Snap.Hutao.Remastered/Snap.Hutao.Database.Model` 直接链接生产上下文、25 个实体、持久化 DTO、转换器、仓库及 SQL 升级器。
UI/工厂便利成员通过 partial 文件分离，不更换持久化类型。

`AppDbQueries.cs` 与 `RepositoryQueries.cs` 包含 69 个查询终结入口。筛选、排序、聚合、关联加载、投影及分页均在数据库执行。
泛型读取按有限实体类型分派至具体查询；泛型写入继续使用 EF。批量命令只允许有限生产表名，值始终参数化，并参加调用方事务。

官方 EF 设计时服务从 `IDesignTimeModel.Model` 生成 53 个 Native AOT 模型/访问器文件，再从新编译模型生成两份官方查询拦截器。
生成宿主用真实设计模型的完整调试表示计算稳定 ModelId；不修改生成文件里的随机 GUID。
查询生成仍使用 EF 官方预编译器，仅对 rc.3 在 sealed 实体上生成的非法 `IInjectableService` 转换进行统一语法树修正。

实际原生执行发现 rc.3 对捕获枚举参数的映射仍会运行时构造 JSON reader/writer，即使模型与查询已经生成。
六个抽卡查询改为 `long` 捕获参数和数据库侧数值比较，避免被 EF 还原成枚举映射；SQL 删除命令传递枚举的 `int` 存储值。
SQL 列、筛选范围、排序与严格导出/包含删除阈值保持原语义，未改成内存筛选。

启动注册中诊断合并残留的 `context.Database.Migrate()` / `GetPendingMigrations()` 已移除，诊断 span 实际包围生产静态升级器，并记录其返回的迁移数。
仓库写入和可重排集合的泛型参数传递 EF 要求的成员保留契约，不全局压制裁剪警告。

### 可重复生成与过期检查

在仓库根目录运行：

```powershell
./scripts/Generate-DatabaseAot.ps1
./scripts/Generate-DatabaseAot.ps1 -Check
./scripts/Generate-DatabaseAot.ps1 -Check -Native
```

生成顺序为：排除旧拦截器构建宿主 → 生成原生模型/访问器 → 重建宿主 → 生成五类 JSON 样本 → 生成查询拦截器 → **普通构建（包含拦截器）** → 运行验收。
`-Check` 在内存重新生成模型、样本、拦截器并比较内容与文件列表，文件缺失、改变或多余均报错；不覆盖产物。
查询调用点的物理文件校验由 C# 拦截器编译器同时检查；查询源、模型与样本固定 LF。
`-Native` 使用 Windows `win-x64` 发布并实际运行，关闭 JSON 反射并要求动态代码支持、动态代码编译和 JSON 反射均为 false。
Linux 可显式传入 `-Runtime linux-x64`，本阶段未把 Linux 历史结果作为本地验收。

### 本阶段验收范围

- 25 个真实实体插入、物化、更新、删除、空值、Cookie、生成键和禁用跟踪读取。
- 全部 69 个查询终结入口；`Include` 用户及养成等级，聚合、投影、排序、Take 与变化的参数。
- 六个抽卡查询切换卡池、负枚举空结果、同卡池多记录排序、严格导出阈值与包含删除阈值。
- 事务回滚恢复批量删除、关联及级联删除的记录；真实背包替换在外键失败后恢复原背包。
- 设置 UPSERT、带引号的键、null 值、失效养成条目返回 0、养成 ProjectId 与 EntryId 不同、抽卡空结果哨兵、评分配置激活/更新/删除。
- 五类非空嵌套 JSON 样本与旧反射 wire policy 双向格式一致，原生读写、未知字段跳过。反射仅用于托管生成格式基准，不进入原生程序。
- 本地重跑 47 个原 EF 历史库升级对照通过；完整 Debug 测试项目 268/268 通过。

原生 EF 发布仍报告 EF 上游实验性 AOT/裁剪警告（含 DbContext 构造与 Spatialite/DependencyModel），不能写成零警告发布。
独立生产数据宿主的 PASS 不等于完整 WinUI 应用验收；当前全应用边界见 [开发报告](aot-development-report.md)。脚本随后已按用户要求移除。

GitHub CI 按当前安排不作为本阶段验收条件；以本地命令及实际原生运行结果为证据。
