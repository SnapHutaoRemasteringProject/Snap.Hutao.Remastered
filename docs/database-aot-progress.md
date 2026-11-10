# EF Native AOT 当前状态

更新：2026-11-10。实现见 [数据库说明](database-aot.md)，全应用结果见 [开发报告](aot-development-report.md)。

## 已完成并重新验证

- 生产启动用静态版本化 SQL，不发现/执行运行时 EF 迁移。
- 47 个原 EF 历史库升级后表、列、索引、外键一致，设置保留；Windows 原生升级器通过回滚、并发和非法历史检查。
- 25 个真实实体、53 个官方原生模型/访问器文件、69 个查询终结入口、两份官方拦截器和五类非空 JSON 样本已恢复并提交。
- 普通宿主构建包含拦截器，0 warning / 0 error；Windows 原生发布运行通过，动态代码支持/编译与 JSON 反射均为 false。
- 覆盖 CRUD、Include、聚合/投影、变化参数、抽卡边界、设置 UPSERT、失效更新、事务回滚、级联及真实业务仓库。
- 生成宿主与应用均用 SourceGeneration 1.3.17；重复生成与过期检查通过。

```powershell
./scripts/Generate-DatabaseAot.ps1 -Check -Native
```

改变查询/模型时先运行不带 -Check 的生成入口，再检查并原生运行。
不手改生成结果，最终普通构建不得使用 GenerateEfQueries=true。
新增表/列须配套正式迁移和静态 SQL，不能仅生成新模型。

## 边界

EF 上游实验性诊断仍在。生产数据宿主的 PASS 不等于 WinUI、账户、网络或全部功能验收。
旧云端丢失产物、无法还原的 WIP 说明已被本机新证据取代，不再作为当前操作指引。
