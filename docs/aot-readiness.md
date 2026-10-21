# R2R 部署与 Native AOT 兼容性准备

## 默认构建与部署

所有配置（包括 Debug）默认启用 `PublishReadyToRun`。不启用 Native AOT、裁剪或单文件发布；插件、运行时 C# 脚本和 CoreCLR/JIT 功能保持不变。
`Release` 是构建配置，`publish` 是部署产物生成阶段；两者不是二选一，Debug 也可以发布 R2R。

```powershell
dotnet build src/Snap.Hutao.Remastered/Snap.Hutao.Remastered.slnx -c Debug
```

应用及解决方案只声明 x64，不在 csproj 中添加 AnyCPU 映射。
本地 Build 通过 SDK 的 `ComputeFilesToPublish` 生成本轮 R2R，随后把 SDK 选择的 R2R 文件同步到普通 bin。
`obj` 中的 IL 编译输入保持不变；非打包 F5 配置和直接运行 bin 中 exe 也能取得 R2R 文件。
VS 的 MSIX Deploy 使用 WinUI 打包 SDK 的发布文件清单，清单应引用本轮 R2R，而不是旧 AppX 副本。
仅生成编译产物不等于已经部署，必须核对部署清单/最终 AppX 中主 DLL 的 R2R 头。

四个 Cake 入口使用 `DotNetPublish`，签名、MSIX、SignPath ZIP 和安装器统一消费 `bin/x64/<Configuration>/<TargetFramework>/win-x64/publish`。
SDK 发布不包含生成后的 AppxManifest.xml，四个入口的 Copy files 步骤显式复制它，并继续补齐 Assets/Resource；不能只改发布命令而遗漏 MSIX 清单。
构建结束和发布结束均自动检查最终主 DLL 的 R2R 文件头；启用 R2R 却没有对应产物时直接报错。
验证不依赖额外工具安装，也不加载应用。显式指定 `-p:PublishReadyToRun=false` 可临时恢复普通 IL 构建。

首次 R2R 编译会增加构建时间和文件大小。F5 可以加载 R2R 产物，但不保证所有方法始终执行预编译代码；
调试器、运行时回退、分层 JIT 以及 `DOTNET_ReadyToRun` / `COMPlus_ReadyToRun` 环境变量都会影响实际执行。
不自动修改 VS 调试设置或强制优化 Debug 代码；比较启动性能时应同时检查无调试器启动。
现有下载功能使用 Cake.Common 自带的 DownloadFile，不需要 Cake.Http addin。

## 已接入的 JSON 路径

- 非打包设置文件：读取使用 `Dictionary<string, JsonElement>` 的源生成元数据，写入使用有限类型编码器。
  覆盖 LocalSetting 已声明的 19 种类型，包括 Windows Point/Size/Rect 与复合设置；格式、缓存值表示、文件位置和原子替换方式不变。
- 数据库 JSON：`JsonTextValueConverter<T>` 使用 `PersistenceJsonContext`，覆盖角色详情、实时便笺、深渊、幻想真境剧诗、幽境危战五种载荷。
  使用原有命名、数字字符串、空值、字符串转义和未知字段策略；不修改 SQL 表结构或旧 JSON 格式。
- 124 种传递闭包类型已检查注册。ID 类型的空 partial 声明供 JSON 生成器识别，字段和运算符仍由 IdentityGenerator 生成；
  显式注册封闭的 ID 转换器，防止另一个生成器看不到转换器属性时把 ID 写成空对象。
- 相关六个枚举属性改用泛型 `JsonStringEnumConverter<TEnum>`，避免运行时构造枚举转换器。
- 公共 `JsonWireOptions` 不创建反射 resolver，持久化上下文不再通过复制 `JsonOptions.Default` 初始化。
- 仓库列表的直接读取及 HTTP 序列化入口使用基础设施源生成上下文；编码、声明类型和数字字符串策略保留。
- 已知的三种属性曲线集合、技能参数集合、武器曲线集合使用生成的数组元数据和静态封闭转换器。
  普通运行时保留插件自定义泛型的兼容路径；无动态代码的运行时不尝试构造未知泛型。
- 字符串枚举/数字字符串枚举使用标准 `JsonConverter` 属性及封闭泛型转换器；ID 转换器显式获取 UInt32 元数据。
  严格数字、自定义 UInt32 转换器与未知枚举数值均有回归测试。
- 养成页三个导航目标改为显式 typeof 映射，不再通过字符串发现类型。

未知的新数据库 JSON 类型必须显式登记，不能静默回退到反射。
其他网络接口、插件自定义数据等尚未迁移的 JSON 路径仍保留旧实现，不能因此宣称全项目 JSON 已兼容 Native AOT。

## EF 编译模型

`Model/Entity/Database/Compiled` 是官方 EF 工具生成的模型，25 个实体由 EF 11 的程序集属性自动发现。
设计时工厂使用内存 SQLite，不访问开发者个人数据库。
原有迁移路径保留；编译模型不等同于查询预编译，也不消除迁移 API 的 Native AOT 限制。

增删实体、属性、关系或更改模型配置后，使用清单中固定版本的 EF 工具重新生成并提交结果，不手改生成文件：

日常维护使用工具清单固定版本的 `dotnet ef dbcontext optimize`，选择 Debug/x64，输出到现有 Compiled 目录。
直接调用 EF 项目工具时给工具进程传入 Platform=x64，不修改项目的平台配置。

本轮对 Debug 执行 `--precompile-queries --no-scaffold`，独立重试后仍在 MSBuildWorkspace 加载 XAML 的
`.g.i.cs`/`.g.cs` 时发生重复定义（XamlGeneratedProgram、InitializeComponent 等）。未把这个失败的查询生成流程
加入默认构建，也未修改生成文件或用 `EF.CompileQuery` 冒充 Native AOT 查询预编译。后续需要解耦 EF 查询与 UI 设计时编译。

## 验证与边界

2026-10-20 R2R 部署接通验证：

- dotnet CLI 和本机 VS MSBuild 的完整 Debug Build 均成功；普通 bin 主 DLL 与 SDK 本轮 R2R 输出哈希一致，obj 中 IL 编译输入保持原样。
- Debug Publish 成功；四个 Cake 入口编译及 dry-run 均通过。
- 读取 VS 生成的部署清单并按其文件映射暂存 953 个文件到 Debug/AppX；bin、AppX、publish 三处主 DLL 哈希一致且均有 R2R 头。
- 按完整 Copy files 步骤生成未签名 Debug MSIX，核对包内主 DLL 与发布 DLL 完全一致，并检查生成清单、PRI 和图标资源。验证包随后删除。
- 新增的最终文件头检查在 dotnet MSBuild 和 VS MSBuild 下均能接受 R2R，并能拒绝普通 IL。
- 167 项 Debug 回归测试通过（排除两项注册表/外网环境依赖测试）；实际加载 AppX 中本轮 R2R 后，JSON、临时数据库、插件和脚本兼容检查通过。

未执行远端 CI、正式签名/安装，未附加 VS 调试器实际测量 F5，也未在该验证过程中启动交互 WinUI。清单暂存检查不等同于已经注册安装包。

验证不接触用户实际设置或数据库：设置用内存样例，数据库使用独立临时文件。
已验证新旧 JSON 跨读写、数字字符串、未知字段、Windows 复合值，以及编译模型与设计模型的实体、列、键、关系及索引一致性。
设置编码器还已独立编译并运行成 Native AOT 程序，关闭 JSON 反射，实际完成读写。
现有测试项目新增的设置兼容性测试直接链接生产编码器。

2026-10-20 本轮只验证 Debug：真实仓库响应及 UTF-16 HTTP 字符集、五种元数据集合、插件泛型兼容路径、
124 类型旧 JSON、25 实体设计模型一致性及五个实际 JSON 列的临时库读写均通过。
相关 Debug 回归测试 21 项通过；Debug 扩展回归 167 项通过（显式排除既有的注册表与外网 HTTPS 环境依赖项）。
生产设置/ID/枚举编码器作为 Debug Native AOT 程序在关闭 JSON 反射时运行通过。
前一轮曾检查实验生成 DLL 的 R2R 头，并直接加载 obj 中的 R2R 程序集完成上述运行检查；该记录不能证明随后更改配置后的 VS/CI 产物仍然启用了 R2R。
AppX 目录要在部署时才会刷新，不能把其中旧部署副本的验证当作本轮构建验证。
未执行正式签名/安装和交互 UI 测试，也未读取真实历史用户数据。

这些验证不是实际 WinUI 启动、退出、导航或性能实测，不能据此给出启动加速百分比。
完整 Native AOT 仍需继续处理网络 JSON、动态转换器、查询预编译、迁移执行以及 WinRT 封送；未来纯 Native AOT 版才按授权禁用插件/脚本入口。
需要查看剩余编译器诊断时，Debug 构建添加 `-p:AotAnalysis=true`；不额外导出生成源码副本，也不改变运行功能。
