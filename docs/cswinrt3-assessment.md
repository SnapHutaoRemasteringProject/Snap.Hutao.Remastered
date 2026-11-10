# CsWinRT 3：已验证结果和 issue 交接

当前应用使用 CsWinRT 2.3.1。下面是升级到 3.0.0-preview.261104.1、SDK 投影 10.0.26100.88-preview、Windows TFM .1 的独立试验，不是当前 AOT 程序故障。

## 实际选择结果

完整数据：[cswinrt3-toolkit-results.json](cswinrt3-toolkit-results.json)。

| 包 | 试验结果 |
|---|---|
| Shimmer、TransitionHelper、Media | NU1202，没有匹配的 .1 Windows 资产 |
| DataTable、TokenView、Behaviors、Collections、HeaderedControls、Primitives、Segmented、SettingsControls、Converters | 选中 lib/net9.0 的 Uno 版，DLL 引用 Uno.UI；不能算 Windows/CsWinRT 3 支持 |
| DevWinUI 10.9.1 | 只有 build targets，没有 compile/runtime DLL |
| Mvvm 8.4.2 | 选中纯 .NET 资产，不属于 WinRT 问题 |

Collections 单包试验还原返回 0，但依赖 Uno.WinUI 5.5.87。因此只检查 NU1202 会漏掉错误的资产选择。
不能据此说 Labs 已适配、Windows 没适配；本轮验证的是这些具体发布版本，不是两个仓库的最新支持状态。

## 为什么会这样

.NET 10+ 用 Windows TFM 的 .0/.1 区分两代不兼容的 CsWinRT。旧 Windows .0 资产被排除后，有些包报错，有些退回包内通用 net9.0 的 Uno 资产。
规则见 [NuGet/Home #14859](https://github.com/NuGet/Home/issues/14859)。

Hlsl、LiquidGlass、WinUIEssential 已有 WinMD，也声明了 WindowsMetadataReference；缺的是可供新目标选择的正确托管投影，不是再加一个 WinMD。
纯 C# Toolkit 控件没有 WinMD 属正常情况，应从源码构建新目标。
Deployment.Runtime 和 ZStandard 要另外审查打包标签，不应要求它们生成 WinRT 投影。

## 下一轮写 issue 的提示词

只准备可复制正文，不提交到 GitHub。先复核包的当前版本和现有跟踪 issue。

- 对 CommunityToolkit/Windows：报告 Media 的 NU1202，以及 Collections/Primitives 等误选 Uno 的情况。
- 对 Labs-Windows：报告它对应的包，不把两个仓库混为一谈。
- 环境：CsWinRT 3.0.0-preview.261104.1、WindowsSdkPackageVersion=10.0.26100.88-preview、net10.0-windows10.0.26100.1；实测 SDK 11.0.100-rc.3.26522.148。
- Windows 包版本 8.3.261026-build.1872；DataTable/TransitionHelper 为 0.1.261027-build.2704；Shimmer/TokenView 为 0.1.250828-build.2223。
- Labs 源：CommunityToolkit-Labs；Windows 源：CommunityToolkit-MainLatest，均为 dotnet/CommunityToolkit 的公开 Azure feed。
- 附最小 csproj、restore 命令、实际选中资产和 Uno 依赖。不是运行崩溃报告，不声称所有组件无法 AOT。
- 要求新增真正的 .1 Windows 资产：共享组件构建配置中添加新目标；该目标使用上述 CsWinRT/SDK 版本，并对齐 WinUI、Animations、Extensions 等依赖。保留旧 .0 资产。
- 不要求纯 C# 控件补 WinMD，不把旧 DLL 换目录、强制回退或“还原没报错”当成适配完成。
- 当前没有完成上游 3.0 重建，不能把建议配置当作已验证补丁。

部署包的打包问题已创建 [Deployment #1](https://github.com/SnapHutaoRemasteringProject/Snap.Hutao.Remastered.Deployment/issues/1)；其他 issue 未创建。用户随后要求只给 issue 材料，不再代发。
