# HTTP、元数据 JSON 与主页空白调查

更新：2026-11-13。保持 .NET/EF `11.*`，只做源码、Debug 编译和相关测试；未还原依赖或运行原生界面。

## 已确认的源码问题

`653366069` 是 `Response.publicFailure`，由 `DefaultIfNull` 在响应为空时生成，并不是远端 HTTP 状态码。`HttpRequestMessageBuilderExtension.SendAsync<T>` 会捕获包括 JSON 反序列化失败在内的异常并返回空响应，所以这个提示不能区分连接问题和缺少 JSON 契约。

默认 `JsonSerializeContexts.Generated` 原先主要登记持久化、AI、桥接等契约，没有登记 UserFullInfo、UidCookieToken、ActionTicket 的完整 `Response<T>` 信封，也没有覆盖实际使用的大多数 HTTP 和元数据文件根类型。JIT 版的反射 resolver 会掩盖缺口，反射关闭后则无法解析这些类型。

本轮增加并接入：

- `HttpApiJsonContext`：从真实编译调用核对的 85 个 HTTP 响应和 33 个请求根契约；为同名 DTO 显式区分生成名称。
- `MetadataFileJsonContext`：23 个 `MetadataService.FromCacheOrFileAsync<T>` 文件集合及其元素契约。
- 18 个元数据 ID 的源声明和闭合 IdentityConverter。只提前声明类型，uint 字段、运算符等仍由原 IdentityGenerator 生成。
- 将 JSON 属性引用的三个 file-local 转换器/工厂改为有唯一名称的 internal 类型，供另一个生成器产出的代码访问；转换逻辑保持。
- 三处非泛型字符串枚举转换器改成闭合的泛型转换器。SYSLIB1030/1031/1034 作为错误处理，避免缺失契约、类型名冲突或不支持 AOT 的枚举转换被忽略。

`scripts/Test-HttpJsonContracts.ps1` 读取编译程序集的实际泛型调用和生成 resolver 的 IL，核对 HTTP 请求/响应、元数据集合及元素契约，并验证默认 resolver 确实注册了相应 context。它不加载或执行应用程序集。先前只登记响应的中间构建被此检查报告缺少 31 个请求契约；补齐后的构建通过。

## 主页显示

公告主页原先在等待元数据初始化成功后才创建卡片。现在先在 UI 线程创建卡片和加载状态，再等待元数据；导航任务仍保持未完成，卡片输入也保持关闭，直到元数据就绪。没有通过提前完成初始化来解锁导航。

这改善等待期间的空白，并补齐卡片读取角色、成就等元数据所需的 JSON 契约。不能仅凭源码判断用户设备的 Git、TLS 或代理路径全部正常。

## 本地诊断与产物版本

请求失败日志写入 `SnapHutaoRemastered/Diagnostics/http-failures-<PID>.log`，包含 send/deserialize 阶段、异常链类型、HRESULT、JSON 位置、HTTP 状态、响应类型、构建版本和进程路径。排除响应正文、请求查询、cookie/header 和任意异常消息。XAML 崩溃日志也记录构建与进程路径。

用户 17:58 的启动游戏页堆栈仍是旧的 ImmutableArray 到 ItemsSource 直接赋值。当前生成方法只更新标量/函数绑定，实际 setter 接收转换后的 List<object>。本机找到的原生 EXE/PDB 时间是 16:08，早于集合修复提交 e9382b557（17:37）；该 PDB 有 LaunchGamePage 符号而没有 ItemsSourceHelper。说明找到的原生产物未包含修复，实际运行入口仍需由新的进程路径/构建记录核对。

`dotnet build -p:PublishAot=true` 更新托管编译输入和 XAML，不等于重新生成原生 EXE。需要完整 Debug Native AOT publish 到新目录，再从同一目录运行匹配的 EXE、PRI/XBF 和资源。不要混用多个发布目录的文件。Build 现会明确提示这一点。

## 验证边界

相关测试包括真实 WorldExploration DTO 的生成元数据/字符串枚举读写、脱敏诊断、原有 JSON facade、集合转换和用户初始化。关闭默认 JSON 反射的运行通过，测试 runtimeconfig 中对应开关为 false。编译程序集 JSON 契约检查与 XAML 集合边界检查通过。

这些结果不等于已登录真实账号或完成原生 UI 验收。当前没有运行用户账号请求、复制用户数据或自动启动应用。

依据：[System.Text.Json 源生成使用说明](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/source-generation)、[Native AOT 发布说明](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/)。
