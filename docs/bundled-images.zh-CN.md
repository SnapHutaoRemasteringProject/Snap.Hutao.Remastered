# 把图片资源放进安装包：操作指南

[English](bundled-images.md)

本文使用 **图片版本 7.1** 举例。它与应用版本（如 2.0.2.0）相互独立。

你需要做的事只有三步：**准备 7z → 上传到固定下载地址 → 配置三个 GitHub 变量并运行 CI**。不需要将大量图片提交到 Git。

## 一、先了解最终效果

| 安装包 | 是否附带图片 | 首次引导是否整包下载图片 |
| --- | --- | --- |
| MSIX 打包版 | CI 配置了资源快照就附带 | 始终跳过资源设置、下载步骤 |
| `…-Setup-cache.exe` | 附带 | 跳过资源设置、下载步骤 |
| `…-Setup.exe` | 不附带 | 保留原来的整包下载，可由用户跳过 |

同一轮 CI 可以同时生成两种 Setup。它们是同一个应用的两种安装包，不会安装成两个独立应用。

**没有配置资源快照时，CI 仍可构建，但不会生成 cache 安装包。此时 MSIX 图片按需联网下载。**

## 二、准备图片压缩包（只需要做一次）

### 第 1 步：等下载完成

确认文件已经是：

```text
C:\Archive\Downloads\7.1.7z
```

不要使用 `.crdownload` 临时文件。以下步骤不会由应用自动操作这个下载目录。

### 第 2 步：检查压缩包内的目录

CI 接受下面两种结构。任选一种即可：

```text
方式 A：里面有一个 raw 目录（外面可以还有一层版本目录）
7.1.7z
└─ raw
   ├─ AvatarIcon
   │  └─ UI_AvatarIcon_….png
   └─ ItemIcon
      └─ UI_ItemIcon_201.png

方式 B：分类目录直接放在压缩包根目录
7.1.7z
├─ AvatarIcon
│  └─ UI_AvatarIcon_….png
└─ ItemIcon
   └─ UI_ItemIcon_201.png
```

这里的分类名和文件名必须保留。**不能使用用户缓存目录里那些 SHA1 哈希文件名。** 压缩包中有多个 `raw` 目录时，CI 会报错，避免选错版本。

### 第 3 步：上传到一个固定 HTTPS 下载地址

把 `7.1.7z` 上传到你能管理的资源服务器或 GitHub Release，得到 **能直接下载文件** 的 HTTPS 地址。

例如，你可以自行部署这样的地址：

```text
https://static.snaphutaorp.org/static/snapshots/7.1.7z
```

**上面只是地址设计示例，本次代码没有创建或上传这个文件。** 建议直接填写完整文件地址，例如 `https://static.snaphutaorp.org/static/raw/7.1.7z`。兼容旧配置时，脚本也接受 `/static/raw` 或 `/static/raw/` 目录地址，按资源版本补成 `/static/raw/7.1.7z` 并输出提示。不要填写 `/static/zip/AvatarIcon.zip` 等单分类包地址。

同一版本地址的内容应保持不变。资源更新时发布新地址，例如 `7.2.7z`。

### 第 4 步：计算这个 7z 文件的 SHA256

在本机 PowerShell 执行：

```powershell
(Get-FileHash -LiteralPath 'C:\Archive\Downloads\7.1.7z' -Algorithm SHA256).Hash
```

复制输出的 **64 位十六进制字符串**。计算对象是上传的整个 7z 文件，不是某张图片，也不是解压后的目录。

## 三、配置 GitHub（三个 Variables，不是 Secrets）

1. 打开构建应用的 GitHub 仓库。
2. 进入 **Settings → Secrets and variables → Actions**。
3. 切到 **Variables** 标签。
4. 点击 **New repository variable**。
5. 按下表逐个创建三个仓库变量。

| Name：必须原样填写 | Value：你要填写的内容 |
| --- | --- |
| `BUNDLED_IMAGES_URL` | 第 3 步得到的完整 HTTPS 文件下载地址 |
| `BUNDLED_IMAGES_VERSION` | `7.1`，不带引号，不填应用版本 |
| `BUNDLED_IMAGES_SHA256` | 第 4 步输出的 64 位哈希，不带空格或引号 |
- URL：决定下载哪个文件。
- VERSION：写入随包资源清单，标明资源版本，供客户端显示。

不用创建 `BundledImagesSource` 变量，CI 会在解压和校验完成后自动设置它。

这些变量不替代现有签名 **Secrets**：正式 Publish 仍需要 `PUBLISH_CERT`、`PUBLISH_PW`；SignPath 流程还需要原来的 SignPath 配置。Alpha/Canary 使用原来的 `CERTIFICATE`、`PW`。

三个资源变量必须一起配置。以后更新资源时，也要一起修改 URL、版本和 SHA256。想停用随包资源时，删除这三个变量；只删除其中一个会导致 CI 报错。

## 四、运行 CI 并下载产物

### 第 1 步：运行正式发布流程

1. 进入仓库 **Actions**。
2. 选择 **Snap Hutao Remastered Publish**，或已有签名配置的 **Snap Hutao Remastered Publish (SignPath)**。
3. 点击 **Run workflow**，选择包含这次代码的分支，然后运行。
4. 在日志中检查 **Prepare optional bundled image snapshot**：应显示准备的图片数量和资源版本，而不是 `No bundled image snapshot configured`。
5. 等待整轮构建、打包和签名完成。

CI 会下载 7z、验证压缩包 SHA256、解压并生成图片清单，再将图片作为普通文件放入安装包。不会把 7z 原封不动塞进应用，也不会让用户首次运行时再次解压它。

### 第 2 步：选择正确产物

| 文件 | 用途 |
| --- | --- |
| `Snap.Hutao.Remastered-<应用版本>.msix` | 已签名的打包版 |
| `Snap.Hutao.Remastered-<应用版本>-Setup.exe` | 普通 EXE 安装包，不附带图片 |
| `Snap.Hutao.Remastered-<应用版本>-Setup-cache.exe` | 附带图片的 EXE 安装包 |

Publish MSIX 使用既有包名 `E8B6E2B3-D2A0-4435-A81D-2A16AAF405C8`，Publisher 必须与发布签名证书一致。下载和更新方式见 [发布说明](appinstaller-release.md)。

## 五、图片到底放在哪里，什么时候生效？

MSIX 内部和安装后的目录结构相同：

```text
<应用安装目录>
├─ Snap.Hutao.Remastered.exe
└─ BundledImages
   ├─ manifest.json              ← 图片版本、文件名、大小和 SHA256
   ├─ AvatarIcon
   │  └─ …png
   └─ ItemIcon
      └─ UI_ItemIcon_201.png
```

MSIX 安装目录由 Windows 管理，通常在 `C:\Program Files\WindowsApps\<包全名>\`；Setup 是用户选择的安装目录。**不要把机器上的完整安装路径写进代码。** 客户端通过当前包安装位置或 EXE 所在目录定位 `BundledImages`。

资源在**该安装包安装完成、应用启动并请求图片时**生效。更新安装包后，重新启动应用即可使用新快照，不需要先复制到用户缓存。

每次请求图片按下面顺序查找：

1. **原有可写缓存**：有有效文件就继续使用，避免覆盖用户可能已经下载的新图。
2. **安装目录的 BundledImages**：直接读取，不复制到缓存。
3. **联网下载**：前两处都没有时，下载到原有缓存目录。

例如：安装包附带 7.1 图片，游戏已经更新到 7.2。7.1 中已有的图片可以直接用；缺少的 7.2 新图片按需下载。应用不会因为资源版本较旧就拒绝显示图片。

明暗主题生成的单色图片仍放在可写缓存中，它们属于派生图片，不等于重复复制一份原图。

## 六、清理更新后留下的重复缓存

1. 打开应用设置。
2. 找到 **缓存目录** 区域并展开。
3. 点击 **清理重复图片缓存**。
4. 等待完成提示，查看删除数量和释放空间。

只会删除“资源路径相同、内容 SHA256 相同，而且安装目录原文件仍有效”的缓存。不同内容、未知地址、其他版本的不同图片和主题派生图片都会保留。安装目录本身不会被清理功能修改。

如果新版安装包内的图片与旧缓存同名但内容不同，客户端暂时保留缓存。当前 ZIP 接口没有可靠版本清单，不能仅靠“7.1/7.2”判断哪份同名图片更新。

## 七、本机先试打包（可选）

1. 把真实 7z 解压到一个独立目录，例如 `C:\images\7.1\raw`。
2. 在仓库根目录打开 PowerShell。
3. 执行：

```powershell
./scripts/Prepare-BundledImages.ps1 -SourceDirectory 'C:\images\7.1\raw' -ResourceVersion 7.1
$env:BundledImagesSource = (Resolve-Path './artifacts/bundled-images').Path
dotnet tool restore
dotnet cake publish.cake
```

准备目录默认是 `artifacts/bundled-images`。再次准备时用新的 `-OutputDirectory`；脚本会拒绝覆盖已有准备目录。命令需在同一个 PowerShell 会话运行，环境变量才会传给构建。

切换资源快照或从有资源切到无资源构建时，先清理旧的应用构建/发布输出，避免目录打包混入之前的散文件。只清理构建输出，不要删除用户数据或图片缓存。

## 八、常见问题

| 现象 | 先检查什么 |
| --- | --- |
| 没生成 cache Setup | 三个变量是否在正确仓库的 Actions Variables 中；准备步骤是否实际执行 |
| SHA256 mismatch | 是否对最终上传的同一个 7z 计算哈希；上传时是否替换过文件 |
| No images found / 目录结构错误 | `SourceDirectory` 下应直接是分类目录；压缩包不能放 URL 哈希缓存 |
| 已配置变量，但旧应用没有图片 | 变量只影响后续构建；必须安装新产物并重启 |
| 同名图片还是旧的 | 有效缓存优先；不同字节不会被当作重复缓存自动删掉 |
| SignPath 拒绝新安装包工件 | 远端 installer 工件配置需要允许同一工件中的普通、cache 两个 EXE |
| 提交 msixupload 被商店拒绝 | 检查正式包标识、递增版本、SDK 工具及认证结果；换上传扩展名不会修复包内部问题 |

## 九、以后需要增量更新时怎么做？

当前功能不依赖新 API。若要可靠更新“同名但内容变了”的图片，建议服务端另外提供：

- 固定入口，指出当前资源版本；
- 不可变版本清单，例如 `/static/manifests/7.2.json`；
- 清单中每个文件的路径、长度、SHA256，以及不可变下载地址。

客户端再比较服务器、安装目录和缓存的哈希，只下载缺失或发生变化的文件。**这部分同步器尚未实现，也没有假定示例地址已经存在。** 当前分类 ZIP 仍只服务于无资源 Setup 的预下载流程。

参考：[微软：手工创建 msixupload](https://learn.microsoft.com/en-us/windows/msix/package/packaging-uwp-apps#create-your-app-package-upload-file-manually)、[微软：上传商店应用包](https://learn.microsoft.com/en-us/windows/apps/publish/publish-your-app/msix/upload-app-packages)。
