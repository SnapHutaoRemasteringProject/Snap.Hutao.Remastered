# Snap.Hutao Plugin SDK 2.0 — 实现规范（提案 v1.0）

> 本文件保留原始设计目标和验收门槛。当前实际源码、开发方式及尚未通过的验证项请优先参阅 [开发上手](PluginSDK2-GettingStarted.md)、[API](PluginSDK2-API.md)、[包格式](PluginSDK2-Packages.md)、[验收记录](PluginSDK2-Testing.md)。实现与设计存在的差异在验收记录明确列出。

基线：SnapHutaoRemasteringProject/Snap.Hutao.Remastered `main` @ `b0fa7ce2141abc31fb7e3ae2b65951e0ab40153a`。本文描述目标规范，不代表代码已实现或已通过 Windows 测试。实施前由用户批准；批准后作为规范性设计，代理不得擅自修改。文中 MUST/必须、MUST NOT/禁止、SHOULD/建议分别表示强制、禁止、可解释的建议。本文指定的新命名、接口 UUID、限额及生命周期策略均是设计决策，不是当前仓库事实。

## 0. 目标及不可变架构决策

- D01：主程序继续使用现有 C#、WinUI 3、DI、MVVM、Windows App SDK 及发布设置；Native AOT 和非 AOT 都使用同一套 SDK 2.0 加载机制。不要切换 TFM、CsWinRT 或 Windows App SDK 版本来掩盖问题；沿目标分支现有版本构建。x64 为首个必须验证架构；其他架构只有真实测试后才能声明支持。
- D02：第一版采用受信任的 **进程内原生 WinRT 组件**。公共接口由 MIDL 3.0 IDL 定义，生成 WinMD、C++/WinRT 与 CsWinRT 投影。只保留一个 C ABI 入口用于插件对象获取；所有业务调用遵循 WinRT ABI。禁止 IPC、JSON RPC、Protobuf、WebView、Harmony、反射动态加载作为 SDK 2.0 的替代路径。JSON 仅用于 manifest 与原有配置文件等持久化数据。
- D03：C++/WinRT 插件必须可用；C# Native AOT 原生 DLL 插件的核心能力也为目标验收项，不能仅用 C++ 代替并宣称 C# 已支持。普通托管 DLL 插件不能由 AOT 宿主动态加载；不承诺旧二进制兼容。C# 插件只依赖公共 SDK，不引用宿主项目；保留熟悉的 HutaoPlugin、PluginContext、PluginSetting<T> 的 SDK 开发体验。
- D04：主程序只暴露稳定的契约，不暴露 IServiceProvider、MainWindow 实现、任意内部 ViewModel、数据库实体、IMessenger、Harmony patch。不得把共享源码或引用同一 .NET assembly 当作跨独立 AOT 二进制的对象共享机制。WinRT 引用计数和 ABI 是边界。
- D05：核心 UI 路线为进程内真实 UIElement（不是远程 UI）。必须支持程序式创建 WinUI 内置控件及现有 .hxaml 松散 XAML、事件、设置绑定和命令。完整外部编译期 XAML（独立 PRI/XBF、x:Class、x:Bind、自定义控件元数据）属于 G3 独立门槛；失败不能阻塞核心，也不能暗中引入资源 Hook。
- D06：模块首次成功映射后保持进程驻留，包括激活失败的模块。Native AOT DLL 不支持 FreeLibrary；统一策略也应用于 C++ 模块。逻辑停用必须撤销贡献和回调；真正替换、删除映射文件只能重启后完成。不宣称沙箱、故障隔离或安全热卸载。第三方原生代码和宿主享有相同进程权限，可能崩溃、挂起、访问内存。只允许运行用户明确授权的可信包；管理员模式需额外确认，不默认提升或绕过权限。
- D07：保留现有插件管理、安装包后缀、插件设置文件、通知 UI、导航框架及游戏集成边界；迁移不是对整个项目重构。仅在现有模块中进行所需的适配；不新增无关功能、不改现有本地化体系、不触发不必要的发布 CI。
- D08：SDK 2.0 公共契约冻结后只能新增接口、新 IID 或新服务，不能修改 V1 接口方法顺序、类型、参数、含义。协议/包格式版本与主程序版本分开；本文所列 SDK 2.0 只是产品版本，ABI major=1。

### 0.1 现有事实及迁移依据

| 当前源码 | 核对结果 / 迁移 |
|---|---|
| API/Model/HutaoPlugin.cs | IsEnabled、Manifest、IconPath；OnInstall、OnLoad、OnEnable、OnDisable、OnUninstall。状态由宿主拥有，不再允许插件任意修改。|
| API/Model/PluginContext.cs | 现暴露 IServiceProvider 与 IPluginService；改为按插件作用域创建的 IPluginContext。|
| Service/Plugin/PluginPackageStore.cs | `<DataDirectory>/Plugins/<id>.hutao` 与 `.hutaodisabled`；ZIP root manifest.json；保留，增强安全与事务。|
| Service/Plugin/PluginWorkspace.cs | 每进程临时工作目录及过期清理；保留模式，改用不可变版本子目录、进程租约。|
| Service/Plugin/PluginAssemblyLoader.cs / PluginAssemblyLoadContext.cs | 每插件可回收 ALC，按 PluginMainAttribute 找类型；用 NativePluginLoader 替换 SDK 2.0 路径。|
| Service/Plugin/PluginRegistry.cs / PluginService.cs | 已拆分注册表、生命周期、包存储与工作区；复用，不再创建一套平行管理系统。|
| API/PluginScopedPage.cs | 实际用 XamlReader.Load 加载 .hxaml；LoadViewModel 绑定宿主 ViewModel 生命周期。新宿主包装页和独立 SDK ViewModel 替代宿主继承。|
| Service/Plugin/PluginSettingService.cs | 实际支持 Int32、Boolean、String；文件 `<Plugins>/<id>/<name>.json`；空文件代表清除、读取默认值。类型注册类列出更多类型并不意味着后端支持。|
| UI/Xaml/View/Page/PluginPage.xaml、PluginSettingPage.xaml / 对应 VM | 保留列表、图标、启停、安装、删除、编辑设置等已有交互。|
| TestPlugin/TestPlugin.cs | Harmony 构造器 patch、私有 MainView 反射、NavigationView 添加项，ITaskContext、IMessenger、PluginSetting<int/string>。须用正式 API 复现，不保留 patch。|
| TestPlugin/TestPluginPage.hxaml / .cs / ViewModel | 计数器显示和递增，消息编辑与校验、按钮 Command、双向 TextBox Binding、主题资源。须真实复现。|
| TestPlugin.csproj / SDK.targets | 旧项目引用宿主 csproj，Build 生成 ZIP .hutao，包含 icon、lib、inject。新 SDK 独立，C# 在 Publish 后打包原生库。|
| Service/Game/Island/GameIslandInterop.cs | 现有消费者根据插件 IsEnabled、GetPluginPath 与 inject 目录识别插件附带文件。保持现有业务管线及安全边界，不在本次实现中新增、改变或绕过该机制。|

以上文件相对路径的宿主根为 `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/`；SDK 根为 `src/Snap.Hutao.Plugin.SDK/`。实施时若目标分支不同，先记录差异并映射现有类；不得静默变更本文语义。

## 1. 项目、依赖方向、产物

沿用 `src/Snap.Hutao.Plugin.SDK/` 解决方案目录，新增以下逻辑项目；具体 MSBuild 工具链实现可因现有构建结构调整，但项目职责、程序集/命名空间、公共产物不得改变。

```
src/Snap.Hutao.Plugin.SDK/
  Snap.Hutao.Plugin.Contracts/         # HutaoPlugin.idl、ABI 快照、生成脚本
  Snap.Hutao.Plugin.Projection/        # 生成 CsWinRT 投影；非业务实现
  Snap.Hutao.Plugin.SDK/               # 现有 NuGet 包升级为 2.x
  Snap.Hutao.Plugin.SDK.Native/        # C++/WinRT 头文件、bootstrap 导出辅助、MSBuild targets
  TestPlugin.Native/                  # C++/WinRT 端到端示例
  TestPlugin.Aot/                     # C# Native AOT 原生 DLL 示例
  TestPlugin/                         # 旧示例保留作迁移对照，移出 2.0 默认构建
  tests/                              # 契约快照、包验证、生命周期、Windows 集成测试
```

宿主 `Service/Plugin/` 保留 IPluginService、PluginService、PluginRegistry、PluginPackageStore、PluginWorkspace、PluginSettingService，新增 NativePluginLoader、PluginSession、PluginContextAdapter、PluginContributionRegistry、PluginNavigationAdapter、PluginUiAdapter、PluginSettingsAdapter、PluginNotificationAdapter、PluginDialogAdapter、PluginDispatcherAdapter。UI 侧新增 PluginHostPage（宿主预编译 ScopedPage 包装，实际承载外部 UIElement）。已有类命名可通过局部适配保留，不能并存两套状态真相。

依赖：宿主和插件分别引用同一份生成投影/契约，SDK facade 引用投影，投影不引用宿主；宿主不引用插件程序集、插件 WinMD 或插件项目。C++ 由相同 Contracts.winmd 生成头文件。固定 WinMD/投影包版本和 SHA256，确保两端使用相同接口定义。WinMD 不包含业务方法实现，不需要运行时反射读取插件 WinMD 来执行核心逻辑。宿主的 WinUI 运行时只有一份；插件引用相同 Windows App SDK ABI，不携带另一套 Microsoft.UI.Xaml 或 Windows App SDK native runtime。

## 2. ABI 启动入口（规范）

原生导出名区分大小写：`HutaoPluginCreate`。插件可以同时导出自身标准 WinRT `DllGetActivationFactory` 供高级 UI 使用，但核心加载只使用 HutaoPluginCreate，禁止根据语言、扩展名、命名猜测或全盘扫描选择入口。

```c
// HutaoPluginAbi.h（C 可包含；Windows x64，HRESULT 32-bit）
#include <windows.h>
#include <inspectable.h>
#include <stdint.h>
#ifdef __cplusplus
extern "C" {
#endif
__declspec(dllexport) HRESULT __cdecl HutaoPluginCreate(uint32_t abiMajor, IInspectable** plugin);
#ifdef __cplusplus
}
#endif
```

契约：
- 宿主传 `abiMajor=1`，输出指针非空且调用前置 null。版本不支持返回 `HRESULT_FROM_WIN32(ERROR_REVISION_MISMATCH)`，输出 null；空输出参数返回 E_POINTER。成功返回 S_OK 和一个拥有 +1 引用的 IInspectable*。对象必须 QI 支持 IHutaoPlugin；不允许 S_OK/null、S_FALSE 或失败/non-null（失败/non-null 视违规，尽可能释放一次，记录错误）。
- 宿主用 `LoadLibraryExW` 的绝对路径、`LOAD_LIBRARY_SEARCH_DLL_LOAD_DIR | LOAD_LIBRARY_SEARCH_SYSTEM32 | LOAD_LIBRARY_SEARCH_APPLICATION_DIR` 加载，GetProcAddress 获取唯一入口；不调用 SetDllDirectory、不更改 CWD/PATH、不扫描 lib 所有 DLL、不全局 AddDllDirectory。依赖 DLL 放入口同目录；`lib/` 保留数据/手动依赖路径，不自动加入搜索路径。预检宿主已加载模块及各已授权插件依赖文件的同名异内容冲突，冲突拒绝（系统/宿主依赖除外但不得随包覆盖）。这是供应链/误加载防护，非恶意代码沙箱。
- Windows loader 映射 DLL 前完成 manifest、哈希、权限、PE machine、包版本、重复 ID 检查；不为读取元数据而 LoadLibrary。DllMain 禁止执行插件业务、创建 UI、加载其他 DLL、等待线程等；构造器/导出只构造实例，不注册功能、不读取宿主、不创建视图。
- 宿主在主 UI STA 线程调用 Create，然后 QI IHutaoPlugin IID，再以 CsWinRT 生成投影包装正确的接口指针；按所用投影 `FromAbi`/ABI helper 的真实引用所有权合同释放临时 QI 与原始返回引用各一次，不保留借用指针。禁止普通 `GCHandle`、托管对象指针、`Marshal.GetObjectForIUnknown` 的内建 COM 路径替代 CsWinRT 投影。实际 wrapper 及引用计数写入 G1 测试；不能猜 API 名称。C++ 用 `winrt::make` 实例化、detach 对应 IInspectable 引用；C# 用 `[UnmanagedCallersOnly(EntryPoint="HutaoPluginCreate", CallConvs=[typeof(CallConvCdecl)])]`、`int` HRESULT、`nint*`，用 CsWinRT 已验证生成 ABI 转换，显式转移所有权。所有导出内部 catch 并转换 HRESULT，异常不得穿越 ABI；禁止直接返回 CLR 对象。
- 一个插件 ID 在进程中最多映射一个版本/哈希，模块映射成功后即登记，即使导出缺失、QI 失败或 OnLoad 失败也不 FreeLibrary。对象可在 disable 后重新创建（同一模块、同一版本）；不重新映射。不同版本安装仅重启生效。模块表不是启用状态表。

## 3. 公共 IDL

唯一 namespace：`Snap.Hutao.Plugin.Contracts`。MIDL 3.0 interface 隐式继承 IInspectable，不写 `: IInspectable`（这是对旧示意语法的修正）。以下是拟冻结的 IDL 正文；必须通过真正 MIDL、cppwinrt、CsWinRT 工具验证。SDK/WinUI WinMD 引用、import、编译参数和工具链要求的 `[version]`/`[contract]` 声明由 G1 按实际工具链补齐且记录；不允许改方法语义或类型以求通过。如果此精确签名在工具链不能成立，提交阻塞报告与最小修订建议，等待批准。UUID 是本文新指定固定接口 ID；首次生成后比对 WinMD，建立 golden ABI 快照。

```idl
namespace Snap.Hutao.Plugin.Contracts
{
    enum NotificationKind { Information = 0, Success = 1, Warning = 2, Error = 3 };
    enum DialogResult { None = 0, Primary = 1, Secondary = 2 };

    [uuid("790c6149-9ca4-4ec0-a1f7-71cfbe03d451")]
    interface IPluginAction
    {
        void Invoke();
    }
    [uuid("790c6149-9ca4-4ec0-a1f7-71cfbe03d452")]
    interface IPluginCommand
    {
        Windows.Foundation.IAsyncAction ExecuteAsync(String commandId);
    }
    [uuid("790c6149-9ca4-4ec0-a1f7-71cfbe03d453")]
    interface IPluginView
    {
        Microsoft.UI.Xaml.UIElement Element { get; };
        void OnClosing();
    }
    [uuid("790c6149-9ca4-4ec0-a1f7-71cfbe03d454")]
    interface IPluginViewFactory
    {
        IPluginView CreateView(String viewId);
    }
    [uuid("790c6149-9ca4-4ec0-a1f7-71cfbe03d455")]
    interface IPluginRegistration
    {
        void Revoke();
    }
    [uuid("790c6149-9ca4-4ec0-a1f7-71cfbe03d456")]
    interface IPluginSettingObserver
    {
        void OnChanged(String name);
    }
    [uuid("790c6149-9ca4-4ec0-a1f7-71cfbe03d457")]
    interface IPluginSettings
    {
        void RegisterInt32(String name, Int32 defaultValue, String description);
        void RegisterBoolean(String name, Boolean defaultValue, String description);
        void RegisterString(String name, String defaultValue, String description);
        Int32 GetInt32(String name);
        Boolean GetBoolean(String name);
        String GetString(String name);
        Windows.Foundation.IAsyncAction SetInt32Async(String name, Int32 newValue);
        Windows.Foundation.IAsyncAction SetBooleanAsync(String name, Boolean newValue);
        Windows.Foundation.IAsyncAction SetStringAsync(String name, String newValue);
        Windows.Foundation.IAsyncAction ResetAsync(String name);
        IPluginRegistration Subscribe(IPluginSettingObserver observer);
    }
    [uuid("790c6149-9ca4-4ec0-a1f7-71cfbe03d458")]
    interface IPluginNotifications
    {
        void Show(NotificationKind kind, String message);
    }
    [uuid("790c6149-9ca4-4ec0-a1f7-71cfbe03d459")]
    interface IPluginDispatcher
    {
        Boolean HasThreadAccess { get; };
        Windows.Foundation.IAsyncAction InvokeAsync(IPluginAction action);
    }
    [uuid("790c6149-9ca4-4ec0-a1f7-71cfbe03d45a")]
    interface IPluginDialogs
    {
        Windows.Foundation.IAsyncOperation<DialogResult> ShowMessageAsync(String title, String message, String primaryText, String secondaryText);
    }
    [uuid("790c6149-9ca4-4ec0-a1f7-71cfbe03d45b")]
    interface IPluginContributions
    {
        IPluginRegistration RegisterCommand(String commandId, String title, IPluginCommand command);
        IPluginRegistration RegisterNavigationItem(String itemId, String title, String iconGlyph, String viewId);
        IPluginRegistration RegisterView(String viewId, IPluginViewFactory factory);
    }
    [uuid("790c6149-9ca4-4ec0-a1f7-71cfbe03d45c")]
    interface IPluginUi
    {
        Microsoft.UI.Xaml.UIElement LoadXaml(String relativePath);
        Microsoft.UI.Xaml.UIElement ParseXaml(String markup);
    }
    [uuid("790c6149-9ca4-4ec0-a1f7-71cfbe03d45d")]
    interface IPluginContext
    {
        String PluginId { get; };
        String PluginName { get; };
        String PluginVersion { get; };
        String PackageDirectory { get; };
        String DataDirectory { get; };
        UInt32 AbiMajor { get; };
        UInt32 ApiMinor { get; };
        IPluginSettings Settings { get; };
        IPluginNotifications Notifications { get; };
        IPluginDispatcher Dispatcher { get; };
        IPluginDialogs Dialogs { get; };
        IPluginContributions Contributions { get; };
        IPluginUi Ui { get; };
        void Log(NotificationKind severity, String message);
        Object GetService(Guid serviceId);
    }
    [uuid("790c6149-9ca4-4ec0-a1f7-71cfbe03d45e")]
    interface IHutaoPlugin
    {
        void OnLoad(IPluginContext context);
        void OnInstall();
        void OnEnable();
        void OnDisable();
        void OnUninstall();
    }
}
```

### 3.1 语义与版本规则

- 所有上述接口是固定 IID 的非 exclusiveto 公共接口，可由宿主或插件实现。不要求 runtimeclass 用于每个接口；如插件需要自己的 WinRT runtimeclass，可以定义独立 IDL，不得扩充公共 V1。不要把这些接口替换成 .NET `GeneratedComInterface` 的 IUnknown 接口（不是同一 WinRT 投影契约）。
- `IPluginContext.GetService(Guid)` 是后续能力发现入口。V1 返回的已知服务 ID 等于上述各 service 接口 IID；只返回当前插件有权访问且宿主支持的接口（投影后使用 QI/as）；未知或不可用返回 null。其余 Context 属性为同一对象的强类型捷径。不接受字符串类型名/反射查找/任意 IServiceProvider。V1 ApiMinor=0；新增接口而非扩展现有接口布局。服务实现可单独新增版本，不需重新编译已有插件。
- IDL 的 `Object` 是 IInspectable，不是任意 CLR object。String 为 HSTRING 投影，空字符串是合法值；不以 null/空区别表示配置缺失。需要可空/复杂类型时未来另加接口，禁止跨边界传入 System.Type、泛型 CLR 接口、Task、委托、异常、Span、nint 包装的托管引用等。
- 所有接口调用默认在宿主主 UI STA（Single-Threaded Apartment，单线程 COM 单元）执行。后台线程不能直接调用 Context/Settings/Ui/Dialogs/Contributions。Dispatcher.InvokeAsync 是唯一允许从 worker 发起的入口：G1 必须验证接口对象 agile / 正确封送（通过 IAgileObject 或经验证的 agile reference），同时验证回调对象封送；若不能成立，不得用错误的跨线程指针绕过，G1 阻塞。HasThreadAccess 同样可跨线程读取；其余属性（含路径）由插件在 UI 线程取得后缓存普通字符串。宿主应在错误线程返回 RPC_E_WRONG_THREAD，不自动同步等待 DispatcherQueue。
- UI 生命周期方法同步、必须快速返回，不做长 IO/同步等待、不在 DLL 入口启动工作线程。异步命令通过 WinRT IAsyncAction；C# SDK 可用 Task/ValueTask 包装，仅在插件内部使用，通过经过验证的 AOT 兼容 WinRT async 适配器桥接，不动态生成代码。生命周期完成条件由同步返回表示，不允许 async void。
- 宿主 `await` 时允许 UI 消息正常运行，不允许持有全局锁或 per-plugin monitor 跨 await、COM/插件回调。不要 `.Wait()`、`.Result`、`GetAwaiter().GetResult()`、`DispatcherQueue` 同步等待。
- 错误：参数非法 E_INVALIDARG；未注册设置/ID 找不到 HRESULT_FROM_WIN32(ERROR_NOT_FOUND)；重复注册 HRESULT_FROM_WIN32(ERROR_ALREADY_EXISTS)；未授权 E_ACCESSDENIED；对象/会话已关闭 RO_E_CLOSED；取消 HRESULT_FROM_WIN32(ERROR_CANCELLED) 或标准 WinRT Canceled 状态；其他错误保留 HRESULT 并记录插件 ID、版本、阶段。插件异常由 WinRT 生成 stub 封送；不得通过 C++ exception 越过 ABI。原生访问违规、死锁不可安全捕获恢复，进程内架构不承诺继续运行。

## 4. Manifest 与包布局

### 4.1 格式

保持 ZIP `.hutao` 与 `.hutaodisabled`，`manifest.json` 必须为根目录唯一 UTF-8 文件，无 BOM 或接受 UTF-8 BOM。旧 manifest 缺 `schemaVersion` 或 schemaVersion=1 被识别为 legacy，见 §10。V2 示例：

```json
{
  "schemaVersion": 2,
  "id": "TestPlugin",
  "name": "Test Plugin",
  "version": { "major": 2, "minor": 0, "patch": 0 },
  "author": ["SnapHutaoRemasteringProject"],
  "description": "SDK 2.0 test plugin",
  "iconPath": "icon.png",
  "api": { "major": 1, "minMinor": 0 },
  "minHostVersion": "1.0.0",
  "runtime": { "kind": "winrt-native", "architecture": "x64", "entry": "bin/x64/TestPlugin.dll" },
  "ui": { "mode": "loose" },
  "capabilities": ["settings", "notifications", "navigation", "commands", "dialogs", "xaml"],
  "files": [ { "path": "bin/x64/TestPlugin.dll", "sha256": "<64 lowercase hex characters>" } ]
}
```

例中 files 为结构示意，实际包必须列出除 manifest.json 外全部常规文件的真实哈希（包括 icon、hxaml、依赖等），SDK 打包器自动生成。minHostVersion 示例不是当前 host 真实最低版本承诺；首个发布值由目标分支实际构建验证后固定并记录。未知顶层扩展字段可忽略，未知 schema、runtime.kind、ui.mode、capability、架构拒绝，不猜测默认值。严格拒绝 JSON 重复属性、大小写不同的重复文件路径、超范围数值。`id` 保持旧 TestPlugin 拼写，ASCII `[A-Za-z0-9][A-Za-z0-9._-]{0,127}`，拒绝 `..` 路径段、结尾点及 Windows 设备名；唯一性 OrdinalIgnoreCase，文件名使用首次持久化的规范拼写。name 1–128 字符，author 至少一项（每项 ≤128，最多 16），description ≤4096，三个版本整数 0..Int32.MaxValue。minHostVersion 3 段非负整数，和宿主产品版本前三段数值比较，不使用 commit hash 作为版本。若 major!=1/minMinor>0/host版本过低/PE 与进程架构不符，展示 Incompatible 而不执行。architecture V1 只允许 x64；增加架构需更新规范与测试矩阵。

ui.mode 枚举 `none`、`loose`、`programmatic`、`compiled`。loose 允许程序创建控件与松散 XAML；programmatic 仅程序创建；none 不允许导航/视图；compiled 只有 G3 通过并由用户批准后才启用，否则拒绝而不是静默降级。capabilities 是 SDK 功能权限，不限制原生代码本身；无对应能力的 Adapter 必须拒绝。UI Dispatcher、Log、生命周期元数据不需单独权限。`files` 为完整内容清单，校验所有条目哈希，不把哈希误当签名/发布者认证。

```
TestPlugin.hutao (ZIP)
  manifest.json
  icon.png                  # 可选
  bin/x64/TestPlugin.dll    # 必须，PE 原生 DLL
  bin/x64/Dependency.dll    # 可选，同目录依赖
  views/TestPluginPage.hxaml
  assets/...
  lib/...
  inject/...                # 仅保留现有兼容数据目录及现有业务路径
```

### 4.2 存储、路径、信任与部署

- 继续用 `HutaoRuntime.DataDirectory/Plugins/<id>.hutao` 或 `.hutaodisabled` 表示用户意图；一 ID 只能有一种后缀。已有双文件冲突显示 Conflict，不自动执行或删除，用户解决后再启用；不再默认为 enabled 获胜。安装重复 ID（任一后缀）拒绝；更新使用显式 Update 操作（V1 可先提供独立命令/测试，不把重复安装当更新）。
- `Plugins/<id>/<settingName>.json` 保留已有设置；plugin-private data 放 `Plugins/<id>/data/`。宿主内部状态放 `Plugins/.sdk2-state/<canonicalId>/state.json`，含 approvedPackageSha256、installedVersion、installHookCompletedVersion、pendingUpdate、lastError、recovery state、desiredEnabled 的事务辅助信息。后缀是持久化启用状态真相；state 不可另行覆盖后缀；不一致进入 recovery。信任以整个 ZIP SHA256+ID+version 为准，任何字节变更需重新授权。SDK 可验证 Authenticode 作为额外信号，不声称签名即可信；不做联网市场或证书强制。本地首次安装/启用 v2 包时明确提示任意原生代码风险，展示实际 SHA256，默认不执行；已授权未变更包可自动启动。旧包显示 Incompatible；不运行历史注入或其它 DLL。管理员模式每次进程启动额外确认，默认该会话不执行。
- 工作区沿用 `%TEMP%/SnapHutaoRemastered/plugins/<pid>_<guid>/<id>/<version>-<zipHash>/`，绝对规范化，私有 ACL（至少不授予其他普通用户）；一代映射后工作区不可覆盖/删除；图标可复制到宿主管理的非执行 metadata 缓存使 disabled/legacy 不必解压整包。仅回收明确不再被任何运行实例占用的目录；进程租约持有独占锁，PID 与启动时间用于诊断，PID 重用/无权限查询不作为删除依据。插件路径 API 只对 Active session 返回该工作区路径；禁用、失败、pending 删除均不返回给现有消费者。
- 解包到新 staging，不使用 `ExtractToDirectory(overwriteFiles:true)` 处理不可信输入。检查 entry 数 ≤4096、单文件 ≤256 MiB、累计解压 ≤512 MiB、ZIP 文件 ≤256 MiB、manifest ≤256 KiB、hxaml ≤1 MiB、图标 ≤4 MiB。逐条流式计数（不能只信头部）；拒绝绝对路径、`..`、`.`、空路径段、反斜杠、冒号/ADS、设备名、末尾空格/点、符号链接/重解析点、重复/case collision、任何规范化后逃逸工作区路径、压缩格式无效；检查目录祖先非重解析点；不得覆盖存在文件。ZIP 目录项可选，不计入 files；解析时保护大小/深度。图标需安全解码、无效用现有默认图标。对有权限的同用户原生恶意进程仍非绝对防御。
- 包写入/替换/重命名及 journal 使用同卷临时文件 + flush + 原子 rename/replace；操作锁必须覆盖同一 DataDirectory 的并行实例（按 canonical data root 的命名 mutex 或独占文件锁，记录实际选择）。所有中间状态 journal 含原始包位置/hash/目标/阶段；恢复规则：未 commit 恢复原后缀和旧包，已 commit 保留新版，ambiguous 冲突保留文件并禁止执行，不自动删除用户数据。不要在跨进程锁内调用插件代码；先记录 transaction reservation，释放锁执行插件，提交前重新获取锁验证版本及 reservation。失败后不遗留已启用/加载标记。
- 入口 DLL 已映射后原生文件不可覆盖。Update：仅已授权新包且同 ID、版本更高，stage 并记录 `PendingRestart`; 当前版本仍可工作，不在当前进程加载新版本；下次干净启动替换且保留旧包备份直至新版本 OnLoad+OnEnable 成功。失败禁止自动运行失败的新版本并可显式回滚旧包；回滚如已映射新模块须再重启。安装失败不丢旧包和设置。未映射插件也统一采用重启更新，避免不同语义。
- DLL 内含完整 native code；SDK 禁止打包宿主 EXE、宿主 DLL、WinRT.Runtime 托管库、WinUI native runtime、Windows App SDK runtime 的副本。禁止依赖运行时 `Assembly.Load`。依赖由各插件实际链接/Native AOT 发布生成，不能把普通 IL DLL 当 entry。PDB 可在开发输出保存，不默认打入包。

## 5. 生命周期：状态、执行顺序、回滚

### 5.1 状态模型

拆分 `DesiredEnabled`（后缀）、`RuntimeState`（Discovered、Disabled、Activating、Active、Disabling、Failed、Incompatible、Conflict、PendingRestart、Removed）和 `ModuleResident`（映射表）。`PluginInfo.IsEnabled` 的现有消费者迁移为**仅 Active 且贡献已 commit**，新增 DesiredEnabled 供管理界面开关。状态变更在 UI 线程通知 `PluginsChanged`；一次事务只通知最终快照；过渡状态用于禁用管理按钮。失败原因、版本、是否需要重启单独展示。不要以模块驻留作为启用。对外不能获得 `HutaoPlugin` CLR 实例列表；内部 `GetAllPlugins()` 调用者改为元数据快照。`TryGetPluginLoadContext` 在 native 记录无意义：保留过渡签名返回 false 或迁移删除所有内部调用（不得伪造 WeakReference）。

### 5.2 操作规则（唯一语义）

每 ID 用 async operation gate，互斥 install/enable/disable/uninstall/update/command-start；跨不同 ID 不用全局同步锁执行业务。回调中拒绝重入生命周期，不阻塞等待自身。UI 线程执行生命周期及 UI 注册；IO 背景执行。所有动态注册属于 session generation（单调递增）、scope 管理，使用 context 内嵌 ID 不能由插件参数伪造。

| 操作 | 顺序及提交 |
|---|---|
| 启动扫描 | 只读取包 metadata；恢复 journal；禁用/legacy/incompatible 只展示；授权且 enabled 的包按 ID 排序尝试激活；主窗口与导航就绪后逐个执行（不得在 App 构造器或 DllMain 调用）。失败不阻断其他插件（可捕获错误范围内）。|
| 安装 `.hutao` | 校验、授权、staging、预留事务 → Create → OnLoad(context) → 若该版本 install marker 未完成调用 OnInstall → OnEnable → 验证 provisional contribution scope → 提交包后缀与 journal → 同一 UI dispatch 提交 scope 和 Active（提交失败回滚后缀并撤销 scope）。若任一步失败，撤销贡献、关闭 session、保留模块，移除未提交包或保留为 disabled 且标 Failed（规范选择：保留已校验的包为 `.hutaodisabled`，以便重试；授权/预检拒绝不写入安装包）。|
| 安装 `.hutaodisabled` | 校验后仅持久化 disabled 包和 metadata；不 LoadLibrary、不 OnInstall、不要求立即授权执行；首次启用前授权并调用 OnInstall。|
| 首次/再次启用 | 已 Active 返回成功不重复 hook/注册；校验授权/兼容、获取或映射固定模块，Create 新实例、OnLoad、新版本未完成安装 hook 时 OnInstall、OnEnable；成功提交 enabled 后缀和 journal，再在同一 UI dispatch 提交贡献、Active。提交失败回滚后缀与 scope。失败保留 disabled（启动时原 enabled 意图也转 disabled 并记录原因）。|
| 禁用 | 标记 Disabling、阻止新命令和导航，关闭插件视图与对话框，取消 in-flight async 操作并等待有界协作完成（见 §5.3），调用 OnDisable 一次（只对 OnEnable 已进入的实例），finally 反向撤销贡献/订阅、断开 context、释放宿主持有实例引用。提交 `.hutaodisabled`，保留 module/workspace 和设置。即使 OnDisable 抛可捕获异常，也完成逻辑撤销；显示 Disabled with error/需要重启，绝不恢复半初始化 Active。持久化失败仍停止执行，标 Failed + recovery journal，禁止下次无提示自启。|
| 卸载 Active | 同 disable 流程；在 context 最终关闭前以 cleanup phase 调 OnUninstall（OnDisable 后）；失败仍撤销宿主贡献，保留禁用包并报告失败，允许重试删除但不自动执行旧回调。成功删除 ZIP、移除 metadata；驻留工作区仅下次清理。|
| 卸载 Disabled/Failed/Legacy | 纯包管理操作，不为卸载重新执行第三方代码；明确标记 OnUninstall skipped。保留设置和 data（显式用户选择清除才删除）。已经映射的模块标 Removed/PendingRestart，禁止当前进程安装同 ID 不同内容。|
| 退出 | 尝试按 Active 逆激活顺序逻辑关闭，每实例一次 OnDisable，不改用户持久化 enabled 意图；不 OnUninstall、不 FreeLibrary；异常记录。|

OnInstall 表示版本安装初始化，首次启用且 OnLoad 成功后、OnEnable 前执行；marker 按 ID+版本+package SHA256 持久化，hook 成功后记载；失败则下次可重试。跨进程崩溃无法保证第三方外部副作用 exactly-once（一次且仅一次），SDK 文档要求 hook 幂等；明确不同于旧实现 install 在 load/enable 后调用。OnLoad/OnEnable 每次新实例一次；OnDisable 只调用一次且允许部分初始化清理；OnLoad/OnInstall 失败且 OnEnable 未进入时不 OnDisable，宿主关闭 scope；插件必须自行确保其失败路径释放自有资源。OnUninstall 是 best-effort 业务回调，不是删除包的必要前提（禁用不执行代码）。

### 5.3 Scope / async / 重入

- `PluginSession` 仅宿主内部持有 plugin projection、context、generation、module ref、registration scope、打开视图、对话框、在途 IAsyncInfo、CTS。Context 不能强持有 PluginSession 本体/宿主 UI（避免环）；adapter 持有可关闭 lease，lease 间接检查 generation。关闭后只允许 Log（直到进程退出，记录 rate limit）和已复制字符串，服务调用均 RO_E_CLOSED；不得通过旧 adapter 注册新 generation。
- OnLoad/OnInstall/OnEnable 期间允许注册设置、读取自己的设置（仅注册默认值；Set/Reset 在 Active 才允许）、Log，UI/command contributions 在 OnLoad/OnEnable 记录为 provisional，不可见且不可执行。OnInstall 不注册贡献；OnLoad 通知缓存，commit 后发送，失败丢弃；对话框/页面创建只在 Active；OnDisable/OnUninstall cleanup phase 只允许 Log、读取设置、撤销 token、释放已有资源，不允许新命令/视图/任务/对话框/通知。激活 hook 成功且包事务提交后在同 UI dispatch 事务提交所有贡献，校验 navigation viewId 已注册、所有 ID 唯一；失败全部撤销。
- Active 允许动态注册（单操作原子可见），撤销 View 时同时移除引用它的导航项并关闭打开视图；Token.Revoke 幂等、UI 线程执行，token 被 GC 不自动撤销。host scope 最终负责统一清理；从任何失败/禁用路径释放 token 和回调强引用。禁止只删除导航项而留下事件、页面、async callback。
- `ExecuteAsync`、`Dialogs.ShowMessageAsync`、Dispatcher.InvokeAsync、Settings.Set/Reset 都以 IAsyncAction/IAsyncOperation 传递；调用和完成可捕获异常。命令按 ID 单并发，执行中禁用触发；不使用同步跨线程调用。禁用时取消宿主持有的 IAsyncInfo，并由插件自己负责取消自建任务；5 秒协作 drain（异步等待 UI 可运行），超时不强杀线程或释放仍在执行的对象，移除 UI/授权并标 `PendingRestart`/禁止再次启用，保留所有仍在用的对象/module 直至任务完成或退出。同步 hook 卡死不可能由同 UI 线程 timeout 安全中断；写 crash/start marker、下一次安全启动提供禁用入口，不承诺当前进程恢复。
- Dispatcher.InvokeAsync：即使已在 UI 线程也 enqueue（不得 inline 重入），返回 task/WinRT action；回调前重新检查 lease，禁用后拒绝/取消；队列失败返回 RO_E_CLOSED；插件回调异常转 HRESULT；不在关闭后留待下一代执行。后台线程不得直接接触 UIElement。

## 6. 正式扩展点和 UI

### 6.1 导航、命令、页面

Contribution identity = canonical plugin ID + `/` + localId；localId `[A-Za-z0-9][A-Za-z0-9._-]{0,63}`，大小写不敏感；每种 contribution 独立 ID 域。每插件最多 64 命令、16 导航、16 视图、128 settings、32 observers；标题长度 1..128。IconGlyph 为非空单个 Unicode scalar（Segoe Fluent Icons 兼容）或空字符串使用默认图标。命令只注册到插件自有功能，不可假设当前主程序不存在的业务菜单。V1 显式扩展点：主导航 `navigation/main`（现有 NavigationView）、插件命令注册表（宿主插件管理区的命令入口，或 SDK 页面调用其绑定回调）。未来 context menu/toolbar/业务服务通过新增契约支持，不允许随意扫描控件树。

宿主 Navigation adapter 使用现有导航服务：由宿主维护 NavigationViewItem、Tag=内部 composite ID、排序（内置项原顺序不变；插件项按 pluginId、itemId OrdinalIgnoreCase 排在约定的插件分组），统一处理点击与选中。主窗口/ MainView 重建时通过注册表快照重建 items，先移除旧事件订阅、绑定新 window generation；不使用 Harmony 构造器 patch、VisualTreeHelper 寻找 Frame、直接操作内部字段。导航使用宿主预编译 `PluginHostPage` + pluginId/viewId 参数，禁止 `Frame.Navigate(typeof(未知插件类型))`。宿主仍负责页面历史、选中状态、返回键。

打开时在主 UI thread 调工厂 `CreateView(viewId)` 返回非 null IPluginView，并取非 null UIElement。每次进入创建新 view（不缓存实例，不跨窗口重用）；该元素必须无现有父级、属于当前 UI thread；不能返回宿主窗口、现有主界面控件或跨线程创建的树；XamlRoot 由容器在加入后确定。宿主保存 IPluginView 和 Element 直至离开。页面在导航离开、窗口关闭、disable、uninstall、注册撤销时：停止输入/取消该 view 绑定的 host 操作；调用 OnClosing 一次（即使抛异常仍清理）；从 ContentPresenter 移除 Element、取消宿主 event handler、释放本地 view reference；清理导航历史中对该 session 的条目；若当前显示插件页，返回现有安全内置页。禁止将插件 Page 对象放入宿主 Frame 的缓存或 DI scope。插件自己负责其控件事件与自建任务清理。Host 不假设可以销毁仍被插件内部持有的对象。

### 6.2 松散 XAML 和原生控件（G2 必须）

保留 `.hxaml`。`IPluginUi.LoadXaml(relativePath)` 从插件工作区读 UTF-8（≤1 MiB），路径必须在 manifest files 内，禁止绝对路径/逃逸/reparse；在 UI 线程执行 `Microsoft.UI.Xaml.Markup.XamlReader.Load`，结果必须 UIElement。`ParseXaml` 接受插件内部字符串同大小限制；失败返回 HRESULT（宿主包装页显示本地化错误及插件 ID，不伪造成功）。仅受信任插件可执行此操作；XamlReader 不是沙箱。

V1 测试支持范围：Grid、StackPanel、Border、TextBlock、TextBox、Button、ToggleSwitch、CheckBox、ComboBox、ListView、ScrollViewer 等 WinUI 公开控件，标准布局、ThemeResource（宿主已知资源）、局部 ResourceDictionary、普通 Binding、DataTemplate（实际验证）。`.hxaml` 禁止 x:Class、x:Bind、x:Load、XAML event handler attributes、未知插件自定义类型、未验证外部 ResourceDictionary Source、插件自行声明宿主 private type 的 using。所有已承诺的控件/资源必须纳入 Windows 测试；宿主无需通过反射发现动态类型。允许插件在 UI thread 程序式创建 WinUI 控件，不要求所有页面使用 .hxaml。

**现有 TestPlugin 的最小可靠迁移**：保留视觉布局和主题资源，将关键控件赋予 x:Name（CounterText、ShowCounterButton、IncrementCounterButton、MessageText、MessageBox、UpdateMessageButton），插件工厂拿到宿主解析的同一个 UIElement，通过 FrameworkElement.FindName 取得已知控件投影，代码连接 Click/TextChanged，显示设置值、使用 async settings 写入、刷新显示。SDK facade 提供强类型 view helper（例如 GetNamedElement<T>，基于已知 WinRT 接口投影/QI，不用运行时反射生成类型）；这一实现能保留 C#/C++ 的原生 code-behind 风格。关闭 view 时移除事件 token/delegate、取消任务、清空引用。不要将这一方案描述为已经支持任意第三方绑定类型。

**MVVM/Binding 兼容（G2 必须验证）**：提供 SDK 编译期已知、可 AOT 封送的状态容器或 plugin-side 生成的 `ICustomPropertyProvider` 与 `INotifyPropertyChanged`，以及可投影 ICommand 的实现；不依赖 reflection。C# 示例 ViewModel 用 `[GeneratedBindableCustomProperty]` partial class（以当前 CsWinRT 版本实际 API 为准），预生成 Counter/Message 属性、通知、Command，DataContext 由插件设置在返回的 UIElement 上；宿主只看到 UIElement / IInspectable，不需要插件 ViewModel 的 CLR 类型。必须验证跨独立 AOT 模块 CCW（托管对象 COM 包装）与 RCW（原生对象托管包装）正确封送。C++ 版本可直接使用程序式更新事件；如支持任意 `{Binding}` 自定义属性，需独立实现 WinRT 属性 provider 或编译期 metadata 并验证。若跨 AOT 模块 Binding 失败，先保持事件版本完整可用，明确 G2-B 未通过，不假造 Binding 已实现，不偷偷改成 JSON Remote UI；向用户提交技术阻塞。对于 `TestPlugin` 的最终 C# 版本，普通 Binding、INotifyPropertyChanged、ICommand、TwoWay UpdateSourceTrigger=PropertyChanged 是独立验收项，不是仅展示静态界面。

### 6.3 完整编译期 XAML（G3）

2026-11-27 实现更新：C++ PrototypeHost 已实测 PRI 内嵌 XBF 页面及 Click/ABI；按用户要求生产宿主已接入，正式 Windows 回归仍是发布门槛。插件包只部署同名组件 PRI，不允许独立 .xbf。C# compiled 尚未实现，保留 .xaml loose。下文为原设计验收目标，具体当前状态见 PluginSDK2-Testing.md。

插件自定义 WinRT Page/UserControl 可以用其自身 IDL、XAML、code-behind、WinMD 及 PRI/XBF 构建，工厂 CreateView 通过 UIElement 交接，宿主不用认识具体 class。研究顺序：程序式 C++/WinRT 控件 → 插件 compiled UserControl → XBF/PRI 资源发现 → 自定义控件元数据/绑定/主题资源 → C# Native AOT compiled WinRT component。动态激活可由插件内部直接构造 runtimeclass 或标准 DllGetActivationFactory；不修改核心入口。验证 ResourceManager(path) 只实例化资源管理器不等同把资源自动加入 WinUI 当前资源图；必须验证实际 InitializeComponent、主题切换、x:Bind、事件、名称作用域、不同插件同名资源。参考 DynamicXAML，但其 DynamicLoader.cpp 使用 Detours 及固定 vtable 索引，禁止将它的资源 Hook 作为生产必需；禁止修改 WinUI 内部函数、硬编码偏移或私有 ABI、全局 XAML metadata 注入猜测。若受支持 API 无法满足，则 compiled mode 保持拒绝，附最小复现与上游问题链接，绝不悄悄降级。C# compiled WinRT component 的 AOT authoring 单独验证，不能由 C++ 成功推出。

## 7. Settings、通知、对话框与服务适配

### 7.1 设置行为

现有 JSON 文件 `<Plugins>/<id>/<name>.json` 保持兼容。注册名沿用 localId 规则（大小写不敏感，首次注册的实际文件名保留），同一 ID 不能跨类型重用；description ≤4096，string 值最多 64 KiB UTF-8。设置支持 Int32、Boolean、String，类型不可隐式转换（`"42"` 不是 Int32，`0` 不是 Boolean）。String 为非 null HSTRING，空字符串是合法值；Reset 恢复 default 而非空字符串。既有空文件、JSON null、缺文件均映射为注册默认值，不写回直到用户显式修改；无效 JSON 或类型不符显示错误/警告并保留文件，不静默覆盖。JSON source-generated context 或显式固定类型读写，禁止使用 `JsonSerializer.Deserialize(json, arbitraryType)` 作为新 ABI 动态序列化方式。

- 激活前后台读取该插件设置文件并构造只含原始受限 JSON/已解析固定类型的快照。`Register*` 在 UI thread 仅从快照读取、建立默认值与类型的 metadata，不同步磁盘 IO。启用过程中注册同名两次直接拒绝（不覆盖默认值），同一版本不同实例允许重新注册；既有有效值优先于默认。`Get*` 返回已注册类型的最新已提交缓存值；无注册报 NOT_FOUND。OnLoad/OnInstall/OnEnable 内同步读可用，异步写入只在 Active（不得同步等待）。与 §5.3 一致，激活阶段不允许 Set/Reset 启动写入。
- 每个 setting 按顺序串行写入：validate → 后台创建 temp、序列化、flush、atomic replace → 切回 UI 更新 cache → 发观察事件 → 完成 IAsyncAction。成功之前 cache 不变；磁盘失败返回错误并保持旧值。Reset 以原有空文件清除语义持久化并更新 cache 为默认值。每次写入检查 generation，在 IO 开始前可取消；已经 durable commit 的写入必须更新宿主真实 cache，关闭会话时抑制旧订阅事件（不假装写入没发生）。不跨 await 持锁调用插件；注册表或旧 UI 类型转换不能绕过固定类型验证。读写同一设置的并发、重启、null/empty、取消、IO failure 都要测试。
- 设置 UI 使用现有 PluginSettingPage、PluginSettingViewModel、EditableSettingItem，并根据三种固定类型显示原生编辑控件。页面能编辑 disabled 插件的已知设置：将成功注册的 descriptors（name/type/default/description）存到 `.sdk2-state/<id>/settings.json`，非代码缓存；disabled package 从未激活时不猜测设置，展示“首次启用后可配置”。metadata 以版本和包哈希关联；不因旧文件修改而执行插件。设置页面和 SDK 同用一套 typed backend，不能维护平行 cache。
- Subscribe 返回 IPluginRegistration，按会话 generation 管理；成功提交后在 UI thread 通知 observer.OnChanged(name)，仅值变化时触发；订阅回调异常记录且不撤销已持久化值；禁用/关闭自动撤销。通知不发生在写锁内；防同步重入写入（写请求排队，不阻塞）。禁用设置 UI 编辑不调用插件。SDK `PluginSetting<T>` 仅支持 int/bool/string，使用 `typeof(T)` 的固定分支和静态投影；不能泛化到任意 DTO，注册只使用自己的 context；公开 `Value` 只读或读 committed 值，提供 `SetValueAsync` 和 `ResetAsync`，不得沿用原 fire-and-forget setter 伪装写入成功。C# SDK 如提供属性 setter 语法糖，必须显式报告 pending/error 并回滚 UI（不列入 V1 必须项）。
- TestPlugin Counter=0 / Message="Default Message"，标题、描述沿用原样；升级保留原同名文件及值。Message 页面输入为编辑草稿，TwoWay Binding 与 `Update Message` 按钮分离：空白字符串触发 Warning，合法字符串提交并显示成功；TextBox 每键更新 ViewModel draft 不必每键 IO，Counter 只在写入成功后显示新值，避免假成功。此处相比原样例即时 property setter 是明确行为修正，不改变用户可达功能。

### 7.2 通知、对话框、日志

- IPluginNotifications.Show 使用现有宿主 `IMessenger` → `InfoBarMessage` 路径，由 adapter 做四种 severity 映射、统一前缀 `[PluginName]`；禁止插件访问 messenger/服务 locator。message 非空、≤4096 字符；单插件最多每 10 秒 10 条，超出丢弃并写聚合日志；激活时暂存最多 10 条，失败不发送；Active 才发。不得创建额外的顶层 Window 或使用全局 Growl 窗口替代现有 InfoBar。
- IPluginDialogs.ShowMessageAsync 使用现有宿主 `IContentDialogFactory`/主窗口 XamlRoot，在当前 UI thread 排队显示。title ≤128、message ≤4096、primaryText 1..64、secondaryText 0..64（空不显示）。标准结果 None/Primary/Secondary，关闭/取消通过 WinRT async 状态或 None 区分：用户关闭/取消返回 None，会话禁用/操作 Cancel 返回 Canceled。不得给插件宿主 ContentDialog 对象/内部工厂实例；宿主负责对话框关闭、取消、清理和串行化（全局同时最多一个，排队会话失效立即取消）。V1 不支持任意自定义对话框 content，真实页面本身仍可使用受信任插件自有 UI；未来另加契约。
- IPluginContext.Log 把 pluginId/version/generation/phase 加入现有 ILogger/Sentry breadcrumbs，severity 映射；消息最大 4096 字符，避免记录设置值、凭据、Cookie、用户数据、绝对私人路径。默认本地日志，不能自动将插件提供的敏感内容上传遥测；错误包含 HRESULT、操作名、SDK/host version，调试可含本地堆栈。

### 7.3 其他宿主服务

V1 只发布上述公共服务，不为将来想象的 API 编造接口或返回值。现有内部 ITaskContext、IContentDialogFactory、IPluginSettingService、IMessenger 分别由 dispatcher/dialogs/settings/notifications 适配。后续真正需要账户、游戏业务、下载、命令菜单等能力时，先审查真实 service 与权限，定义独立 versioned IDL（不将内部服务直接暴露、不得传认证 token/Cookie）。`GetService` 是扩展入口，不是万能 service locator。可扩展性来自接口版本化和明确扩展点，不来自允许任意读取宿主内部类型。

## 8. SDK 开发体验与打包工具

### 8.1 C++/WinRT

SDK 包提供固定 Contracts.winmd、由它生成的 C++/WinRT 投影头文件、HutaoPluginAbi.h、一个 MSBuild .props/.targets（引用路径及一致 WinUI 版本），以及最小模板。模板用 `winrt::implements<Implementation, IHutaoPlugin>` 实现插件对象；`IPluginViewFactory`/`IPluginView` 等同理，内部可用正常 C++ 类、STL、WinRT 类型。核心 DLL 无需注册全局 WinRT 激活工厂；只有插件自行公开 runtimeclass 时才由对应 C++/WinRT 组件工具链产生其 WinMD 与 DllGetActivationFactory。SDK 导出入口负责 ABI version check、创建实例、返回 owned IInspectable 引用、转换异常。不要要求每个接口实现都必须是一条 runtimeclass。

### 8.2 C# Native AOT

SDK NuGet 版本建议 2.0.0（正式发布前确认），引用生成的 Contracts 投影，并以公共 facade 提供 `HutaoPlugin` 抽象基类、`PluginContext` 强类型封装、`PluginSetting<T>`、view/registration helper。宿主不引用 facade 的插件实现类；facade 可以在插件内部管理普通 C# 对象、Task、泛型、System.IO 等。基类生命周期签名：`public abstract void OnLoad(PluginContext context); public virtual void OnInstall(); public virtual void OnEnable(); public virtual void OnDisable(); public virtual void OnUninstall();`（后三者与 OnInstall 默认无操作），内部由生成/手写 adapter 实现 IHutaoPlugin 并转调 facade；adapter 的 WinRT CCW 必须 AOT 可生成。宿主拥有 IsEnabled、Manifest、IconPath；插件读取 Context 对应 metadata，不可设置宿主状态。不要把公共 facade 类型作为 ABI 类型。

模板为当前仓库支持的 .NET TFM（不擅自升级/降级）及 Windows x64，项目为原生共享库发布：`PublishAot=true`, `NativeLib=Shared`, `RuntimeIdentifier=win-x64`, `AllowUnsafeBlocks=true`；按实际 .NET 版本验证 Native AOT 库的项目配置，入口由 SDK 构建时生成或模板静态代码。C# Native AOT 只能通过已验证的 `UnmanagedCallersOnly` 导出、CsWinRT ABI 投影交互。不要使用 `AssemblyLoadContext`、运行时代码生成、依赖 JIT、反射扫描 `[PluginMain]`。可选编译期生成器识别单一入口类（必须唯一且可创建），不作为首版阻塞条件，首版模板明确注册 `new TestPlugin()`。禁止为通过 AOT 构建全局 suppress IL2026/IL3050/IL2104 等警告；记录依赖诊断并解决真实问题。

打包在 C++ Link 成功或 C# `dotnet publish -c Release -r win-x64` 成功之后，从 **明确发布产物路径** 收集 native entry、指定 native dependencies、assets、views、icon、lib/inject data；不能沿用普通 Build 的 `$(TargetPath)` 当作 native entry。manifest 模板由项目维护，打包器填真实 files hash、验证 PE、路径/版本/能力；输出 `<id>.hutao`（`.hutaodisabled` 由用户/宿主管理）。禁止打包整个引用树、`../` 路径或重复依赖。SDK 打包输出确定性：按 ordinal 路径排序、固定 ZIP entry timestamp（1980-01-01 UTC 表示）、不包含绝对路径/构建时间；同样输入应得到相同 ZIP/hash（压缩器版本固定）。C++ 和 C# 共用同一 validator/pack tool，测试其输出可安装。SDK 不要求安装宿主源代码。

### 8.3 样例必须实际覆盖的功能

两种语言的 TestPlugin 分别打包，ID 建议 `TestPlugin.Native` 与 `TestPlugin`（C# 与旧 ID 一致以验证设置迁移，不能同时加载同 ID 旧包）。OnLoad 注册 `Counter`、`Message` 两项设置，注册 view `main`、navigation `main`、command `show-counter`；使用通知、日志；OnEnable 提交导航（实际注册可在 OnLoad provisional）；用户点击导航加载与旧页面视觉布局相近的真实控件；ShowCounter 显示现值、Increment 保存并刷新、Message 支持双向编辑并验证；设置管理页编辑后打开插件页可读到相同值；测试一个 MessageDialog 按钮及一次后台任务通过 Dispatcher 回到 UI。OnDisable/OnClosing 取消异步、撤销注册并清理 handler；多次启停不重复导航。OnInstall/OnUninstall 写可观察的日志，不依赖外部副作用；不得只用 Hello World 导出验证然后宣布迁移成功。

## 9. 安全、可靠性及运维的具体边界

- Native DLL 加载在映射时即可执行原生初始化代码；任何哈希、manifest、capability 不能约束恶意原生代码。用户的信任确认必须发生在 LoadLibrary 之前。不能把 in-process native plugin 当成可安全运行不可信 Marketplace 代码的产品架构；未来需要隔离时另提设计，不在本任务擅自加入 IPC。
- 启动时在 `.sdk2-state/startup-session.json` 保存 process identity、当前 activation plugin ID/hash、phase（before map、create、load、install、enable、committed）及 clean flag。进程异常退出/用户强制结束可能导致未清 marker；下一次启动在执行任何插件前展示 Safe Mode，默认停用最近未完成激活插件、其余由用户选择；记录异常并不把 marker 当作确定崩溃归因。允许命令行 `--disable-plugins` 本次进程禁用全部插件执行、不更改持久化后缀；此参数由现有 activation/单实例机制接收时不得被转发到已有不安全进程误认为生效，按现有 CLI 机制明确处理/提示。启动后 crash 的 Active 插件归因不明，可选择禁用所有再逐个启用。
- 宿主日志和 UI 错误区分 Incompatible（版本/架构/旧包）、Rejected（包验证/授权）、LoadFailed（OS loader/导出/QI）、ActivationFailed（hook）、OperationFailed（回调/IO）、PendingRestart（驻留/更新/drain 超时）。展示可重试动作，保留诊断，不吞异常或把失败显示成已启用。主进程原生崩溃无法保证写日志；不添加吞掉访问违规的全局 SEH 或尝试 Thread.Abort。
- 不承诺物理卸载、内存立即归零、独立 GC、同进程安全隔离。性能目标：禁用插件扫描不得执行原生代码；启动仅已授权 enabled 插件映射一次；导航/回调不经过 JSON 序列化或 IPC；设置 IO 不阻塞 UI。测量 0、1、10 个空插件启动时间、峰值/稳定内存和 50 次启停后对象/事件数量，记录基线与实测而非编造毫秒/MB 指标。

## 10. 旧 SDK、迁移及兼容承诺

旧包 `schemaVersion` 缺失/1 或仅托管 DLL：保留原包和配置，列表显示“旧版插件，需要重新构建（SDK 2.0）”；不可在 AOT 宿主启用；非 AOT 宿主默认也不走旧 ALC 路线（不维护双加载机制），不可通过改扩展名/复制 WinMD 假装升级。用户可禁用、卸载、手动安装迁移版；同 ID 的迁移需要显式 Update/Migrate（保留旧包备份，用户重新授权，重启生效），legacy 版本迁移允许 V2 version ≥ 旧 version（同版本必须 hash 改变且显式 migrate），普通 v2 update 必须更高版本。回滚到 legacy 仅恢复原包和设置并显示 Incompatible，不在新宿主执行。旧设置路径与名称保持不变；legacy 数据不被自动删除。旧 plugin SDK v1 的源码可保留历史 tag/迁移文档，不作为 SDK 2.0 发布的依赖。

迁移映射：

| 原 API/模式 | SDK 2.0 |
|---|---|
| `[PluginMain]` + ALC 反射 | `HutaoPluginCreate` → IHutaoPlugin |
| `HutaoPlugin.Manifest/IsEnabled/IconPath` 可变 | 宿主 metadata + context 只读属性 |
| `context.ServiceProvider.GetRequiredService<T>()` | context.Settings/Notifications/Dispatcher/Dialogs/Contributions/Ui 或 versioned GetService |
| `PluginSetting<T>.Value = ...` fire-and-forget | `await SetValueAsync(...)` + committed 值 |
| `PluginScopedPage` 继承宿主类 | 插件 IPluginView + SDK facade + 宿主 PluginHostPage |
| `LoadXamlManually` 从嵌入资源或文件读取 | `context.Ui.LoadXaml("views/...")`；字符串资源使用 ParseXaml |
| `LoadViewModel` 继承内部 ViewModel | 独立 AOT-compatible ViewModel/INotifyPropertyChanged/ICommand + view lifetime |
| Harmony patch MainWindow | Contributions.RegisterNavigationItem/RegisterView |
| `ITaskContext.InvokeOnMainThread` | Dispatcher.InvokeAsync |
| `InfoBarMessage`/IMessenger | Notifications.Show |
| `IContentDialogFactory` | Dialogs.ShowMessageAsync |
| `GetPluginPath` / `GetAllPluginInfos` | 保持内部元数据及 Active 工作目录语义，非公开 raw CLR instance |
| ALC unload | 逻辑撤销、驻留模块、重启替换 |

## 11. 实施顺序：门槛先于大规模重构

G0 — 基线审计：checkout 用户指定分支（本文基线只读，不擅自切换分支）。记录 commit、TFM、SDK/WinAppSDK/CsWinRT 版本、package deployment、已有 Native AOT 与非 AOT 构建测试命令；核对实际全部 `IPluginService`/`HutaoPlugin` 消费者、DI 注册、MainView 导航和设置持久化、游戏集成。保留现有 CI/打包/本地化。报告源码差异与迁移表，不删除旧逻辑直到对应新测试成立。

G1 — **必须先单独验证双向 ABI**：新独立 minimal WinUI host（普通 + PublishAot=true）和两个 DLL（C++/WinRT、C# Native AOT），从同一 WinMD 生成两语言投影；通过唯一 export 创建 IHutaoPlugin；C# 宿主实现 IPluginContext 或同契约测试 adapter（CsWinRT generated CCW），插件回调 host、返回正确 WinRT interface 引用；验证 `String`、`Guid`、`Object`、enum、IAsyncAction、IAsyncOperation<DialogResult>、STA/Dispatcher worker invocation、错误 HRESULT、AddRef/Release 及多次 create/close；测试一个 UIElement 的 ABI 往返（程序式 Button，点击进入插件）。分别在 x64 Native AOT release host、x64 非 AOT release host 实际运行两种 DLL，共四种组合。不允许仅 JIT Debug 成功就宣布 AOT 通过。验证 SDK 版本固定、没有宿主引用、无动态托管加载。C# 宿主接口 CCW 如直接实现失败：允许以同一 IDL/ABI 的 C++/WinRT bridge 实现并通过内部固定 C ABI/GCHandle token 回调宿主，但 bridge 方案属于需要用户批准的备选实现；不得改变公共契约。若 C# AOT 插件端 ABI 不成立，停止 G1 并提交最小复现，不把它降级为只支持 C++ 的完成状态。

G2 — UI/业务核心：G2-A 程序式 UI 与 .hxaml event version（两种 DLL）；G2-B C# AOT plugin-side Binding/INotifyPropertyChanged/ICommand/TwoWay（真实宿主 Native AOT）。验证开关、主题、命令、对话框、线程、设置持久化、页面导航与关闭。此阶段首先用独立 sample；成功后再接真实宿主。G2-A 可作为可用核心的里程碑，G2-B 未通过必须标记未完成原功能完全等价，记录 blocker，不代称成功。

G3 — compiled XAML 实验（可与 G4 并行；非核心阻塞）：按 §6.3 验证外部 C++/WinRT compiled Page/UserControl，单独验证 C#；受支持资源加载方式失败则保留 experimental/unsupported，提交上游 issue/最小复现。禁止在主干引入 Detours 或内部资源 Hook。

G4 — 迁移生产管理层：以 G1/G2 的实测实现替换 ALC，沿现有 PackageStore/Workspace/Registry/Service 做小范围变更；实现 manifest、信任、journal、状态机、设置、导航、通知和对话框；编写包验证单元测试与 UI integration tests；复用原页面及现有服务。禁止保留空 stub、TODO 的成功返回或模拟插件。游戏集成只做 metadata/path gate 适配，不改变原业务行为，不在验收中加载额外注入 DLL。

G5 — SDK 发布与完整回归：生成 Contracts.winmd/投影/NuGet/Native SDK 及可打包示例，在独立目录构建，无宿主项目引用；用实际主程序 AOT 和普通发布包安装、启用、执行、关闭、更新/回滚、卸载。测试 packaged（现有 MSIX 模式）和仓库支持的 unpackaged 模式（如不支持则记录，不改变部署模型）。记录产物、日志、测试命令与结果。只有 G1、G2-A、G2-B、G4、G5 必须项全部通过才称“SDK 2.0 核心功能及原插件样例功能迁移完成”；G3 不影响核心发布但不得宣称 compiled XAML 已支持。

## 12. 验收矩阵（交付标准）

| ID | 实际操作与断言 |
|---|---|
| ABI-01 | 两种原生插件 x 两种 host 发布模式四组合；无需主程序重新编译安装新插件；导出、QI、双向 method 调用成功。|
| ABI-02 | 错误 major、missing export、QI 不支持、成功空指针、返回 HRESULT，正确拒绝且 UI 不显示 Active；不 FreeLibrary 已映射 DLL。|
| ABI-03 | Plugin-owned UIElement 交给 host，按钮触发实际 plugin code；字符串/异步/枚举/服务 QI 与引用计数验证，无需反射。|
| ABI-04 | worker → Dispatcher.InvokeAsync → UI；错误线程直接调用被拒绝；取消和关闭后的回调不触达下一 generation。|
| ABI-05 | 固定 IDL/WinMD IID 与签名 golden snapshot；旧 V1 二进制对增加服务的宿主仍正常。|
| PKG-01 | 真实打包器从 Native Publish/Link 输出生成 .hutao，独立项目不引用宿主；包文件哈希清单完整。|
| PKG-02 | 禁用/旧版/未授权/错误架构包未调用 LoadLibrary（用探针证明，不仅看状态）；恶意 ZIP 路径、重复、超限、hash mismatch 被拒绝。|
| PKG-03 | 同 ID 大小写、同名依赖冲突、双后缀冲突，拒绝自动执行；并行实例不能互相覆盖/删除 workspace。|
| LIFE-01 | 新安装 OnLoad→OnInstall→OnEnable；restart OnLoad→OnEnable（不重复 OnInstall）；disabled install 零第三方执行；首次启用补 OnInstall。|
| LIFE-02 | 连续 enable 不重复；disable 撤销所有贡献；50 次启停只映射一次，导航/handler 数量回基线；新 generation 新实例。|
| LIFE-03 | OnLoad/OnInstall/OnEnable/OnDisable/OnUninstall 分别抛错，按 §5 状态和文件回滚；失败绝不假成功；关闭所有 scope。|
| LIFE-04 | async command in flight 禁用，合作取消成功；不合作 drain timeout → PendingRestart，不释放仍在调用的对象，不再次激活；旧 callback 不能注册。|
| LIFE-05 | Active/disabled uninstall，后者无第三方执行；设置保留；升级需重启、失败回滚；宿主强制退出模拟 journal 恢复和 Safe Mode。|
| SET-01 | Counter/Message 的默认值、原 v1 JSON 值、Bool 注册编辑、空文件/JSON null/空字符串语义正确。|
| SET-02 | SDK 与现有 setting page 双向一致，类型不符/重复注册/IO 失败正确处理；写入成功后才更新 cache，重启仍相同。|
| SET-03 | 同插件多个并发写入有序，不同插件同 setting 名隔离；observer 撤销、无重复订阅；旧 disabled metadata 可编辑且不启动插件。|
| UI-01 | 正式 navigation 注册与点击，窗口重建后恰好一个条目；未 commit 时无条目；未授权 plugin 无条目；内置项顺序不变。|
| UI-02 | .hxaml 真正由 XamlReader.Load 解析；主题切换、控件与布局、Counter/Message/按钮完整交互；程序式 UI 路径亦通过。|
| UI-03 | C# AOT plugin-side Binding、INotifyPropertyChanged、ICommand、TextBox TwoWay 在 AOT 宿主真实运行；不得以手动更新文本冒充 Binding。|
| UI-04 | back/navigation/disable/window close 清理 IPluginView 与 handlers；错误 XAML、null/parented view、factory 抛错显示错误且宿主仍正常。|
| UI-05 | 真实通知四种级别、对话框结果/取消、UI 线程调度；不额外创建宿主顶层 Window；禁用后无残留对话框/通知回调。|
| UI-06 | G3 实验独立：独立 XBF/PRI、x:Class、code-behind、x:Bind、多个 plugin resource、无 Hook。没有通过时 compiled manifest 被明确拒绝。|
| COMP-01 | 旧 .hutao/.hutaodisabled 仍可列出、禁用/卸载；升级保留 Counter/Message；不加载旧 IL。|
| COMP-02 | 现有游戏集成 metadata/path consumer 只收到 Active 已授权插件路径；不改变现有游戏调用或安全逻辑。|
| BUILD-01 | 当前 TFM/SDK、AOT/non-AOT x64 real publish、MSIX 安装与执行（或真实项目支持的部署矩阵），不添加 blanket suppress。|
| BUILD-02 | SDK/C++ 示例/C# 示例独立 build+pack；仓库已有测试通过，新增测试真实覆盖上述行为。|

单元测试可 mock OS loader/clock/storage 做失败注入，但 ABI/UI/发布的验收必须由 Windows 实际产物执行。报告每项 Pass/Fail/Blocked/NotRun、命令、环境、日志或截图；没有运行不允许 Pass。不要将 Linux/静态检查或 .NET analyzer 视为 WinUI 运行时验证。

## 13. 交付及代理执行约束

必须交付：规范中全部公共 IDL/ABI header/WinMD 生成脚本与 ABI snapshot；真实宿主适配代码；C++/WinRT + C# Native AOT SDK 示例及 .hutao 产物生成方式；包校验和状态机测试；Windows G1/G2 运行日志及 G3 单独报告；迁移文档、SDK README 与开发/调试说明。修改前检查 git status，不覆盖用户未提交修改，不改无关业务；未经明确授权不推送、不合并、不强推、不触发发布 workflow、不发布 NuGet。不能为了完成任务任意修改仓库历史或分支。

**禁止代理自行修改的关键事项**：命名空间和接口 IID、导出名与签名、ABI major/minMinor、类型语义、线程/引用所有权、逻辑禁用策略、包目录与后缀、旧包处理、数据迁移规则、核心 UI 模式与测试标准。遇到编译器或框架限制，提供真实错误、最小复现、已尝试方案、可能影响、最小变更提案；用户批准前不更改规范。允许选择内部类的私有实现细节（如具体 IO 封装/现有 DI wiring），不得改变外部行为；所有选择写 ADR（Architecture Decision Record）。

**实现代理任务提示词（可直接复制）**：

> 以本文为唯一 SDK 2.0 设计规范，在用户指定 Snap.Hutao.Remastered 分支实现。首先完成 G0、G1，不直接大规模重构；使用真实 MIDL/WinMD/CsWinRT/C++/WinRT 工具链编译并运行 AOT/non-AOT × C++/C# 的 ABI 测试。任何门槛失败立即记录阻塞与最小修订建议，不能换架构、引入 IPC/Harmony/资源 Hook、伪造 API 或跳过验收。G1 通过后完成 G2-A/B，验证旧 TestPlugin 的全部用户功能，再按 G4 迁移现有管理层与包/设置/UI，保留原有业务管线。按 G5 真实发布安装并逐项输出 §12 矩阵结果。不要擅自改变公共签名、项目依赖、TFM、包格式、版本语义、线程规则或驻留策略。未获用户批准不得推送/合并/触发发布。未完成或未验证的功能明确标记，禁止把 stub、模拟或静态检查称为运行成功。

## 14. 参考来源及未验证事项

源码基线：[Snap.Hutao.Remastered @ b0fa7ce](https://github.com/SnapHutaoRemasteringProject/Snap.Hutao.Remastered/tree/b0fa7ce2141abc31fb7e3ae2b65951e0ab40153a)，对应路径见 §0.1。
工作分支：[aot/plugin](https://github.com/SnapHutaoRemasteringProject/Snap.Hutao.Remastered/commits/aot/plugin)

- [WinRT type system](https://learn.microsoft.com/en-us/uwp/winrt-cref/winrt-type-system)：WinRT 接口继承 IInspectable、接口与 ABI 类型约束。
- [MIDL 3.0 reference](https://learn.microsoft.com/en-us/uwp/midl-3/) 与 [advanced topics](https://learn.microsoft.com/en-us/uwp/midl-3/advanced)：接口语法、IID、版本等；本文的 IDL 尚未运行编译器，不能直接声称可编译。
- [CsWinRT AOT/trimming](https://github.com/microsoft/CsWinRT/blob/master/docs/aot-trimming.md)：生成投影与属性绑定。具体版本下 host CCW、双 AOT runtime 的 WinRT async/Binding 都是 G1/G2 验证项。
- [.NET Native AOT libraries](https://learn.microsoft.com/dotnet/core/deploying/native-aot/libraries)：UnmanagedCallersOnly 导出，Native AOT 库不支持卸载。
- [WinUI XamlReader](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.markup.xamlreader?view=windows-app-sdk-2.0)：松散 XAML 对象树、x:Class 与事件限制。
- [MRT Core](https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/mrtcore/mrtcore-overview)、[WinUI #6299](https://github.com/microsoft/microsoft-ui-xaml/issues/6299)、[DynamicXAML](https://github.com/ahmed605/DynamicXAML)：动态编译 XAML 的资源/元数据难点；DynamicXAML 不等于稳定生产 API。
- [LoadLibraryExW](https://learn.microsoft.com/en-us/windows/win32/api/libloaderapi/nf-libloaderapi-loadlibraryexw)：原生 DLL 搜索策略。

明确未验证：此文件为设计提案，未修改仓库、未执行 MIDL 或 Windows 构建/运行；精确投影 API、CCW/RCW 引用行为、跨 AOT 模块 binding、资源动态注册、MSIX 部署均需要实际验证。G1/G2 的明确阻塞规则优先于任何示例代码或理论推断。设计中固定的路径、命名、IID、限额、取消等待 5 秒、hook 顺序、禁用/更新语义是本次根据用户目标做出的提案选择，用户批准后方可冻结。
