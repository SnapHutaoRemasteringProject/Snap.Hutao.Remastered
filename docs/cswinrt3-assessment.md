# CsWinRT 3.0 预览版评估（2026-11-10）

评估版本：`3.0.0-preview.261104.1`。当前应用继续使用 `2.3.1`，未将不兼容依赖强制回退到旧投影。

## 与 Native AOT 的关系

新版将封送发现/生成移到应用级构建，能看到其他源生成器产出的代码，并改进私有/合成类型、泛型、事件、异步对象与生命周期处理。
因此它是值得迁移的方向，但不解决 EF 运行时模型/查询生成、JSON 契约缺失或 Roslyn 运行时脚本等应用层问题。
参考 [本次发布说明](https://github.com/microsoft/CsWinRT/releases/tag/3.0.0-preview.261104.1)。

这不是原位替换包版本的升级。官方要求配套 `WindowsSdkPackageVersion=10.0.26100.88-preview` 与 `.1` Windows 目标框架；
2.x/3.0 投影不具有二进制兼容性，相关依赖须提供适用版本。
`Point`/`Size`/`Rect` 等投影成员、事件委托及底层互操作 API 也有变化。
参考 [官方迁移设计](https://github.com/microsoft/CsWinRT/blob/staging/3.0/docs/cswinrt3.0-spec.md)、
[AOT 说明](https://github.com/microsoft/CsWinRT/blob/staging/3.0/docs/aot-trimming.md)。

## 实际主项目试验

```powershell
./scripts/Test-CsWinRT3.ps1
```

脚本编译真实主项目，临时覆盖 CsWinRT 包版本与 SDK/TFM；还原、中间及输出目录在 `bin/CsWinRT3Assessment`。
没有新建替身 WinUI 项目，也不改变当前 2.x 项目的依赖清单或生成结果。
显式给还原传入 `TargetFrameworks`，避免 CLI `TargetFramework` 覆盖在静态还原图中丢失、误用原 `.0` 资产。

Windows / SDK `11.0.100-rc.3.26522.148` 上，新的 SDK 投影和 CsWinRT 包已实际还原；
真实依赖图在 `.1` 兼容性检查中报 `NU1202`，以下八个包没有当前目标可用的兼容资产：

| 包 | 当前版本 |
|---|---|
| CommunityToolkit.Labs.WinUI.Shimmer | 0.1.250828-build.2223 |
| CommunityToolkit.Labs.WinUI.TransitionHelper | 0.1.261027-build.2704 |
| CommunityToolkit.WinUI.Media | 8.3.261026-build.1872 |
| Snap.Hutao.Remastered.Deployment.Runtime | 1.8.1 |
| Snap.ZStandard | 1.5.7 |
| WinUI.Composition.Hlsl | 1.3.1 |
| WinUI.LiquidGlass | 1.3.1 |
| WinUIEssential.WinUI3 | 1.9.2 |

这轮尚未到 C#/XAML 编译或原生运行，不能声称新版已经修复本应用的运行问题。
不使用 `AssetTargetFallback`、忽略还原错误或混装 2.x/3.0 投影来越过兼容性检查。
需先为上述依赖提供兼容构建，再评估其余投影的 API/二进制兼容性和实际绑定行为。

## GitHub 反馈的使用边界

- [CsWinRT #2382](https://github.com/microsoft/CsWinRT/issues/2382) 记录旧 WinUI 投影消费新运行时时缺少 `IWinRTObject` 的问题，符合需要共同迁移的设计约束；这是旧预览版报告，不直接证明本次新版仍有同一故障。
- [CsWinRT #2386](https://github.com/microsoft/CsWinRT/issues/2386) 讨论 `ICustomPropertyProvider`、`IXamlType.IsBindable` 与运行时绑定；不能将升级包版本视作全部 `{Binding}` 已验收。
- [WindowsAppSDK #6394](https://github.com/microsoft/WindowsAppSDK/issues/6394) 的 PRI/XBF 发布缺失已有上游修复记录；本项目仍实际出现资源生成未运行及输出复制遗漏，必须按当前构建图与发布文件验证，不能直接照搬历史结论。

当前优先完成 2.3.1 路径上的完整应用原生发布/启动及功能验证。依赖具备条件后，可重复执行同一主项目试验再决定切换。
