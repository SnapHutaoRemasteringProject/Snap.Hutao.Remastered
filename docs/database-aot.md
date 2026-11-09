# 数据库 Native AOT 实现

## 已落地：运行时升级不再执行 EF 迁移

`AddDatabase` 在创建 EF 上下文池前调用生产 `DatabaseSchema.Upgrade`。普通构建与 Native AOT 使用同一个入口，不以跳过升级或新建空库代替旧库兼容。

迁移的事实来源仍是 `Migrations` 下原有的 47 个 EF 迁移及其目标快照。`tools/Snap.Hutao.Database.Schema` 是仅用于生成的可移植宿主：

- 直接编译原迁移与快照，使用相同版本 EF 11 / SQLite 生成器。
- 提供同名的设计时 `AppDbContext` 来关联历史迁移，不依赖 WinUI、应用实体或用户数据库。这个上下文不用于应用查询，也不用于声称应用实体模型已通过 AOT。
- 按 `IMigrationsSqlGenerator` 的命令边界输出 SQL，补齐自定义命令之间的分号。直接拼接 `IMigrator.GenerateScript` 在历史自定义 SQL 没有结尾分号时会出现 `near INSERT` 错误。
- 生成 47 个 SQL 文件、静态登记表和原迁移源文件的指纹清单。生成文件不手改。
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
dotnet run --project tools/Snap.Hutao.Database.Schema -- .
dotnet run --project tools/Snap.Hutao.Database.Schema -- . --check
dotnet run --project tools/Snap.Hutao.Database.Schema -- . --verify
```

`--check` 比较生成结果；`--verify` 用原 EF 迁移逐一创建历史版本，交给生产升级器升级，再对照 EF 最终数据库的表、列、索引及外键，并检查旧设置值保留。

原生验证项目直接编译生产升级器、登记表和 SQL，不包含 EF：

```powershell
dotnet publish src/Snap.Hutao.Remastered/Snap.Hutao.Remastered.DatabaseAotSmoke -c Release -r win-x64
& ./src/Snap.Hutao.Remastered/Snap.Hutao.Remastered.DatabaseAotSmoke/bin/Release/net11.0/win-x64/publish/Snap.Hutao.Remastered.DatabaseAotSmoke.exe
```

Linux 使用 `-r linux-x64` 并运行对应无扩展名可执行文件。程序要求 `IsDynamicCodeSupported` 与 `IsDynamicCodeCompiled` 均为 false；所有数据库位于自己创建的临时目录，结束后删除，不接触用户数据，也不请求网络。

## 2026-11-10 的实际结果

- SDK 11.0.100-rc.3.26522.148，Linux x64：生成与源指纹门禁通过。
- EF 对照：**47 / 47** 个由原 EF 创建的历史数据库升级后结构一致，旧设置保留。
- 独立数据库原生发布：**0 warning / 0 error**，实际运行 **PASS**。
- 原生路径覆盖 47 个历史版本、25 个应用表、重复启动、SQL/DDL 回滚、外键失败回滚、未知/较新/缺失历史/无历史数据库拒绝以及两个并发启动。
- 主工程本地还原成功；构建实际停在 `MSB3096: GetInstalledSDKLocations`，该任务仅支持 Windows。没有把这个结果写成应用已经通过编译或启动。

## 尚待落地：EF 模型和查询

运行时迁移阻塞已经移除。EF 查询仍需继续完成：

- 原编译模型不是 `--nativeaot` 生成的访问器版本，不能证明实体读写可以在原生运行时工作。
- `RepositoryExtension.Query` 等泛型委托隐藏了完整查询；需要把查询落在具体 `AppDbContext` / 实体入口，再由官方预编译器生成拦截器。
- 当前实体包含 UI 便利成员，持久化 JSON 还引用领域 DTO；需要先建立真实生产数据代码的独立编译入口，不能用替身实体证明应用模型可用。

GitHub CI 按当前安排不作为本阶段验收条件；以本地命令及实际原生运行结果为证据。
