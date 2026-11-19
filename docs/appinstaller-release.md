# App Installer release distribution

## Channels, identities and R2 object layout

The Store MSIX in `src/output` retains its Microsoft Store Package Name and Publisher. If the release PFX does not match the Store Publisher, that MSIX is **unsigned and Store-submission only**. R2 Publish uses the same Package Name, but substitutes the self-hosted signing certificate's Publisher. Store and self-hosted installations therefore have different Windows package identities and cannot update each other.

The default build uses ReadyToRun (R2R, .NET precompilation). Public paths and filenames omit its runtime name. The optional NativeAOT (native ahead-of-time compilation) variant uses the concise `AOT` suffix so developers and users can distinguish the packages and update feeds. Both variants are published for Publish and Alpha:

```text
r2:package/
├── Publish/
│   ├── Snap.Hutao.Remastered.Publish.x64.appinstaller
│   ├── Snap.Hutao.Remastered_<version>_x64_Publish/
│   │   ├── Snap.Hutao.Remastered_<version>_x64_Publish.msix
│   │   ├── Snap.Hutao.Remastered.cer
│   │   └── Dependencies/x64/*.appx (if required)
│   └── AOT/
│       ├── Snap.Hutao.Remastered.Publish.AOT.x64.appinstaller
│       └── Snap.Hutao.Remastered_<version>_x64_Publish_AOT/
├── Alpha/
│   ├── Snap.Hutao.Remastered.Alpha.x64.appinstaller
│   ├── Snap.Hutao.Remastered_<version>_x64_Alpha/
│   └── AOT/
│       ├── Snap.Hutao.Remastered.Alpha.AOT.x64.appinstaller
│       └── Snap.Hutao.Remastered_<version>_x64_Alpha_AOT/
└── Canary/ (R2 workflow remains disabled)
```

The R2R and AOT packages in **each** channel share that channel's existing Windows package identity (name and publisher) and therefore cannot be installed side by side. Their update descriptors point to separate immutable runtime feeds. Installing a different runtime is an explicit distribution change, not an update in the same feed.

- Publish default: `https://package.snaphutaorp.org/Publish/Snap.Hutao.Remastered.Publish.x64.appinstaller`
- Publish AOT: `https://package.snaphutaorp.org/Publish/AOT/Snap.Hutao.Remastered.Publish.AOT.x64.appinstaller`
- Alpha default: `https://package.snaphutaorp.org/Alpha/Snap.Hutao.Remastered.Alpha.x64.appinstaller`
- Alpha AOT: `https://package.snaphutaorp.org/Alpha/AOT/Snap.Hutao.Remastered.Alpha.AOT.x64.appinstaller`

The same naming convention is reserved for Canary if R2 uploads are enabled later.

The packaging script reads the **generated** `AppxManifest.xml`, checks Package Name / Publisher / Version / architecture, and creates a signed MSIX containing the matching channel-specific descriptor in its installation root. It does not reuse the provided Debug descriptor, nor does it modify the Store artifact. Required framework dependencies are validated against the actual APPX manifest (name, publisher, architecture, version). For `Microsoft.VCLibs.140.00.UWPDesktop` on x64, the generator first checks published dependencies and the Visual Studio `Microsoft.VCLibs.Desktop` SDK, then falls back to Microsoft's pinned `DesktopAppInstaller_Dependencies.zip` from the WinGet 1.9.25180 release if a matching framework is unavailable. The archive SHA-256 is checked against the [Microsoft WinGet package manifest](https://github.com/microsoft/winget-pkgs/blob/master/manifests/m/Microsoft/VCLibs/Desktop/14/14.0.33728.0/Microsoft.VCLibs.Desktop.14.installer.yaml) before extracting the retail x64 APPX. This fallback provides version `14.0.33728.0`; if a future package requires a newer minimum, the build fails until the dependency source is updated. Other missing required frameworks also fail packaging instead of silently publishing a broken installer.

## How an installed app checks for and installs updates

There are **two separate update paths**, and neither requires the disabled `ms-appinstaller:` web protocol.

### 1. Windows App Installer automatic checks (OS-owned)

1. Users download and open the channel-specific `.appinstaller` file (or install via `Add-AppxPackage -AppInstallerFile <URL>`). Windows installs its `MainPackage` and records the App Installer update source and settings for that package. An embedded descriptor can also provide App Installer configuration on supported Windows versions, but do **not** assume every standalone `.msix` install has identical recorded settings.
2. The descriptor currently contains `<UpdateSettings><OnLaunch HoursBetweenUpdateChecks="0" /></UpdateSettings>`. When the user launches the app, **Windows' deployment service** can check the stable HTTPS App Installer URL. Zero means no minimum-hour interval between launch-triggered checks; it does **not** mean a continuously running update monitor.
3. The service downloads the current descriptor at the stable URL, examines its `MainPackage Version`, identity, signature requirements and `Uri`, then fetches and deploys the newer MSIX when appropriate. The package must have the same Windows identity and a **higher version**. The old embedded descriptor may still reference the version originally installed: its stable root `Uri` is what leads Windows to the **new current remote descriptor**.
4. There is **no** `AutomaticBackgroundTask` element in the current generated file, so the descriptor does not enable the separate background check that Windows would otherwise run approximately every eight hours.

The Windows App Installer repository / configuration is separate from the application's own Settings page. On supported Windows versions, use `Get-AppxPackageAutoUpdateSettings` in PowerShell to inspect whether this specific installed package has an associated update source and whether on-launch updates are enabled. The system also allows App Installer settings to be changed by policy or user configuration. Installing only a raw MSIX without an established association must not be assumed to automatically update; inspect the actual configuration.

References: [Microsoft: update settings](https://learn.microsoft.com/en-us/windows/msix/app-installer/update-settings), [automatic update configuration and embedded App Installer](https://learn.microsoft.com/en-us/windows/msix/app-installer/auto-update-and-repair--overview), [App Installer overview](https://learn.microsoft.com/en-us/windows/msix/app-installer/app-installer-file-overview).

### 2. In-app check / update button (application-owned)

1. `UpdateService.CheckUpdateAsync` on a self-hosted packaged installation locates the **single** channel-specific descriptor embedded in `Package.Current.InstalledLocation`, verifies its `MainPackage Name` and `Publisher` against the running package, then obtains the stable HTTPS `Uri`.
2. The app makes an HTTP request for the **current online descriptor** and compares its `MainPackage Version` with `Package.Current.Id.Version`. This is necessary for Alpha: the ordinary Hutao infrastructure version API is for the main release and must not compare an Alpha build against the production version. Store installations continue to use the existing infrastructure API / Microsoft Store flow.
3. If a newer version is reported, the app shows its existing confirmation/skip-version dialog. When the user approves, `UpdateService.TriggerUpdateAsync` calls `PackageManager.RequestAddPackageByAppInstallerFileAsync(remoteUri, AddPackageByAppInstallerOptions.ForceTargetAppShutdown, volume)`.
4. Windows then retrieves the remote descriptor and the referenced signed MSIX (plus framework dependencies, if any), validates identity, and performs deployment. The application does **not** download or patch the MSIX directly. `ForceTargetAppShutdown` permits the deployment service to stop the running application; any restart/automatic launch behavior must be verified on the actual packaged desktop app.

This explicit in-app path also works for a standalone MSIX that contains our signed embedded descriptor, even if Windows did not record on-launch auto-update settings. Old sideload installations lacking a recognized descriptor receive an unsupported-installation notice rather than being migrated across identities. Store installations open Microsoft Store and unpackaged installations are no longer supported.

Reference: [Microsoft: updating non-Store apps from code](https://learn.microsoft.com/en-us/windows/msix/non-store-developer-updates).

## CI publication and storage cleanup

- **Publish:** `publish.yml` preserves both Store MSIX artifacts. The default and NativeAOT builds each produce their own signed self-hosted AppPackages artifact. A downstream `publish_r2` job publishes and cleans `Publish/` and `Publish/AOT/` sequentially **only after both builds succeed**.
- **Alpha:** `alpha.yml` builds signed default and NativeAOT variants in separate matrix jobs. A single downstream `publish_r2` job, after both builds and tests pass, publishes and cleans `Alpha/` and `Alpha/AOT/` sequentially. Pull requests never publish to R2.
- **Canary:** continues generating both variants as GitHub CI artifacts for validation. The R2 publication job is still explicitly disabled by `if: false`.

Each R2 publication validates the target channel/runtime and versioned MSIX, uploads immutable versioned MSIX/dependencies/certificate files, verifies them, and **only then** overwrites the stable `.appinstaller` descriptor. After the new pointer is verified, the script purges old version directories matching **only that exact channel and runtime**, after a default **48-hour retention window** based on their uploaded objects' timestamps. A client may already have obtained an earlier descriptor and still be downloading its versioned MSIX, so immediately removing every old version would break that installation. Versions with missing timestamp information are retained rather than deleted without proof that they are old. The retention window is configurable through `-PreviousVersionRetentionHours` (1–720). This bounds long-term R2 usage while tolerating in-progress downloads; recently published versions temporarily remain. For the default feed this occurs directly under `<Channel>/`; the script only deletes matching version directories, never its sibling `AOT/` directory. This order achieves the requested storage cleanup **without an interval where the existing update URL points to a deleted package**. CI serializes concurrent publication jobs; stale workflow runs must not roll back the current version. The R2 bucket root, other channels, and pre-existing legacy download files are never purged.

The public endpoint must serve `.appinstaller` as `application/appinstaller`, `.msix` as `application/msix` and framework `.appx` as `application/appx`. The mutable descriptor should not be cached for long; versioned objects may be cached immutably. Code-signing certificates must be trusted by the target device for sideload installation.

## Verification checklist

1. Validate generated XML `Uri`, `MainPackage`, `Version`, signature and dependencies for **both** Publish and Alpha runtimes.
2. From a clean Windows machine, download/open the HTTPS `.appinstaller` per channel and verify installation and certificate trust.
3. Check the installed package's associated source with `Get-AppxPackageAutoUpdateSettings`, then publish a higher version and test `OnLaunch` auto-updates.
4. Confirm the in-app update check compares against the corresponding **online channel**, not a different channel's API, and that the in-app update button invokes Windows deployment.
5. Verify R2 contains only the latest version directory for **each** feed after publication, while its update descriptor stays accessible, and Canary never uploads R2 objects.
