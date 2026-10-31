# nahida-mouo 与 hoshiizumiya 代码质量审查

> 审查对象：本仓库两位主要贡献者（`nahida-mouo`、`hoshiizumiya`）的已合入代码。
> 审查分支：`feat/xaml-improve`（工作树），提交历史取全部分支。
> 审查方式：只读静态审查。逐文件读码 + `git log --author` / `git blame` 判定归属；
> **未编译、未运行应用**，涉及 UI 状态机、性能、真实报文的结论均标注为"推断/未验证"。
> 本文件不修改任何生产代码，也不更新 `CODE_REVIEW.md`（那是实现验证日志，二者用途不同）。

---

## 0. 摘要

| | nahida-mouo | hoshiizumiya |
|---|---|---|
| 综合评分（8 维均分） | **2.5 / 5** | **3.1 / 5** |
| 一句话 | 覆盖面最广、底层调试与注释强，但**数据完整性与状态机原子性**欠账 | 防御式设计、测试与安全边界强，但**I/O 位置与一致性**欠账 |
| 最危险的一处 | Yae 协议解析失败仍覆盖旧背包档案（N3） | 延迟启动的跨线程保存 + 页面 token 取消失效（H1/H2） |
| 最大优势 | 插件 ALC、材质单实例切换、池化缓冲区真实修复 | 凭据 `PasswordVault` + `[JsonIgnore]`、设置原子写、31 个测试文件 |

**最高优先级修复（P1，按建议顺序）**

1. **N3** 背包刷新：解析失败必须中止、保留旧档案，返回值语义要真实（[`PlayerStoreParser.cs:36`](../src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Service/Yae/PlayerStore/PlayerStoreParser.cs#L36)、[`BackpackService.cs:59`](../src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Service/Backpack/BackpackService.cs#L59)）。
2. **H1/H2** 延迟启动：`ObservableCollection` 不得在后台线程枚举保存；延迟任务不得绑定页面 `CancellationToken`（[`LaunchGameViewModel.cs:255`](../src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/ViewModel/Game/LaunchGameViewModel.cs#L255)、[`:503`](../src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/ViewModel/Game/LaunchGameViewModel.cs#L503)）。
3. **N1/N2** 插件：启用/禁用做成"钩子成功后再提交"，`manifest.Id`/`settingName` 做白名单校验（[`PluginService.cs:119`](../src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Service/Plugin/PluginService.cs#L119)、[`:215`](../src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Service/Plugin/PluginService.cs#L215)）。
4. **H3/H4** 配置持久化：读失败不得用空配置覆盖；设置加载失败后必须仍能落盘（[`AdvancedStartDelayedProgramStore.cs:65`](../src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Service/Game/AdvancedStart/AdvancedStartDelayedProgramStore.cs#L65)、[`JsonSettingsProvider.cs:50`](../src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Service/AI/Settings/JsonSettingsProvider.cs#L50)）。
5. **N5** 迁移：只增不改，历史迁移一经发布即冻结。

---

## 1. 方法与数据

### 1.1 归属判定

```powershell
git log --all --author='nahida-mouo' --no-merges --format='%h|%ad|%s' --date=short
git log --all --author='hoshiizumiya' --no-merges --format='%h|%ad|%s' --date=short
git blame -L <start>,<end> --date=short -- <path>
```

> 注意：仓库经历过一次全库改名（`c28a372f8`），纯 `--author` 统计会把"最后一次触达"误当成"作者"。
> 本文所有结论所在行均用 `git blame` 复核；作者存疑的行（如上游 `DismissedLight` 的 Yae 管道代码）不计入。

### 1.2 规模（非 l10n 提交）

| 指标 | nahida-mouo | hoshiizumiya |
|---|---|---|
| 提交数 | 381 / 485（含 Crowdin/翻译提交 104 条） | 166 |
| 增 / 删行 | +120,417 / −57,074 | +55,312 / −7,278 |
| 新增 C# 文件 | 264 | 271 |
| 新增测试文件 / 约行数 | 10 / ~700 | 31 / ~2,900 |
| 中位提交规模 / 每提交文件数 | 25 行 / 17.8 | 40 行 / 9.2 |
| 提交标题（关键词粗分） | 修复 87、功能 68、构建/CI 29、性能 9 | 修复 65、功能 47、构建/CI 16、性能 2 |
| Conventional Commits | 0/381 | 57/166 |
| 重复标题组数（最大重复） | 27 组（`提升版本号` ×10） | 18 组（同类 feat ×4） |
| >1500 行提交数 | 13 | 8 |

### 1.3 抽样策略

- nahida：插件系统（`Service/Plugin/**`、`API/**`）、Yae 互操作与二进制解析、背包/圣遗物评分、`ServerDomainService`/重定向处理器、材质切换（`MaterialAppearance`/`ControlMaterialState`）、迁移与构建脚本。
- hoshiizumiya：AI/Agent 子系统（`Service/AI/**`、`ViewModel/AI/**`、`Core/AI/**`，108 文件/约 4,800 行）、Markdown 渲染、液态玻璃 profile、延迟启动、洞察窗口、R2R 构建接线。
- **归属更正**：`Service/Git/RepositoryCatalog*` 不是 hoshiizumiya 的代码，`git blame` 全部指向 nahida-mouo 的 `32ec3faa1`；仓库内无 `Mirror` 标识符。

---

## 2. 评分卡

| 维度 | nahida-mouo | hoshiizumiya | 说明 |
|---|---|---|---|
| 架构与分层 | 3.0 | 3.0 | nahida：上帝类 + 对话框 code-behind；hoshi：AI 层 9 个 controller 很干净，但洞察窗口约 590 行 code-behind |
| 正确性（并发/生命周期） | 2.0 | 2.5 | nahida：状态机非原子、跨线程写 `ObservableObject`；hoshi：页面 token = 长任务 token、关窗等待无超时 |
| 数据完整性 | 1.5 | 2.5 | nahida：解析失败覆盖旧数据、改写历史迁移、设置 null 缓存不更新；hoshi：读失败后写入被静默禁用、延迟程序列表可被空配置覆盖 |
| 安全 | 2.5 | 4.0 | nahida：未校验 `manifest.Id` 即拼路径、`Ioc.Default`、重定向头透传；hoshi：凭据库 + `[JsonIgnore]` + 端点校验 + 工具审批，仅原始错误消息直出 |
| 内存/资源 | 3.0 | 3.5 | nahida：真实修复池化缓冲区；插件设置写非原子。hoshi：每模型切换新建客户端且无共享 transport |
| 可测试性 | 1.5 | 4.0 | nahida：背包/插件/评分零测试；hoshi：竞态、合并、迁移、安装均有测试 |
| 可读性/注释 | 4.0 | 3.5 | nahida：复杂处"为什么"写得很清楚；hoshi：XML 文档完整，但 window 五个 partial 拉长阅读路径 |
| 规范一致性 | 2.5 | 2.5 | nahida：硬编码英文异常、UI 大量复制、`{Binding}` 混用；hoshi：双本地化通路、42:1 的 `{Binding}`/`x:Bind`、批量改 14 个 locale |
| **综合** | **2.5** | **3.1** | 均分，非加权；权重不同会落在 2.4–2.7 / 3.0–3.2 |

---

## 3. nahida-mouo

### 3.1 问题清单

#### P1

| ID | 位置 | 问题 | 影响 / 证据 |
|---|---|---|---|
| N1 | [`PluginService.cs:79`](../src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Service/Plugin/PluginService.cs#L79)、`:119`、`:135` | 启用/禁用先移动文件并翻转 `IsEnabled`，再执行 `OnEnable/OnDisable`；钩子抛异常时状态已半提交；`IsEnabled` 在 `ConfigureAwait(false)` 之后赋值，而 `PluginInfo` 是 `ObservableObject` 且在 `PluginPage.xaml:111-133` 被绑定 | 插件"界面禁用但仍加载"或反之；`PropertyChanged` 在池线程触发绑定更新 |
| N2 | [`PluginService.cs:215`](../src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Service/Plugin/PluginService.cs#L215)、`:770`；[`PluginSettingService.cs:334`](../src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Service/Plugin/PluginSettingService.cs#L334) | `manifest.Id` 仅判空，随后拼进插件目录、临时目录、设置文件路径；`settingName` 来自插件代码同样未校验 | `Id: "../../x"` 使 `File.Copy`/`Directory.CreateDirectory`/`Directory.Delete` 落到任意路径 |
| N3 | [`PlayerStoreParser.cs:36`](../src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Service/Yae/PlayerStore/PlayerStoreParser.cs#L36)、[`BackpackService.cs:59`](../src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Service/Backpack/BackpackService.cs#L59)、`:63` | 解析中断时保留"部分物品"继续；随后先删旧物品再插入部分数据；`RefreshByEmbeddedYae` 恒返回 `true`，UI 提示成功 | 协议变动（Yae 随游戏版本升级）时旧档案被半份新数据覆盖，用户无感知 |
| N4 | [`MaterialAppearance.cs:38`](../src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/UI/Xaml/Control/Theme/MaterialAppearance.cs#L38)、`:146` | `hasLocalStyle` 用 `ReadLocalValue` 判断"调用方样式"，但本方法自己赋的 `MaterialNative*Style` 也是 local value；`Track` 挂 `Loaded`、`Release` 挂 `Unloaded` | 原生材质下卸载/重载后，切回玻璃不再恢复样式 → 虚拟化列表逐项丢失玻璃外观（**推断**，未运行） |
| N5 | [`20260919084443_AddBackpackItemEquippedAvatarId.cs`](../src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Migrations/20260919084443_AddBackpackItemEquippedAvatarId.cs)、提交 `b21660fc8` | 就地改写已提交迁移（`nullable:false → true`）并删除两个已生成迁移；启动执行 `Database.Migrate()` | EF 按迁移 ID 记账，改过的迁移不会在老库重跑；`1.20.4` 之前跑过 dev/canary 的库存在 `NOT NULL` 与可空模型不匹配的风险 |

#### P2

| ID | 位置 | 问题 |
|---|---|---|
| N6 | [`AvatarDataParser.cs:103`](../src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Service/Yae/AvatarData/AvatarDataParser.cs#L103)、`:134` | "形状打分"猜 `avatar_list` 字段号（注释解释了为何不写死），但嵌套 length-delimited 字段用 `ReadLength()` 而非 `SkipLastField()`——长度前缀被消费、负载未跳过，后续把负载当 tag 解析；判定失败时静默返回空，装备角色信息无声消失 |
| N7 | [`PlayerStoreParser.cs:58`](../src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Service/Yae/PlayerStore/PlayerStoreParser.cs#L58) | 顶层**所有** length-delimited 字段都按 `Item` 解析（`prop_map` 条目也会变成 `Item`），每次刷新落库一批 `ItemId=0`/小整数幻影行；入库前无 `ItemId == 0` 过滤（对照 UIGF 导入有该过滤） |
| N8 | [`PluginSettingService.cs:263`](../src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Service/Plugin/PluginSettingService.cs#L263)、`:305` | `SetSettingAsync(..., null)` 写空文件但 `SeedCache` 遇 null 直接返回 → 缓存继续返回旧值直到重启；写入是直接覆盖（非临时文件+Move），崩溃/并发会留截断 JSON |
| N9 | [`BackpackItemCategoryExtension.cs:80`](../src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Model/Entity/BackpackItemCategoryExtension.cs#L80) | 物品 id 区间全为裸数字且相互重叠，靠判断顺序决定归属；`Furniture` 枚举值永远不会被返回（无分支）；`Unknown` 桶会被构建但没有 tab 展示 |
| N10 | [`BackpackViewModel.cs:300`](../src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/ViewModel/Backpack/BackpackViewModel.cs#L300)、`:133`、`:399`、`:418` | 取消检查发生在共享字段（`categoryItems`/`categoryTokens`/`foodQualityMap`）发布**之后**；`ApplySortCommand` 与 `FilterCommand` 是同一实现的两份副本；`BuildSortTokens` 查不到分类时沿用上一分类 token |
| N11 | [`YaeService.cs`](../src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Service/Yae/YaeService.cs) 约 83 / 130 / 224 行 | 成就/背包/存档/角色四条路径各自复制约 80 行"进度对话框→切后台→receiver→版本→fieldId→config→Invoke→遍历→解析"；Mora 补算亦重复 |
| N12 | [`PluginService.cs`](../src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Service/Plugin/PluginService.cs) 全文件（909 行） | 上帝类（安装/卸载/加载/ALC/临时目录/设置定位/通知）；8 个方法重复"埋点 + try/catch + InfoBar"；`DisablePluginAsync`、`RefreshPluginsAsync` 为无 `await` 的 `async`（CS1998） |
| N13 | [`ServerDomainService.cs:12`](../src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Web/ServerDomainService.cs#L12)、[`ServerDomainRedirectHandler.cs:44`](../src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Core/IO/Http/ServerDomainRedirectHandler.cs#L44) | 新增 `Ioc.Default` 静态单例（有注释解释，仍属规范禁止项）；重定向处理器先 `Dispose()` 再读 `StatusCode`、丢弃旧 `Content` 未释放、对任意绝对 `Location` 带原请求头重发 |
| N14 | [`ReliquaryScoreCalculator.cs:103`](../src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Service/AvatarInfo/Factory/ReliquaryScoreCalculator.cs#L103) | 评分权重裸魔数（`1.1979`、`0.398*0.5`、`0.026*0.66`）无出处；同名重载 `(float)`/`(string)` 语义不同（一个做 % 换算一个不做）只靠中文注释区分；三段循环可合并 |

#### 其他模式性问题

- 新增功能几乎零测试：背包、插件、评分配置、`ServerDomainService` 在测试工程内无任何引用。
- UI 复制：`BackpackPage.xaml:588-731` 8 个 PivotItem 为同一块 22 行重复；`LaunchGamePage.xaml` 5 份相同的教学提示块。
- 可访问性：新增的凯瑟琳/合成台信息按钮、评分对话框 7 个滑杆均无 `AutomationProperties`；背包卡片只能 `Tapped`，键盘不可达。
- 本地化：`PluginService` 多处硬编码英文异常；`PluginScopedPage` 用 `Console.WriteLine` 报错；新版文件缺少 DGP Studio 版权头。
- 评分配置对话框 260 行状态机全部在 code-behind。

### 3.2 强项

1. **插件 ALC 设计正确**：契约程序集强制回宿主、插件自带副本优先、卸载前摘掉 `Resolving`，并把 `BUG-11` 的教训写进注释（[`PluginAssemblyLoadContext.cs:65`](../src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Service/Plugin/PluginAssemblyLoadContext.cs#L65)）。
2. **真实内存缺陷修复**：`3b2437599` 定位到 `ByteString` 引用已归还的 `ArrayPool` 缓冲区，改为拷贝并删除 `UnsafeAccessor` hack（[`YaeData.cs:37`](../src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Core/LifeCycle/InterProcess/Yae/YaeData.cs#L37)）。
3. **材质单实例切换**是仓库里少见的先设计后实现：注释解释了"不能恢复 caller style"与"玻璃 brush 必须清掉"的原因，只是 `hasLocalStyle` 的归属判定未闭环（N4）。
4. **域替换做得细**：只改 authority、保留端口/查询、规避两个域名后缀互含的陷阱（[`ServerDomainService.cs:63`](../src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Web/ServerDomainService.cs#L63)）。
5. **背包逐项容错**：单条物品解析失败不拖垮整包，聚合上报 Sentry（[`BackpackViewModel.cs:368`](../src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/ViewModel/Backpack/BackpackViewModel.cs#L368)）。
6. **61 文件 `x:Bind` 机械重构干净**：0 处 `((vm:…)DataContext)`，模板均补 `x:DataType`。
7. **响应式修复**：连续数日追自己功能的回归，不是"发完就走"。

### 3.3 修复顺序

1. N3（数据完整性）→ 2. N1/N2（插件状态机 + 路径校验）→ 3. N8（设置原子写/null 缓存）→ 4. N5（迁移策略）→ 5. N6/N7（Yae 解析与幻影行）→ 6. N4（材质样式归属）→ 7. N11/N12（抽重复、拆上帝类）→ 8. 补背包/插件/评分测试。

---

## 4. hoshiizumiya

### 4.1 问题清单

#### P1

| ID | 位置 | 问题 | 影响 / 证据 |
|---|---|---|---|
| H1 | [`LaunchGameViewModel.cs:255`](../src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/ViewModel/Game/LaunchGameViewModel.cs#L255)、[`AdvancedStartDelayedProgramStore.cs:34`](../src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Service/Game/AdvancedStart/AdvancedStartDelayedProgramStore.cs#L34)、`:79` | `Task.Run` 延迟 500 ms 后对 UI 绑定的 `ObservableCollection` 调 `store.Save(Entries)`；存储层 `ExecuteDelete` + 插入且无事务 | 跨线程枚举 → `InvalidOperationException`/半截快照；并发保存互相踩 |
| H2 | [`LaunchGameViewModel.cs:503`](../src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/ViewModel/Game/LaunchGameViewModel.cs#L503)、[`LaunchGameShared.cs:226`](../src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/ViewModel/Game/LaunchGameShared.cs#L226) | 延迟启动使用页面 `CancellationToken`，`LaunchAdvancedDelayedAsync` 吞掉 `OperationCanceledException` | 切页面即静默取消全部延迟启动，与功能目的相反；应使用 `ShutdownToken` |
| H3 | [`AdvancedStartDelayedProgramStore.cs:65`](../src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Service/Game/AdvancedStart/AdvancedStartDelayedProgramStore.cs#L65) | `catch { return new(); }` 把 DB/IO 故障与数据损坏一视同仁；下次 `Save` 用空配置覆盖该行 | 一次瞬时读失败 + 任意编辑 = 延迟启动列表被清空，无日志 |
| H4 | [`JsonSettingsProvider.cs:50`](../src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Service/AI/Settings/JsonSettingsProvider.cs#L50)、`:69`、`:255` | 首次加载失败后 `writesEnabled` 保持 false，保存分支直接跳过；只有外部文件变更成功才恢复 | `agent-settings.json` 损坏后，设置界面的所有改动静默不落盘 |
| H5 | [`AccountInsightsWindow.xaml.cs:119`](../src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/UI/Xaml/View/Window/AccountInsightsWindow.xaml.cs#L119)、[`.Gacha.cs:16`](../src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/UI/Xaml/View/Window/AccountInsightsWindow.Gacha.cs#L16)、[`.Charts.cs:15`](../src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/UI/Xaml/View/Window/AccountInsightsWindow.Charts.cs#L15) | 约 590 行功能逻辑在 window code-behind：拉历史、拼统计 JSON、建图表、系统提示词、关闭时序；无 VM | 违反 `CLAUDE.md §6`，除 markdown 分区器外不可测 |
| H6 | [`AccountInsightsWindow.xaml.cs:195`](../src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/UI/Xaml/View/Window/AccountInsightsWindow.xaml.cs#L195) | 关闭只取消 `lifetime`，生成请求用命令自己的 token；`DisposeClosedResources` 等 `GetPendingOperations()` 无超时 | 关窗后 HTTP 流继续、scope 延迟释放，VM 继续写已关闭窗口的绑定集合 |
| H7 | 提交 `a39807962` 的 resx 变更 | 同一提交给 14 个 `SH.<locale>.resx` 各加约 577 行英文占位（`CLAUDE.md §5.7` 禁止）；部分至今仍是英文，如 [`SH.de.resx:5862`](../src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Resource/Localization/SH.de.resx#L5862) | 14 个语言界面出现英文；Crowdin 视为"已翻译"，不会再补 |

#### P2

| ID | 位置 | 问题 |
|---|---|---|
| H8 | [`ObservableObjectHierarchyObserver.cs:43`](../src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Service/AI/Settings/ObservableObjectHierarchyObserver.cs#L43) + [`JsonSettingsProvider.cs:69`](../src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Service/AI/Settings/JsonSettingsProvider.cs#L69) + [`AgentSettingsView.xaml:183`](../src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/UI/Xaml/Control/AgentSettingsView.xaml#L183) | 每次按键：重挂全部子监听 → 同步序列化整棵设置树 → 临时文件+Move → 枚举/写入 `PasswordVault`；文件监听再把内容读回。约 40 字符的 endpoint ≈ 40 次磁盘+凭据操作，全在 UI 线程 |
| H9 | [`AgentViewModel.cs:38`](../src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/ViewModel/AI/AgentViewModel.cs#L38)、[`AgentConversationCollectionController.cs:75`](../src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/ViewModel/AI/AgentConversationCollectionController.cs#L75)、`:92` | `OnLoaded` 同步读全部会话 JSON；`OnWindowClosed` 同步重写全部会话文件 |
| H10 | [`AgentConversationTurnController.cs:197`](../src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/ViewModel/AI/AgentConversationTurnController.cs#L197)、`:199`；[`AgentConversationViewModel.cs:105`](../src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/ViewModel/AI/AgentConversationViewModel.cs#L105) | 用户消息先入列表、再校验 API Key 并抛异常；`CanSendMessage` 未像 `CanRegenerate` 那样检查请求参数。正常关窗会被全量保存兜底，异常退出才丢 |
| H11 | [`AgentChatClientFactory.cs:60`](../src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Service/AI/AgentChatClientFactory.cs#L60)、[`AgentService.cs:18`](../src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Service/AI/AgentService.cs#L18) | 每次切换模型新建客户端并各自持传输；`OpenAI.OpenAIClient` 经元数据核实**不实现 `IDisposable`**，连接池无法显式回收，客户端只增不减直到 scope 释放 |
| H12 | [`StringResourceProxy.cs:14`](../src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Resource/Localization/AI/StringResourceProxy.cs#L14)、`:29` | 第三套本地化通路（运行期字符串键索引器 + `StringResourceValue` + `snuxm:` 前缀）；`SupportedCultures = ["zh-Hans","en"]` 与 15 个 locale 矛盾且无任何引用（死代码） |
| H13 | [`AgentSettingsView.xaml`](../src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/UI/Xaml/Control/AgentSettingsView.xaml) | 388 行内 42 处 `{Binding}`、1 处 `x:Bind`；没有类型化 `DataContext` 影子属性 |
| H14 | `Build/ReadyToRun.targets:5` | `AfterTargets="Build"` + `DependsOnTargets="ComputeFilesToPublish"` 使每次 Build（含 Debug）都跑发布/CrossGen 管线 |
| H15 | [`LiquidGlassProfile.cs:340`](../src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/UI/Xaml/Control/Theme/LiquidGlassProfile.cs#L340)、`:274` | `TryEnqueue` 失败时回滚但不标记参数失败（用户只看到数值弹回）；重试用原始值而非已知可用的 `cachedDefault`；3 轮"stabilize"用异常做失败路径（仅 Sentry） |
| H16 | [`SettingsModelConfigurationViewModel.cs:205`](../src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/ViewModel/AI/Settings/SettingsModelConfigurationViewModel.cs#L205)、[`AgentService.cs:69`](../src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Service/AI/AgentService.cs#L69) | 第三方异常 `ex.Message` 原样进 InfoBar / 聊天记录；AI 代码 12 处 `!` 空宽容（`options.ApiKey!` 等，违反 SH007） |

#### P3 / 待测量

- [`AccountInsightsWindows.cs:45`](../src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/UI/Xaml/View/Window/AccountInsightsWindows.cs#L45)：`Receive` 未做线程封送（同类接收者都走 `InvokeOnMainThread`）。
- [`MarkdownHelper.cs:717`](../src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/UI/Xaml/Control/Markdown/MarkdownHelper.cs#L717)：正则含 `(?:\\.|.)+?` 与变长反向引用，无法 `NonBacktracking`，未设 `matchTimeout`；**未做基准，不认定 ReDoS**。
- [`ObservableTextContent.cs:29`](../src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Service/AI/Observables/ObservableTextContent.cs#L29)：流式 `Text +=` 为 O(n²)、每次 set 触发全串 `StartsWith`；量级不致命。
- 液态玻璃测试用 double 替掉 31 个光学描述符中的 30 个，并靠反射重置私有静态字段 → 回归保护有限。

### 4.2 强项

1. **凭据处理**：`PasswordVault` + `[JsonIgnore]` + 清空即删的陈旧项清扫；全仓 `ApiKey` 站点无日志/埋点泄漏（[`AgentCredentialStore.cs:12`](../src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Service/AI/Settings/AgentCredentialStore.cs#L12)）。
2. **设置/会话持久化**：临时文件 + `File.Move` 原子替换，版本号防"读到一半被本地编辑覆盖"（[`JsonSettingsProvider.cs:227`](../src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Service/AI/Settings/JsonSettingsProvider.cs#L227)、[`AgentConversationFileProvider.cs:68`](../src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Service/AI/AgentConversationFileProvider.cs#L68)）。
3. **工具面极小且需审批**：只有 `GetCurrentTime`/`ShowSummary`，关闭 file memory/web search/todo/agent mode/skills/OTel/自动审批。
4. **模型输出渲染有防线**：只允许 http(s) 链接、图片降级为链接、无 HTML 通路、稳定的块级增量渲染。
5. **端点校验**：仅 HTTPS 或 loopback HTTP，拒绝 `UserInfo`。
6. **测试意识**：31 个测试文件覆盖竞态、合并、迁移、安装、媒体控制；`AgentSettingsWatchRaceTest` 正对最难的并发点。
7. **兼容层克制**：`UsageContentRectifyDelegatingChatClient` 的非流式索引回删与"延迟一个 update"流式处理都正确，`await using` 到位。

### 4.3 修复顺序

1. H1/H2/H3（延迟启动的数据与生命周期）→ 2. H4（设置持久化恢复）→ 3. H5/H6（洞察窗口 VM 化与关闭时序）→ 4. H8/H13（设置保存防抖 + `x:Bind`）→ 5. H7（停止改下游 locale）→ 6. H11（客户端/transport 缓存）→ 7. H14/H15（构建与玻璃 profile 的失败可观测性）。

---

## 5. 共同问题

1. **没有质量门**：8 个 workflow 均不跑 `dotnet test` 或 `dotnet format --verify-no-changes`（`CODE_REVIEW.md#ENG-05`）。上面绝大多数问题都不会在 PR 阶段被拦。
2. **UI code-behind 承载决策**：nahida 的评分配置对话框（260 行）、hoshi 的洞察窗口（约 590 行 / 5 个 partial）。
3. **本地化双轨**：nahida 硬编码英文异常；hoshi 另起 `StringResourceProxy`/`SHName` 通路并批量改 14 个 locale。
4. **新功能缺测试**：两人都在"纯逻辑/竞态"上缺回归保护，hoshi 明显好于 nahida。
5. **硬编码的协议/数字**：nahida 的物品 id 区间、评分权重；hoshi 的 R2R 载荷清单、光学参数默认值。都需要"来源 + 常量名"。

---

## 6. 建议

### 6.1 分工建议（按现有强项，而非评价人）

- **nahida-mouo**：协议解析、底层互操作、材质/控件基础设施、构建与发布脚本——他在这几块的问题都是"差一道闸"，补上事务/校验/测试即可。
- **hoshiizumiya**：设置与凭据、AI 集成、需要长期演进的控制器层——他的分层与测试习惯更适合这类"边界多、要长期维护"的模块；UI 窗口层应交出。

### 6.2 团队级改进（一次投入，长期收益）

1. CI 增加 `dotnet test` + `dotnet format --verify-no-changes`（最小门禁）。
2. 迁移规范：**只增不改**；已在文档与 PR 模板中写明，并在 CI 检查迁移文件是否被修改。
3. locale 规范：只改 `SH.resx`；CI 检查 `SH.<locale>.resx` 是否被同 PR 修改。
4. 事务模板：为"先删后插"的刷新路径提供 `ExecuteInTransaction(Action<AppDbContext>)` 帮助方法，避免每次手写。
5. UI 线程规则：在 review checklist 增加"后台线程不得枚举/写入绑定集合；跨页任务不得使用页面 token"。
6. 协议解析：统一用 `SkipLastField()` 跳过未知字段，并对关键解析器提供真实报文的回归样本（可脱敏）。

---

## 附录 A：已撤回或降级的判断

| 曾提出的判断 | 结论 | 依据 |
|---|---|---|
| `OpenAIClient` 可能"未释放、泄漏连接" | **撤回** | 元数据读取：`OpenAI.OpenAIClient` / `OpenAIModelClient` / `ChatClient` / `ResponsesClient` 的接口实现表为空，不实现 `IDisposable`；问题改为"每客户端各自持传输、无共享"（H11） |
| 缺 API Key 时用户消息"静默丢失" | **降级为 P2** | 正常关窗会 `SaveConversations` 全量写盘；仅异常终止才丢（H10） |
| 设置 live-reload 在窗口打开前"一定失效" | **降级为 P3** | 设置 provider 在窗口内首次解析，构造前已 `Initialize`；生产路径不可达，属隐式时序契约 |
| Yae `cmdId` 读写宽度不对称（ushort vs uint） | **撤回** | 二者属于不同命令（`RequestPacketList` 写 uint 列表；`ResponsePacket` 读 ushort 字段），不能据此判定协议错误 |
| "nahida 名下 84 处空 catch" | **撤回** | 该统计按"最后触达作者"归集，混入上游代码，不可归属 |

## 附录 B：未验证清单（重审时优先补）

1. N4 的材质恢复退化——需实际运行并滚动虚拟化列表。
2. `ToggleSwitchHeaderHitTestVisibleBehavior` 在材质切换后是否失效——需运行验证。
3. N6 的 `ReadLength()` 影响——需构造真实 `avatar_data` 报文跑一次解析。
4. H14 的 R2R 构建耗时影响——需在干净仓库计时 Debug Build。
5. Markdown 正则最坏情况——需对 50k 字符对抗输入计时（不得据此宣称 ReDoS）。
6. H11 的连接池累积——需多次切换模型并观察 socket 数量。
7. 所有 UI 线程/绑定时序结论——均未经运行验证。

## 附录 C：复核命令

```powershell
# 作者与规模
git log --all --author='nahida-mouo' --no-merges --numstat --format=''
git log --all --author='hoshiizumiya' --no-merges --numstat --format=''

# 关键行归属（示例）
git blame -L 100,160 --date=short -- src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Service/Plugin/PluginService.cs
git blame -L 90,155 --date=short -- src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Service/Yae/AvatarData/AvatarDataParser.cs

# 迁移与发布点
git log --all --diff-filter=A --format='%an %h %s' -- src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Migrations
git merge-base --is-ancestor afacb140b 1.20.4    # 该迁移首次进入的正式版本
```

---

*本文件为审查记录，不代表任何已修复状态；修复后建议在同目录新建 `code-review-contributors-<date>-followup.md` 或在 `CODE_REVIEW.md §14` 追加验证行。*
