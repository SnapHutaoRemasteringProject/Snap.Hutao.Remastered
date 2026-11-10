# 构建、原生发布与交付说明

更新：2026-11-10。现状与证据见 [开发报告](aot-development-report.md)，剩余工作见 [当前阻塞项](aot-blockers.md)。

## 构建模式

默认 Debug/Release 都启用 ReadyToRun，使用 .NET 运行时，不默认裁剪或 Native AOT。
Native AOT 是显式发布模式；Release 是配置，publish 是交付阶段，不能混为一谈。
应用只声明 x64，不增加 AnyCPU 映射，不固定 SDK feature band。

```powershell
dotnet restore src/Snap.Hutao.Remastered/Snap.Hutao.Remastered.slnx
dotnet build src/Snap.Hutao.Remastered/Snap.Hutao.Remastered.slnx -c Debug -p:Platform=x64
dotnet test src/Snap.Hutao.Remastered/Snap.Hutao.Remastered.Test -c Debug --logger trx
```

同目录旧实例会锁住普通 bin，先由用户关闭或选独立编译输出；独立 OutDir 编译检查不等于已经生成完整可部署布局。
测试项目源码链接生产文件，没有引用整个 WinUI 应用，测试 PASS 不覆盖所有页面。

## Native AOT 发布

在 Windows、.NET 11 SDK、Windows SDK、VC++ 工具和 WinUI 构建工具齐备的环境执行完整发布：

```powershell
$nativeOutput = Join-Path $PWD ('artifacts/aot/publish-' + (Get-Date -Format yyyyMMdd-HHmmss))
dotnet publish src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Snap.Hutao.Remastered.csproj -c Release -p:Platform=x64 -r win-x64 -p:PublishAot=true -p:PublishReadyToRun=false -p:SelfContained=true -p:JsonSerializerIsReflectionEnabledByDefault=false "-p:PublishDir=$nativeOutput/"
./scripts/Test-NativePublish.ps1 -PublishDirectory $nativeOutput
```

原生和普通构建的隐式包不同，不要并行还原同一项目、覆盖同一个 obj/project.assets.json 后再跳过还原。
本机 rc.3 的 ILC 目标在 --no-build 下仍调用 Compile，会遇到 WinUI 生成项未初始化；使用上面的完整发布。
使用新目录，避免旧 R2R/CoreCLR 文件混入交付；不跳过检查来容忍旧文件。

## 构建目标的职责

| 文件 | 职责 |
|---|---|
| Build/PatchedXamlCompiler.targets | 在私有副本修正 WinUI 编译器资源名；副本按配置/TFM/RID 隔离，避免执行中的文件互锁 |
| Build/ReadyToRun.targets | 默认 R2R 输出、最终 PE 检查与必需载荷检查 |
| Build/AotPublish.targets | AOT 引用顺序、ILC 前 PRI/XBF 准备、正确时间加入发布清单、部署辅助程序补齐、最终原生检查 |
| Build/DatabaseSchema.targets | 历史迁移源指纹及 SQL 资源接线 |
| Build/BundledImages.targets | 可选不可变图片快照 |

AOT 生成/复制使用 SDK 的真实流水线，不手补生成结果。
XBF 清单来自当前 XAML 输入，不扫描旧 publish/AppX 文件，因此已删除脚本窗口的旧缓存不会重新打包。
发布门禁检查 x64 PE、CLR 数据目录为空、没有主托管 DLL/根目录 CoreCLR 宿主，及 PRI/XBF、Assets/Resource 和原生载荷。
单独的 apphost 本来也可能没有 CLR 头，不能只看这一项就认定 Native AOT。
Cake/MSIX/安装器必须消费同一轮完整发布树，不能改用旧 AppX 副本；本轮尚未验收打包安装。

## JSON 与数据库维护

JsonSerialize 从调用方同一份 options/生成解析链取得 JsonTypeInfo；未知类型或解析器异常明确失败，不静默反射重试。
普通运行时的反射兼容受 IsReflectionEnabledByDefault 控制，原生发布关闭该开关。
新网络响应、元数据、AI 会话和工具类型必须补齐根与嵌套闭包，并验证旧格式跨读写。

数据库运行时升级使用版本化 SQL；生产查询具体化并由官方工具预生成。
统一入口为 Generate-DatabaseAot.ps1，维护及回滚/边界覆盖见 [数据库实现](database-aot.md)。
不要直接对含 WinUI XAML 的主项目重走旧 Workspace 查询生成路线，设计/Workspace 依赖保留在独立工具。

## 独立数据目录与启动

非打包应用接受进程环境变量 SNAP_HUTAO_PROFILE_ROOT，必须是绝对路径。
设置、诊断设置、数据库、缓存及基于 DataDirectory 的业务文件进入该目录。
显式 profile 不读取旧设置中的个人数据目录/待删除目录；单实例锁与私有 IPC 按目录隔离，也不替换普通安装的 URI 注册。
它不会自动隔离游戏、外部辅助进程或全部系统集成。MSIX 与默认启动路径保持原行为。

```powershell
./scripts/Test-NativePublish.ps1 -PublishDirectory <发布目录> -Launch -ProfileRoot <独立目录绝对路径>
```

-Launch 启动完整应用，并把 stdout/stderr 写入 profile；它不自动证明导航、账户、数据库和主要功能通过。
用户物理 Esc 已停止本轮界面操作，未恢复前不自动启动/操控新的测试窗口。
认证、验证码和真实账户步骤由用户完成，不拿个人数据做破坏性验证。

## 当前产品范围

插件/Harmony 已在此前分支移除。用户现已授权移除 C# 脚本调试功能：菜单、窗口、上下文、执行器与测试入口均删除。
不加入 CoreCLR 脚本进程；应用无 Roslyn 脚本/Workspace/EF Design 运行时依赖，构建期分析器和数据库设计工具仍保留。
CsWinRT 维持 2.3.1；3.0 的资产问题与包适配见 [独立评估](cswinrt3-assessment.md)，不是本次 Native AOT 的前置条件。

## 验证口径

完整原生发布、生产数据库原生测试和最新普通构建已通过。
最新测试首跑有一个外网 TLS 失败，该项单独复验通过；细节记录在 CODE_REVIEW.md。

未验证：原生 GUI 启动/导航/退出、完整网络/AI/COM 路径、真实无对应运行时机器、MSIX 安装/卸载及性能。
不把旧 194 条当成当前剩余诊断数，也不写“全部原生功能完成”。
历史 JSON/扫描材料见 [归档](https://github.com/SnapHutaoRemasteringProject/Snap.Hutao.Remastered/blob/32976c2f4b7ef2f8053b34131ffd7fdbae5b5722/docs/aot-blockers.md)；R2R 旧验证保留于 CODE_REVIEW.md。
