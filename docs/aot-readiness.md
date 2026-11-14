# 构建、原生发布与交付说明

更新：2026-11-14。当前按用户要求只构建/发布 Debug。现状与证据见 [开发报告](aot-development-report.md)，剩余工作见 [当前阻塞项](aot-blockers.md)。

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

`dotnet build -p:PublishAot=true` 只更新托管编译输入和 XAML，不会替代原生发布。`bin/.../native` 中可能保留旧 EXE/PDB；源码和生成代码已经修复，并不表示该文件已经更新。请完整 publish 到新目录，并从该目录使用匹配的 EXE、PRI/XBF 和资源。

在 Windows、.NET 11 SDK、Windows SDK、VC++ 工具和 WinUI 构建工具齐备的环境执行完整发布：

```powershell
$nativeOutput = Join-Path $PWD ('artifacts/aot/publish-' + (Get-Date -Format yyyyMMdd-HHmmss))
dotnet restore src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Snap.Hutao.Remastered.csproj -r win-x64 -p:PublishAot=true -p:PublishReadyToRun=false
dotnet publish src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Snap.Hutao.Remastered.csproj -c Debug -p:Platform=x64 -r win-x64 -p:PublishAot=true -p:PublishReadyToRun=false -p:SelfContained=true -p:JsonSerializerIsReflectionEnabledByDefault=false "-p:PublishDir=$nativeOutput/"
./scripts/Test-NativePublish.ps1 -PublishDirectory $nativeOutput
```

仅先还原普通解决方案，再带 --no-restore 发布 AOT，可能没有导入原生编译器，无法产生原生 EXE。以上 AOT 参数必须同时用于还原；之后才可在相同配置下加 --no-restore。项目现在会在 PrepareForPublish 前明确拒绝未激活的原生编译器。当前用户要求不自动还原时，须由用户执行该定向还原或明确授权。

原生和普通构建的隐式包不同，不要并行还原同一项目、覆盖同一个 obj/project.assets.json 后再跳过还原。
此前 rc.3 的 ILC 目标在 --no-build 下仍调用 Compile，曾遇到 WinUI 生成项未初始化；当前继续使用完整发布，不依赖该组合。
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
JsonOptions.Default/Create 在普通运行时与原生运行时均使用生成契约，让缺失契约在普通调试中也能暴露。
显式托管兼容入口 CreateReflection 带有裁剪/动态代码要求标记，且仍要求 IsReflectionEnabledByDefault 开启；原生发布关闭该开关。
新网络响应、元数据、AI 会话和工具类型必须补齐根与嵌套闭包，并验证旧格式跨读写。
当前会话通过 AgentConversationJsonOptions 登记应用契约；SDK 类型仍由 SDK 自身契约处理，以保留其内部审批状态字段。旧文件双向读写、24 种可观察内容、重试与待审批恢复均有验证；这不代替实际模型服务/工具执行验收。

2026-11-14 最新源码检查：Debug、`--no-restore`、`PublishAot=true`、`AotAnalysis=true` 编译通过，0 错误、194 个警告（上一轮 254 个）。
这是编译和分析检查，没有执行原生发布或启动应用。上一轮关闭默认 JSON 反射的相关测试为 42/42；
本轮 17 类选择项模型的属性分派、WinRT 转换元数据、集合赋值、页面 VM 属性提供器声明及 HTTP/元数据契约检查均通过。
GachaArchive.Uid 的异常来自 DisplayMemberPath：即使 ItemsSource 使用 x:Bind，显示属性仍通过 ICustomPropertyProvider 查找。
三个存档类型已补齐对应属性的生成访问器，且修复前程序集会被新增的分派检查拒绝。
手写源码中的转换位置使用 CsWinRT 官方 DynamicWindowsRuntimeCast 标注；编译产物检查确认 71 个目标类型具有实际 DynamicDependency 根。
泛型 NameValue<T>、LevelParameters<TLevel,TParam> 的 UI 部分显式实现 IBindableCustomPropertyImplementation，
从而不依赖不支持泛型声明的属性生成器；语言选项也保留维护来源等派生属性的分派。
20 处已确认的 NameValue 选择器改用应用范围的共享编译模板，保持原选项实例、类型化值、选择和相等性逻辑。
其余已确认的启动方案、账户、养成计划、评分预设、楼层、世界等级与技能等级显示字段均有实际编译出的属性访问器。
45 个存在转换警告的依赖属性改为显式源码声明，getter 直接标注需要保留的类型；
注册字段、默认值、回调和访问器语句与迁移前生成定义作逐 token 对照，45 项均一致。未修改生成器包或生成文件。
背景图片像素访问改为已知 IMemoryBufferByteAccess IID 的 QueryInterface 和原生槽位调用，避免传统 ComImport RCW；
4 项缓冲区回归覆盖内存读写、HRESULT 失败、空缓冲区与非法长度。文件扩展名忽略大小写，
本地文件夹变化触发刷新，过渡完成后才提交上一张图片状态，避免取消后单图不再重试；非退出时的 COM 呈现失败会记录错误。
养成编辑接口现在声明既有实现提供的属性通知及突破状态写入契约，编译绑定仍跟踪等级变更和双向编辑。
统计卡片在 DataContext 变化时刷新私有类型化入口；下载窗口使用函数绑定，避开 Window 转换器查找根被当作 FrameworkElement 的生成代码错误。
数据库生成宿主同样通过 Debug --no-restore 编译，仅余 NU1507；共享 DTO 的 WinRT 标注移到应用 UI 部分，未增加宿主依赖。
仍需研究的警告按本轮编译统计如下，不以全局屏蔽方式清零：

| 类别 | 数量 | 后续范围 |
|---|---:|---|
| WMC1510 | 108 | 剩余运行时属性路径、模板上下文与属性提供器 |
| CsWinRT1034 / CsWinRT1035 | 0 | 当前编译没有这两类警告，未屏蔽分析器 |
| CS8305 / WMC1501 | 76 | SDK 实验性图表、窗口布局和控件 API |
| IL2026 / IL3050 | 5 | EF Core 与 EventTriggerBehavior 的裁剪/动态代码要求 |
| IDE0005 | 4 | 数据库源码导入；修改需同步重新生成查询拦截器校验和 |
| NU1507 | 1 | 保留原有包源策略 |

本次编译统计不代表原生链接阶段的诊断；EF Core、事件行为及第三方程序集的裁剪/AOT 要求仍须在新原生发布中复核。
修复依据为 [CsWinRT 绑定说明](https://github.com/microsoft/CsWinRT/blob/2.3.1.260716.1/docs/aot-trimming.md#icustompropertyprovider-type-support-on-aot-winui-binding)
和当前版本的 [转换标注生成器](https://github.com/microsoft/CsWinRT/blob/2.3.1.260716.1/src/Authoring/WinRT.SourceGenerator/DynamicWindowsRuntimeCastGenerator.cs)。
泛型属性分派使用当前版本公开的 [ICustomPropertyProvider 实现契约](https://github.com/microsoft/CsWinRT/blob/2.3.1.260716.1/src/WinRT.Runtime/Projections/ICustomPropertyProvider.net5.cs)，没有启用反射回退。

NuGet.Config 保持原配置；本轮没有执行依赖还原或修改包版本。NU1507 的包源策略警告暂不处理。

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
2026-11-11 的 Debug 全量测试 274/274、关闭 JSON 反射回归 16/16；细节记录在 CODE_REVIEW.md。

未验证：原生 GUI 启动/导航/退出、完整网络/AI/COM 路径、真实无对应运行时机器、MSIX 安装/卸载及性能。
不把旧 194 条当成当前剩余诊断数，也不写“全部原生功能完成”。
历史 JSON/扫描材料见 [归档](https://github.com/SnapHutaoRemasteringProject/Snap.Hutao.Remastered/blob/32976c2f4b7ef2f8053b34131ffd7fdbae5b5722/docs/aot-blockers.md)；R2R 旧验证保留于 CODE_REVIEW.md。
