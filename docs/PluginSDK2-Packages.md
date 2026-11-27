# SDK 2.0 包格式与宿主运行行为

## 1. Manifest schemaVersion 2

.hutao 是 ZIP 文件，不是程序集。ZIP 根必须有 manifest.json，其他所有常规文件均列入 files。源项目编辑 manifest.template.json，打包器负责生成 files。以下是最小程序式 UI 示例：

```json
{
  "schemaVersion": 2,
  "id": "Example.MyPlugin",
  "name": "My Plugin",
  "version": { "major": 1, "minor": 0, "patch": 0 },
  "author": ["Example Author"],
  "description": "Example programmatic plugin",
  "api": { "major": 1, "minMinor": 0 },
  "minHostVersion": "1.0.0",
  "runtime": { "kind": "winrt-native", "architecture": "x64", "entry": "bin/x64/MyPlugin.dll" },
  "ui": { "mode": "programmatic" },
  "capabilities": ["navigation", "notifications"]
}
```

| 字段 | 类型及约束 |
|---|---|
| schemaVersion | 整数 2，标记包清单格式 |
| id | 1–128 ASCII 字符，字母/数字起始，其余允许字母数字 ._-；禁止 ..、结尾点、设备名；唯一性大小写不敏感 |
| name | 1–128 字符；可读名称不是 ID |
| version | major/minor/patch，三个非负 Int32 |
| author | 1–16 个非空作者字符串，每个 ≤128 字符 |
| description | 字符串，≤4096，可空字符串 |
| iconPath | 可选、已列入 files 的相对路径 |
| minHostVersion | 三段 Version；宿主产品版本前三段比较。1.0.0 是测试样例占位，不是实际旧宿主支持声明 |
| api | major=1, minMinor=0，其他值当前拒绝 |
| runtime | kind=winrt-native，architecture=x64，entry=bin/x64/xxx.dll |
| ui.mode | none / programmatic / loose / compiled |
| ui.metadataProvider | 仅 compiled 必需；组件 WinRT runtimeclass 全名，如 TestPlugin.Native.XamlMetaDataProvider |
| capabilities | 唯一能力字符串数组，见下表 |
| gameLibraries | 可选数组，最多 16 个唯一顶层 inject/*.dll 路径；与 game-libraries 能力同时声明，全部列入 files 并通过 native x64 PE 校验 |
| files | 打包器生成 [{path,sha256}]；小写 64 个十六进制字符；不得列 manifest.json 本身 |

`schemaVersion` 表示清单格式，`api.major/minMinor` 表示宿主和插件使用的二进制接口（ABI）版本，`version` 则由插件作者为每次发布递增。SDK 1.x 的历史清单无 `schemaVersion`，也没有 V2 的 `runtime`、`api`、`ui` 和 `files` 字段。宿主将这类包显示为 SDK 1.x，并提示安装作者用 V2 SDK 重新构建的包。缺少 `schemaVersion` 但含 V2 字段时报告清单错误。

编译型包的同名 PRI 放在入口 DLL 旁边并登记在 `files` 中。组件 DLL 的文件名不含扩展名时，应与 XAML URI 中的组件前缀一致。XBF 作为 EmbeddedData 写入 PRI，供宿主的 MRT（现代资源管理系统）读取。

能力映射：

| capability | 对应服务 |
|---|---|
| settings | IPluginSettings |
| notifications | IPluginNotifications |
| navigation | IPluginContributions.RegisterView / RegisterNavigationItem |
| commands | IPluginContributions.RegisterCommand |
| dialogs | IPluginDialogs |
| xaml | IPluginUi；仅 loose 允许 |
| game-path | C# First 游戏路径定位服务；可能同步主程序已定位的路径，要求 active UI STA |
| game-libraries | 声明原有 Island 游戏启动流程的第三方库；不是 WinRT 任意进程操作服务 |

none 不允许 navigation；compiled 不允许 xaml。Dispatcher、Log、生命周期元数据不需要独立 capability。能力只约束 SDK API，**不能阻止进程内原生代码直接访问文件、网络和内存**，不是沙箱。

## 2. staging 与验证

```text
stage/
  bin/x64/MyPlugin.dll
  bin/x64/OptionalNativeDependency.dll
  views/Main.xaml
  assets/icon.png
```

把 manifest.template.json 放在 staging 外；入口必须存在且为 x64 native PE DLL，不能含 CLR CorHeader。打包器遍历 staging 的普通文件，按 ordinal 路径排序并使用固定 ZIP entry timestamp。用同一工具链和完全相同字节输入才能比较可重现哈希。

路径以 / 分隔，相对路径 ≤1024，单段 ≤255；不允许 ..、反斜杠、冒号、尾随空格/点、设备名、控制字符、reparse point、大小写重复或文件/目录冲突。校验限制：压缩包 256 MiB、单条目 256 MiB、解压总量 512 MiB、总 ZIP 条目 ≤4096、manifest 256 KiB、.xaml 1 MiB、icon 4 MiB。JSON 重复属性拒绝。验证不执行第三方 DLL。PE 格式校验不保证所有导出/依赖/ABI 兼容，激活时仍需检查 HutaoPluginCreate 与 IHutaoPlugin IID。

PluginPack.exe pack <manifest-template> <staging-dir> <output.hutao>：生成并验证包、输出 SHA256；verify <package>：输出 ID、版本与 SHA256。退出码 0 成功，1 操作失败，2 命令用法错误。ZIP SHA256 不是签名或发布者认证，用户必须确认来源。

## 2.1 游戏附加库的声明（v1 inject/ 迁移）

新版不会仅因为目录里有 DLL 就处理它。包作者需要明确声明 game-libraries 能力以及 gameLibraries 文件列表。该声明与已存在的 plugin runtime.entry 是两种不同角色：runtime.entry 是宿主内运行的 SDK 插件入口，gameLibraries 是原有游戏启动服务使用的附加库。后者不要求 HutaoPluginCreate 导出，但必须是 native x64 DLL。未声明的 inject/ 文件不会被自动加载。有效的 v2 plugin runtime.entry 仍然必需。

例如，对经授权的游戏附加组件，manifest 中可包含：

```json
"capabilities": ["game-libraries"],
"gameLibraries": ["inject/ExampleGraphicsExtension.dll"]
```

上例假设 ui.mode=none、无其他宿主贡献。所有声明文件必须存在于 staging 中，并由 PluginPack 写入 files 哈希清单。带此能力的包在信任确认对话框中列出请求及风险；只有 active、已批准的插件进入现有 Island 新游戏启动流程的快照，预检查和使用前再次验证文件哈希。禁用影响下一次启动，不会卸载已经运行的游戏中已加载的库。启动快照形成后禁用不撤销该次快照；不要在启动过程中修改包。Resume、普通非 Island 启动、PrototypeHost 不执行游戏附加库。该功能不是任意 PID 或进程控制 API，不保证第三方组件、反作弊或游戏版本兼容性；仅在游戏允许的环境使用。

## 3. 正式安装存储与信任

路径在实际 HutaoRuntime.DataDirectory 下：

```text
Plugins/
  Example.MyPlugin.hutao                 # 持久化启用意图
  OtherPlugin.hutaodisabled              # 禁用意图
  Example.MyPlugin/
    Counter.json
    Message.json
    data/
  .sdk2-state/
    host.lock
    running
    Example.MyPlugin/
      state.json
      pending.hutao                     # 更新暂存时
      backup.hutao                      # 应用更新后到首次成功启用前
    workspaces/<process-generation>/<id>/...
```

插件管理扫描只验证包；不映射 DLL。重复 ID、两种后缀冲突或文件名/manifest ID 不一致不执行。冲突文件需要用户确认后手动移走一个再刷新。安装 .hutaodisabled 只保存已验证包；安装 .hutao 先展示实际 ID/版本/ZIP SHA256 与同进程权限警告，用户信任后安装并启用。启用时再次验证准确哈希与解压文件；旧版 ZIP 和 managed DLL 不可运行。正式宿主不加载开发裸 DLL。

state.json 记录批准的包 SHA256、成功安装回调 SHA256、待更新与备份哈希、错误和暂存进程代号。包后缀仍是启用意图，不是 active 会话状态。成功 OnLoad/OnInstall/OnEnable 后才提交启用后缀并公开贡献。失败清理上下文，保留禁用包及错误记录；不能因失败重新执行同一已隔离模块。

正常启动仅自动启用已批准、字节未变、.hutao 且无待更新的兼容包。管理员进程跳过自动启用，每次显式启用需用户确认。上次存在 running marker（异常退出）则进入恢复模式，跳过自动启用，仍可由用户明确手动启用。host.lock 防止多个 SDK2 宿主同时管理同一数据目录；不同 profile 应使用各自数据目录。正常退出逻辑关闭 active 实例并保留启用意图；异常退出不保证执行回调。

workspace 是当前进程的验证过的解压内容，不是用户数据目录。模块一旦映射便保持到进程退出；禁用不覆盖/删除映射文件。相同包的重新启用验证 workspace 的文件集合和哈希。下次获取独占锁时清理以前的工作区，遇到 reparse point 或 IO 错误记录并跳过。

## 4. 更新、回滚、卸载

更新通过插件管理页选择 Update，必须同 ID、更高三段版本、与宿主 ABI/版本兼容，并确认新包哈希。当前进程只暂存，不热替换；当前 active 插件可继续工作。重启后使用 Apply staged update（当前实现选择显式应用而不是无提示自动替换），将旧 ZIP 保存为 backup，验证 pending 后原子替换为 disabled 新包。用户再次启用新包；成功后清理 backup，失败保留备份供回滚。

Roll back：尚未应用时取消暂存；已应用时，若新版本未成功启用，恢复已验证的旧 ZIP 为 disabled 并清除批准，用户重新确认信任。若当前进程曾映射新版本，必须再重启才能启用恢复版。中断更新时检查 state.json、pending/backup SHA256；若文件和 marker 不一致，不自动信任其他文件。此更新恢复实现仍需故障注入测试；不是经过证明的完整事务日志实现。

卸载 active 实例先逻辑 disable/撤销 UI，再 OnUninstall；错误保留 disabled 包并显示错误。卸载 disabled/legacy 不为调用回调而运行 DLL。默认保留 Plugins/<id> 下的设置和 data；用户明确确认后可在应用退出时手动删除。旧包迁移目前采用显式卸载旧 ZIP（数据保留）并安装同 ID 的 SDK2 重建版；不承诺兼容旧 AssemblyLoadContext 或旧 injection API。

## 5. 安全与发布

原生插件与宿主具有相同 OS 进程权限。恶意或错误代码可能破坏宿主、读取数据、造成崩溃。仅向用户分发可信代码，明确来源、版本、SHA256、所需能力；不要把权限声明或 hash 当作安全隔离。禁止插件覆盖宿主文件、强行卸载 Native AOT 模块、引用内部程序集、全局资源 Hook、任意反射注入等。旧 inject/ 的宿主扩展已通过上文显式声明恢复；未声明文件不自动加载。SDK2 ABI 不暴露任意进程操作服务。若需要其他业务扩展点，单独设计并审查新接口及 IID。
