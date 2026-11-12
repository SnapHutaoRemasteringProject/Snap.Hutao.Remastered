# AOT 开发与验收项目：用途与操作

以下两个项目都用于生成和验证生产数据库代码，位于本目录并加入 slnx 的 AOT Development 分组。主应用不会启动或打包它们，也不安装后台服务；正常启动项目仍选 Snap.Hutao.Remastered。
保留源码；bin/obj 是本机产物，不提交。当前所有命令均使用 Debug。

| 工具 | 解决什么问题 | 输入 → 输出 | 什么时候用 |
|---|---|---|---|
| Snap.Hutao.Database.Schema | Native AOT 不能依赖 EF 在启动时反射发现、执行迁移 | 应用 Migrations 中的原迁移/快照 → Core/Database/Schema 下的 SQL、清单与指纹 | 增删改迁移、升级 EF 后重新生成；提交前检查和历史库对照 |
| Snap.Hutao.Database.Model | EF 原生程序不能在运行时构造模型、编译 LINQ 查询 | 真实生产上下文/25 个实体/查询/JSON DTO → Compiled 下的模型、访问器、官方查询拦截器 | 实体映射、查询、持久化 JSON 或 EF 版本变化后生成并实际原生运行 |



两个独立 AotSmoke 测试项目已按用户要求移除；它们不参与应用功能，也不负责生成生产代码。原生生产数据层验证保留在 Model 的 -Native 入口，历史升级对照保留在 Schema 的 --verify 入口。

## 开发者怎么运行

仓库根目录：

```powershell
# 1. 迁移源有变化：生成 SQL，随后检查并对照原 EF 创建的历史库
dotnet run --project src/Snap.Hutao.Remastered/Snap.Hutao.Database.Schema -c Debug -- .
dotnet run --project src/Snap.Hutao.Remastered/Snap.Hutao.Database.Schema -c Debug -- . --check
dotnet run --project src/Snap.Hutao.Remastered/Snap.Hutao.Database.Schema -c Debug -- . --verify

# 2. 模型、查询、持久化 DTO 或 EF 有变化：按正确顺序生成并验收
./scripts/Generate-DatabaseAot.ps1
./scripts/Generate-DatabaseAot.ps1 -Check -Native

```

脚本依次生成模型/访问器、重建宿主、生成官方拦截器，然后普通构建（包含拦截器）并执行；-Native 再发布和运行真正的原生程序。
不要手改 SQL 或 Compiled 文件。--check 发现过期文件时返回 1；先确认迁移源正确再生成，不能忽略检查。

Schema 的同名空上下文仅用于读取历史迁移，**不能拿它执行 migrations add 创建新生产迁移**。
新增迁移需要包含生产模型和完整历史快照的设计时环境；当前 Model 工具尚未提供这条创作流程。已使用过的迁移 ID 必须保留，不能因修改旧迁移或整理 Git 历史而删除。

## 数据库到底如何变化

应用启动仅执行已生成且校验通过的待执行 SQL；不启动上述工具。已有迁移记录不重写，不删除用户库，不支持自动降级。
2026-11-12 恢复的是旧提交 7a7a03a7e 中已经使用过的 20261022144006_MakeBackpackEquippedAvatarIdNullable：

- 该 ID 已在旧数据库的 __EFMigrationsHistory 中出现，后来源码被清理；只保留 47 条的 AOT 登记表会错误拒绝这种 48 条历史。
- SQL 将 backpack_items.EquippedAvatarId 的旧 NOT NULL/default-0 约束修正为可空，复制原物品数据并恢复原索引/外键。
- 已经记录第 48 条的数据库启动时不重跑、不修改记录；只有前 47 条的库才执行这条修复。

ProductVersion 是 EF 生成器的完整版本，MigrationId 才标识应用过哪条迁移。当前版本只在 DatabaseMigrations.EfProductVersion 中生成一次，执行每条 SQL 后在同一事务里参数化登记；SQL 文件只保留结构/数据操作，避免包补丁版本造成每份 SQL 都变化。旧库里 10.0.x、11.0.0-rc.x 的记录原样保留。
全部 csproj 的包版本统一在仓库根 Directory.Packages.props 管理；各 csproj 只保留 PackageReference 和包用途元数据。.NET/EF 配套包通过其中的 HutaoDotNetPackageVersion=11.* 跟随稳定 11 系列；SDK 仍不通过 global.json 固定版本。构建检查实际 EF 版本是否与生成产物一致，版本变化时重新生成，不手工改版本或关闭检查。本流程直接用 EF 官方设计服务，不需要另装 dotnet-ef；已移除重复固定版本的本地工具引用。
需要检查 11 系列新包时运行 `dotnet restore src/Snap.Hutao.Remastered/Snap.Hutao.Remastered.slnx --force-evaluate`，再执行上面的生成与验收。11.* 是还原规则，不表示应用启动时自动联网更新依赖。

## 为什么显式保存 SQL，官方 AOT 路线是什么

当前仍然是在应用启动时升级数据库，只是执行预先生成的 SQL，而不是调用 EF Database.Migrate()。
[EF 11 的 Migrate 源码](https://github.com/dotnet/efcore/blob/v11.0.0/src/EFCore.Relational/Extensions/RelationalDatabaseFacadeExtensions.cs)仍明确标为 RequiresDynamicCode，并建议 migration bundle 或其他执行方式。
[官方部署文档](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/applying)提供 bundle、SQL、CLI 和运行时迁移等方案，没有要求所有 AOT 应用采用同一种部署方式。

本项目选择保留原 C# 迁移为事实来源，由 EF 官方 SQL 生成器提前生成并嵌入 SQL，以便纯原生主进程启动时自动升级个人 SQLite 库，不引入额外 CoreCLR 迁移进程。SQL 是生成结果，不是另一套手工维护的数据库定义；结构变化的差异应能审查，版本号变化已集中处理。
查询方面仍使用官方 compiled model/UnsafeAccessors/预编译查询拦截器；它们解决模型和查询运行，不会令 Database.Migrate 自动获得 AOT 支持。

当前验证覆盖登记的 48 条迁移，以及已发布过的旧 NOT NULL 列变体。它不等于覆盖 Git 历史中所有被删除、改写的迁移分支；其他未知 ID 仍拒绝修改，并给出具体不匹配记录。
实现与详细验证见 [数据库说明](../../docs/database-aot.md)，应用交付见 [AOT 报告](../../docs/aot-development-report.md)。

Fixtures、样本生成器及文件格式基准检查已按用户要求移除。Model 保留真实实体 CRUD 和基本 JSON 往返；不再声称覆盖原先五份深层旧格式样本。
