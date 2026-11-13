# 公告页 DataContext 异常源码调查

更新：2026-11-13。用户要求只做源码研究，.NET/EF 保持 `11.*` 主版本策略，不还原、不启动界面。本记录不代表原生崩溃已修复。

## 能够确认的范围

原堆栈中的 `AnnouncementPage_obj1_Bindings.Update_DataContext(AnnouncementViewModel)` 给资源 `ViewModelBindingProxy.DataContext` 赋整个 ViewModel。`ScopedPage.InitializeDataContext` 已先把同一实例赋给页面的原生 DataContext，因此不能直接认定 ViewModel 本身无法封送。

WinUI 的 DataContext 更新会通知绑定。异常停在这个 setter，不能单独区分对象赋值、绑定属性读取、返回值封送和目标属性更新。公开 WinUI 源码只用于核对这一设计，未将公开主分支等同于正在运行的实验版二进制。

`Binding` 在 Native AOT 下受支持，并非必须全部换成 `x:Bind`。CsWinRT 要求普通属性路径有生成的 `ICustomPropertyProvider` 实现；属性返回的集合等对象，还需要自己的 WinRT 接口支持。

## 逐层核对

| 层次 | 证据与判断 |
|---|---|
| 页面 ViewModel | 已有静态 WinRT vtable；自定义属性生成器也生成了 `IBindableCustomPropertyImplementation.GetProperty`，包括相关属性和命令。不能仅以未写官方属性标记判定它缺少访问器。 |
| XAML 类型 stub | WinUI 的 `PropertyInfoPropertyAccess` 对非 bindable 类型返回空访问器，后续仍会尝试 `ICustomPropertyProvider`。stub 本身不是失败证据。 |
| 原代理绑定 | 涉及 `IsActivityCalendarPresented`、`ActivityCalendar`、`DisplayedActivityCardItems` 和 `CopyCodeCommand`；其中列表即使为空也会返回一个实际的 `List<Act>`。 |
| 复制命令 | 实际解析的是 Toolkit 8.4.2 的 Windows 资产。两个 `AsyncRelayCommand` 类型均带 `WinRTExposedType`，无需凭猜测另补命令 vtable。 |
| 活动列表 | 历史导出的全局 lookup 未含 `List<Act>`；已有包含直接 x:Bind 的主编译程序集含该项。同一源码的 XAML 临时编译在显式登记前缺少该项、登记后出现该项和对应 `ReadOnlyCollection<Act>` 项。 |

CsWinRT 2.3.1 的 Auto 分析会检查可见语法中的对象转换；具体类型没有发生转换时通常不会登记外部泛型类型。我们自己的属性生成器输出不作为另一个生成器的输入，里面的 `List<Act>` 到 `object` 转换因此不能作为可靠的自动发现依据。`x:Bind` 由 XAML 构建阶段生成有类型的调用，能够使这类转换在后续 C# 编译时可见。

这支持“集合接口生成覆盖缺口”这一假设，但不等于已证明旧原生包的 `E_INVALIDARG` 必然由它产生。临时编译与主编译的比较不是失败原生包的运行对照。

## 当前源码处理与验证

- 已有提交 `82200f1db` 移除公告页 ViewModel 资源代理，相关根属性改为 x:Bind；复制按钮仍使用原命令。
- 在 AssemblyInfo 显式登记 `GeneratedWinRTExposedExternalType(typeof(List<Act>))`，使集合接口支持不再依赖 XAML 调用位置的自动发现。未更改服务返回值、集合顺序或内容。
- 兑换码 DTO 的 Code/Title 是 init-only 属性，三个相关 x:Bind 使用 OneTime，消除不具备变化通知却声明 OneWay 的 WMC1506。
- 对比 XAML 临时程序集 `GlobalVtableLookup.LookupVtableEntries` 的实际 IL：显式登记后确有 `List<Act>` 和 `ReadOnlyCollection<Act>`。没有手改生成代码。
- Debug no-restore 检查完成 XAML 临时编译及绑定生成；完整构建被现有 EF 缓存版本与数据库生成产物版本一致性校验拒绝。没有关闭校验，没有还原或调整包版本，不能报告完整编译或原生运行通过。日志在 `artifacts/aot/2026-11-12/announcement-binding`。

## 后续运行证据

如恢复原生调试，应使用同一个新发布目录内的 EXE、XBF/PRI 和匹配符号，对照无消费者代理、单独 bool 绑定、单独列表 Binding、显式接口登记后的列表 Binding。若列表 Binding 在登记前失败、登记后成功，才可把本次根因落到缺失的集合接口支持。

当前生成的 `Update_DataContext(AnnouncementViewModel)` 已没有整对象 DataContext setter。若新测试仍报告完全相同的整对象赋值堆栈，先核对实际加载的程序与资源是否来自本次发布。日历 Grid 对 `ActCalendar` 的 DataContext 赋值是另一处生成方法，不应混淆。

Scale/GetElementVisual 冲突、异常处理器主动 Debugger.Break、Sentry.Flush 等待期间的代理登记错误分别记录；没有证据将它们认定为此处 E_INVALIDARG 的同一根因。

## 上游依据

- [CsWinRT 2.3.1 AOT 文档](https://github.com/microsoft/CsWinRT/blob/2.3.1.260716.1/docs/aot-trimming.md)：生成属性访问器、外部类型登记和 Auto 模式边界。
- [CsWinRT 2.3.1 AotOptimizer](https://github.com/microsoft/CsWinRT/blob/2.3.1.260716.1/src/Authoring/WinRT.SourceGenerator/AotOptimizer.cs)：`NeedVtableOnLookupTable`、`GetVtableAttributesToAddOnLookupTable`，尤其具体类型与目标类型相同的过滤。
- [CsWinRT 2.3.1 属性访问实现](https://github.com/microsoft/CsWinRT/blob/2.3.1.260716.1/src/WinRT.Runtime/Projections/ICustomPropertyProvider.net5.cs)：先使用生成接口；AOT 缺少实现时抛带类型/属性名的 NotSupportedException，而不是一概拒绝 Binding。
- [WinUI 属性路径解析](https://github.com/microsoft/microsoft-ui-xaml/blob/7f4194de7c67878c0770111ef73fe34f077c3f7c/dxaml/xcp/dxaml/lib/PropertyAccessPathStep.cpp) 与 [属性元数据访问](https://github.com/microsoft/microsoft-ui-xaml/blob/7f4194de7c67878c0770111ef73fe34f077c3f7c/dxaml/xcp/dxaml/lib/PropertyInfoPropertyAccess.cpp)：依赖属性、应用元数据、ICustomPropertyProvider 的查找顺序。
- [CsWinRT 维护者确认的泛型集合自动分析限制](https://github.com/microsoft/CsWinRT/issues/2304#issuecomment-3989485080)：可用显式外部类型登记补足；其案例不是公告页崩溃的复现证明。
