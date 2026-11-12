# Native AOT 后续开发流程

仓库：SnapHutaoRemasteringProject/Snap.Hutao.Remastered；分支：feat/aot-compatible。
更新：2026-11-12。先读适用的 AGENTS.md、CLAUDE.md、CODE_REVIEW.md。

## 文档入口

| 文档 | 用途 |
|---|---|
| [开发报告](aot-development-report.md) | dev 对比、交付、证据、清理与未验收范围 |
| [构建与交付](aot-readiness.md) | 默认 R2R、原生发布、独立目录和文件门禁 |
| [当前阻塞项](aot-blockers.md) | 后续按优先级验收/修复的工作 |
| [数据库实现](database-aot.md) / [状态](database-aot-progress.md) | 升级、模型、查询、生成及业务边界 |
| [CsWinRT 3 评估](cswinrt3-assessment.md) | 实际资产选择、已知原因和 issue 提示词 |
| [历史扫描](https://github.com/SnapHutaoRemasteringProject/Snap.Hutao.Remastered/blob/32976c2f4b7ef2f8053b34131ffd7fdbae5b5722/docs/aot-blockers.md) | 旧 194 条基线及早期实验，不是当前结论 |

## 起点与用户决策

- 本机 SDK 为 11.0.100；.NET/EF 包统一共享 11.*（当前还原 11.0.0），不新增 global.json。本流程直接调用官方设计服务，无独立 dotnet-ef 工具依赖。
- 两个 AOT 生成项目已在 src/Snap.Hutao.Remastered 与 slnx；用途及命令先读该目录的 [AOT.md](../src/Snap.Hutao.Remastered/AOT.md)。
- 必须保留已使用的迁移 ID。用户实际旧库有 20261022144006_MakeBackpackEquippedAvatarIdNullable，已从原提交恢复；当前登记 48 条，不能重新退回只含 47 条的版本。
- 用户要求当前只做 Debug 构建/原生发布，不构建 Release。Generate-DatabaseAot.ps1 也已默认 Debug。
- 数据层真实生产代码原生运行通过，生成产物齐全；主 WinUI 已实际原生发布。
- 插件/Harmony 已在此前分支移除；C# 脚本现经用户授权移除，不做 CoreCLR 脚本进程。
- CsWinRT 保持 2.3.1；3.0 评估止于依赖还原，不能说新版已修复运行问题。
- 物理 Esc 停止了界面操作，原生交互验收未完成；恢复操作前须取得恢复授权。
- 只提交可验收阶段成果，不保存普通 WIP commit；不擅自推送、等 CI 或改 CI。

## 开始前

```powershell
git status --short
git branch --show-current
git fetch origin dev
git merge-base origin/dev HEAD
git diff --stat origin/dev
dotnet --info
dotnet tool restore
```

保留已有修改，不切回/重置分支；不沿用旧环境的通过结果。
需要 Windows SDK、VC++ 原生工具、WinUI 构建工具及 WebView2。

## 数据库改动

1. 表/列/关系变化先准备正式迁移；迁移创作宿主须包含原快照并先做无改动基线演练。Schema 工具只生成/执行已有迁移，不能用它的最小上下文创作新生产实体迁移。
2. 有迁移变化时，运行 Schema 工具生成 SQL，再 --check、--verify 对照全部历史版本。
3. 运行 Generate-DatabaseAot.ps1，按模型/访问器 → 重建宿主 → 样本 → 官方查询拦截器顺序生成。
4. 运行 -Check -Native，检查过期/多余产物、普通构建并实际原生运行。
5. 扩充真实业务验收，检查 Include、参数变化、空值/哨兵、严格导出/包含删除边界、回滚与级联。

不手改 Compiled、SQL、样本，不加载全表后内存筛选。
GenerateEfQueries=true 只作生成引导，不用于最终构建或验收。

## 应用构建和发布

先普通构建与测试，再串行完整原生发布；不要并行覆盖同一 obj/project.assets.json。
命令见 [构建与交付](aot-readiness.md)。当前 SDK 的 WinUI/Native AOT --no-build 组合有生成项问题，使用完整发布。
发布到新目录，核对 PE、无主托管 DLL/CLR 宿主、PRI/XBF、Assets/Resource 和辅助载荷。

恢复界面操作后，用 Test-NativePublish.ps1 -Launch -ProfileRoot <绝对路径> 在独立目录验收。
执行 [当前阻塞项](aot-blockers.md) 的启动/退出、旧库、主要页面、WebView2、通知、AI 和打包流程；认证/CAPTCHA 由用户完成。
不把编译成功、文件检查或数据宿主写成完整应用已验收。

## 阶段交付

记录源码版本、SDK/包版本、命令、退出码、运行结果及未验证范围；详细日志留 artifacts，不把文件大小/哈希清单当成功能报告。
同步开发报告、领域文档和 CODE_REVIEW.md；比较 dev，清理实验漂移，保留必要生成/验证宿主。
测试失败须记录原因与复验，不通过过滤/抑制写成全量通过。警告逐项归属，不复用旧 194 条作新统计。
