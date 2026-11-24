# App Installer 与旧版 EXE 分发

正式版默认使用 Native AOT。Publish MSIX 使用原正式版包名 `E8B6E2B3-D2A0-4435-A81D-2A16AAF405C8` 和 Publisher `CN=SnapHutaoRemasteringProject`，发布证书须与 Publisher 一致。Alpha 和 Canary 保留各自的包标识。安装和更新由 Windows 校验包身份与签名。

## 下载和更新

| 用途 | 文件或入口 |
| --- | --- |
| 推荐安装和自动更新 | [正式版 App Installer](https://package.snaphutaorp.org/Publish/Snap.Hutao.Remastered.Publish.x64.appinstaller) |
| 手动安装打包版 | `Snap.Hutao.Remastered-<version>.msix` |
| 继续使用旧解包版 | `Snap.Hutao.Remastered-<version>-Setup.exe` |
| 安装时一并获取图片资源 | `Snap.Hutao.Remastered-<version>-Setup-cache.exe`，在构建配置提供图片快照时生成 |

打开下载后的 `.appinstaller` 文件可以关联 Windows 更新源。胡桃也能通过 Windows 部署 API 直接打开托管描述文件，无需把 XML 写入用户数据目录。独立 MSIX 的用户仍能在胡桃里检查和安装对应频道的更新。Windows 的浏览器 `ms-appinstaller:` 协议默认关闭。 [Microsoft App Installer 文件说明](https://learn.microsoft.com/en-us/windows/msix/app-installer/app-installer-file-overview) · [Windows SDK 接口说明](https://learn.microsoft.com/en-us/windows/msix/app-installer/app-installer-documentation)

解包版检查到更新时，会提供「安装 MSIX 打包版（推荐）」选项。选择官方源或 Mirror 酱仍可继续用 EXE 更新原安装。正式打包版通过官方 `.appinstaller` 更新，或将 Mirror 酱返回的 MSIX 地址直接交给 Windows 部署。Windows 可以复用未变化的包块。测试频道使用各自的官方更新地址。Mirror 酱继续使用 `SnapHutaoRemastered` 和 `SnapHutaoRemastered_msix` 两个资源 ID。 [Microsoft MSIX 差分更新说明](https://learn.microsoft.com/en-us/windows/msix/desktop/managing-your-msix-deployment-update)

## CI 和 R2

- Publish 校验并上传已签名的 MSIX、标准 EXE 和可选缓存 EXE。标准 EXE 和独立 MSIX 同步到 R2 桶根，供旧版更新器及直接下载使用。
- Publish 与 Alpha 各使用一个频道目录，公开文件名不带运行时后缀。Canary 生成工件供 CI 验证。
- 官方仓库的 `dev` 分支通过构建和测试后，将已签名的 Alpha MSIX 上传到 Mirror 酱 `SnapHutaoRemastered_msix` 的 `alpha` 频道，使用现有的 `MirrorChyanUploadToken`。
- 官方 Publish 和 Alpha 发布工作流将当次 Native AOT 构建生成的 `Snap.Hutao.Remastered.pdb` 上传到 Sentry，并保留 Actions 符号工件。上传认证使用仓库机密 `SENTRY_API_TOKEN`。 [Sentry 调试文件说明](https://docs.sentry.io/cli/dif/)
- 自托管发布先上传并校验版本目录下的 MSIX、证书及框架依赖，再替换固定的 `.appinstaller`。只有替换验证通过后，才清理同一频道内匹配的旧版本目录。
- SignPath 工作流提供相同命名的原生安装包。正式版 Mirror 酱工作流从 GitHub Release 选择标准 EXE 和 MSIX，分别上传到各自的资源。

发布验收需要在 Windows 上覆盖三种情况：已有关联更新源的 MSIX、直接安装的 MSIX、使用 EXE 的旧解包版。对每种情况核对包身份、证书、升级选项及用户数据目录。
