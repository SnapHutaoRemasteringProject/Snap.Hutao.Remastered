# App Installer release distribution

## Channels and package identity

The release workflow retains the original Store MSIX from `src/output`. Its Store Publisher remains unchanged; when `PUBLISH_CERT` has a different subject, the Store package is intentionally unsigned and is only for Store submission.

Separately, the R2 sideload MSIX uses the **same Package Name** but the Publisher from the release signing certificate. Windows therefore treats the Store and R2 channels as distinct package identities. Cross-channel in-place updates are not supported or attempted.

The regular R2R release additionally produces `AppPackages/` containing:

- `Snap.Hutao.Remastered_x64.appinstaller` — stable update descriptor.
- `Snap.Hutao.Remastered_<version>_x64_Release/Snap.Hutao.Remastered_<version>_x64_Release.msix` — signed immutable package.
- `Dependencies/x64/*.appx` within that version directory, when the packaged manifest declares external framework dependencies.
- `Snap.Hutao.Remastered.cer` in the version directory and a root `index.html`.

The descriptor is generated from the actual published `AppxManifest.xml` rather than the Debug sample and is also embedded in the R2 MSIX install root. The running R2 app reads its stable HTTPS URI and requests the **remote, current** descriptor using `PackageManager.RequestAddPackageByAppInstallerFileAsync`. It does not rely on the deprecated/disabled `ms-appinstaller:` protocol.

Installing through the online `.appinstaller` establishes an association for App Installer update checks; a standalone MSIX installation may lack that association, but the embedded descriptor still allows explicit in-app updates.

## Publishing

The `publish.yml` workflow uploads the entire `AppPackages/` tree as a GitHub artifact. It publishes only the normal R2R release to the existing `r2:package/` bucket (`https://package.snaphutaorp.org/`), not NativeAOT, Alpha or Canary.

The R2 script uploads immutable versioned content first, verifies sizes, and only then replaces the stable `.appinstaller` pointer. It explicitly sets `Content-Type` for `.msix`, `.appx` and `.appinstaller`, and disables caching of the mutable descriptor. It never deletes old R2 releases or legacy Setup EXEs. Historical version cleanup should be introduced separately with an explicit retention policy.

The certificate subject must differ from the Store Publisher and must be valid for code signing. If the certificate is not rooted in a CA trusted by Windows, sideload users must trust its exported `.cer` in the appropriate Windows certificate store.

Alpha and Canary continue publishing their already-established signed MSIX identities and exported certificates as CI artifacts, without generating unpackaged Inno Setup installers. New unpackaged installer publishing and unpackaged in-app updating are discontinued.

## Verification

The packaging script opens the resulting MSIX and validates that its embedded `.appinstaller` matches the remote descriptor, and that the packaged identity matches the R2 publisher. Missing required framework dependencies fail the build instead of silently producing a broken installer.

Before enabling real customer updates, test a clean installation using the public `.appinstaller`, verify the certificate trust path and the HTTPS `Content-Type` / `Content-Length` headers, then increment the MSIX version and check automatic and explicit in-app updates on Windows 10/11.
