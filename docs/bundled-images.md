# Bundled image snapshots

[中文分步操作指南](bundled-images.zh-CN.md)

## Store submission output

Formal Publish/SignPath packaging keeps the Store Publisher. When the configured
certificate subject differs from that known Store Publisher, it skips MSIX signing,
exports `msix-signed=false` and labels the artifact `unsigned-store-only`. Setup
signing continues with the existing certificate. This unsigned MSIX is a Store
submission payload, not an installable sideload package. Other identity mismatches
remain errors; the Store manifest is never rewritten to match an old certificate.

All four MSIX workflows also run `scripts/New-MsixUpload.ps1` after packaging/signing.
It creates a ZIP-format `.msixupload` containing the original MSIX and, when the
application PDB is available, a nested `.appxsym` symbol archive. Missing symbols
produce a warning. The `Snap.Hutao.Remastered-Store-upload` artifact contains these
files; retain `.msix` for installation and use the formal Publish upload for Store
submission. Alpha/Canary uploads are test outputs and may have different identities.
This does not automatically submit or publish anything in Partner Center.

See [Microsoft's manual upload instructions](https://learn.microsoft.com/en-us/windows/msix/package/packaging-uwp-apps#create-your-app-package-upload-file-manually).

The application can ship a read-only `BundledImages` directory beside the executable.
This is a loose-file payload, not an archive extracted into the user's cache.
MSIX identity and the Inno Setup AppId are identical with and without this payload.
Only the optional installer filename ends in `-Setup-cache.exe`.

## Runtime behavior

- Retain the existing URL/SHA1 cache layout. Nonempty cached images take precedence,
  since they may contain a newer image or a previously selected quality.
- If there is no cached image, resolve the application's static raw URL to
  `BundledImages/<category>/<filename>` and read the installed file directly.
- If neither exists, download to the existing writable image cache. A 7.1 snapshot
  therefore does not prevent displaying images added in game 7.2.
- Derived Light/Dark monochrome images remain in writable storage. Original images
  are not copied out of the installation to create these variants.
- Category ZIP imports compare SHA256 before saving: an image identical to its
  installed counterpart needs no writable copy. An existing different cached image
  is preserved when the archive entry only duplicates the installed snapshot.
- Settings > Cache folder > Remove duplicate cached images compares the resource
  path, length and SHA256, including rehashing the installed file before deletion.
  It recognizes URL hashes for both primary and backup API/static hosts, preserves
  unknown URLs and different bytes, and never deletes installed files or derived
  theme images. Locked/unreadable files are reported to diagnostics and retained.
- MSIX and Setup with a valid bundle skip both resource guide steps. Only Setup
  without a bundle retains category ZIP preloading. Skipping these steps does not
  mark the unversioned ZIP contracts as fulfilled. The image-resource guide page
  contains the snapshot explanation card; it is subject to the same page-skipping rules.

The resource snapshot version is separate from the application/game version. It
is descriptive metadata, not a claim that unversioned server files are up to date.
Existing different cached bytes are deliberately preserved: without server hashes
the client cannot safely determine whether those bytes are older or newer.

## Prepare a local snapshot

Do not check the large images into Git. Once the real archive is available, extract
it outside the application output and identify the directory containing categories:

```powershell
./scripts/Prepare-BundledImages.ps1 -SourceDirectory 'C:/images/7.1/raw' -ResourceVersion 7.1
$env:BundledImagesSource = (Resolve-Path ./artifacts/bundled-images).Path
dotnet cake publish.cake
```

The preparation script expects `category/file.png` (also jpg/jpeg/webp/gif/bmp),
rejects invalid paths/empty images, copies the payload and creates `manifest.json`:

```json
{
  "schemaVersion": 1,
  "resourceVersion": "7.1",
  "files": [
    { "path": "ItemIcon/UI_ItemIcon_201.png", "length": 12345, "sha256": "64 hexadecimal characters" }
  ]
}
```

Use a new preparation directory for each snapshot. Use clean app build/publish
output when changing snapshots or switching between bundled and unbundled builds,
so older loose output files cannot be included by directory-based MSIX packaging.
The MSBuild None items copy this directory to both normal output and publish
output, including VS/MSIX deployment through ComputeFilesToPublish. Keeping these
files out of MRT indexing prevents category names such as Bg from being treated
as language qualifiers. The four Cake entry points consume the same
published tree. All three Inno Setup entry points build the original installer and,
when a manifest exists in the staged payload, a second installer with images.
The regular installer excludes BundledImages, while the cache variant replaces
the previous installed snapshot. Installing the regular variant over a previous
cache variant keeps that optional snapshot usable; uninstall removes installer-owned files.

## CI configuration

Set these GitHub repository **variables** together:

- `BUNDLED_IMAGES_URL`: immutable HTTPS URL to the versioned 7z archive.
- `BUNDLED_IMAGES_VERSION`: e.g. `7.1`.
- `BUNDLED_IMAGES_SHA256`: SHA256 of that exact archive.

Prefer the complete file URL, e.g. `https://static.snaphutaorp.org/static/raw/7.1.7z`.
For compatibility, a URL ending in `/static/raw` or `/static/raw/` is resolved to
`<directory>/<BUNDLED_IMAGES_VERSION>.7z` with a warning. It does not download a
directory listing or omit archive hash validation.

Alpha, Canary, Publish and SignPath workflows run `Get-BundledImages.ps1`, verify
the archive hash, extract with 7-Zip, prepare a manifest and export
`BundledImagesSource` to subsequent steps. An archive may contain one `raw`
directory or put categories directly at the root. Ambiguous/invalid layouts fail
the build. An entirely absent configuration means no bundled resources; a partial
configuration fails rather than silently publishing a different payload.
Uploads/signing include both installer variants. The remote SignPath installer
artifact configuration must accept the two installer EXEs when the snapshot is enabled.

## Server version API (future, not required for this feature)

Current category ZIPs have no version in their URLs. Sample HEAD checks on Bg.zip
returned identical lengths/ETags for Original/Full and High/Minimum; this is not
proof that every category implements the same policy. This client does not assign
freshness based on those headers or on the game's version number.

For reliable same-name updates, publish an atomic versioned manifest such as
`/static/manifests/7.2.json` and immutable image URLs (or content-addressed files).
It can reuse the schema above, with explicit image quality if variants are added.
A future synchronizer can then compare server, installed and cached SHA256 values,
download only the missing/changed files and record cache provenance. It should
switch manifests atomically after successful validation. No such endpoint is
assumed or contacted by this implementation.
