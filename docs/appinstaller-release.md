# App Installer release distribution

## Channels, identities and R2 object layout

The Store MSIX in `src/output` retains its Microsoft Store Package Name and Publisher. If the release PFX does not match the Store Publisher, that MSIX is **unsigned and Store-submission only**. R2 Publish uses the same Package Name, but substitutes the self-hosted signing certificate's Publisher. Store and self-hosted installations therefore have different Windows package identities and cannot update each other.

Files uses separate `/files/stable/` and `/files/preview/` delivery paths. Our distribution additionally separates runtime variants so their stable update descriptors never overwrite each other:

```text
r2:package/
├── Publish/
│   └── R2R/
│       ├── Snap.Hutao.Remastered.Publish.R2R.x64.appinstaller
│       ├── index.html
│       └── Snap.Hutao.Remastered_<version>_x64_Publish_R2R/
│           ├── Snap.Hutao.Remastered_<version>_x64_Publish_R2R.msix
│           ├── Snap.Hutao.Remastered.cer
│           └── Dependencies/x64/*.appx (if required)
├── Alpha/
│   ├── R2R/
│   │   ├── Snap.Hutao.Remastered.Alpha.R2R.x64.appinstaller
│   │   └── Snap.Hutao.Remastered_<version>_x64_Alpha_R2R/
│   └── NativeAOT/
│       ├── Snap.Hutao.Remastered.Alpha.NativeAOT.x64.appinstaller
│       └── Snap.Hutao.Remastered_<version>_x64_Alpha_NativeAOT/
└── Canary/ (reserved; R2 workflow is disabled)
```

R2R means ReadyToRun (.NET native precompilation); NativeAOT means Native Ahead-of-Time compilation. Alpha R2R and NativeAOT retain the **same existing Alpha package identity**, so Windows will not install them side by side. Choosing one runtime is a distribution choice. Each package embeds a descriptor pointing to its own R2R or NativeAOT URL; updating that installation through App Installer does not silently change runtime. To use the other runtime, explicitly install that distribution. The Canary package likewise retains its existing identity.

- Publish R2R descriptor: `https://package.snaphutaorp.org/Publish/R2R/Snap.Hutao.Remastered.Publish.R2R.x64.appinstaller`
- Alpha R2R descriptor: `https://package.snaphutaorp.org/Alpha/R2R/Snap.Hutao.Remastered.Alpha.R2R.x64.appinstaller`
- Alpha NativeAOT descriptor: `https://package.snaphutaorp.org/Alpha/NativeAOT/Snap.Hutao.Remastered.Alpha.NativeAOT.x64.appinstaller`

The packaging script reads the **generated** `AppxManifest.xml`, checks Package Name / Publisher / Version / architecture, and creates a signed MSIX containing the matching channel-specific descriptor in its installation root. It does not reuse the provided Debug descriptor, nor does it modify the Store artifact. Missing required framework dependencies fail packaging instead of publishing a broken installer.

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

- **Publish:** the existing manually triggered `publish.yml` preserves Store MSIX artifacts and publishes the signed self-hosted `Publish/R2R` AppPackages directory to R2. NativeAOT continues as a separate GitHub artifact, not a Publish R2 feed.
- **Alpha:** `alpha.yml` builds signed Alpha R2R and NativeAOT packages in separate matrix jobs. Both AppPackages outputs are saved as GitHub artifacts. A single downstream `publish_r2` job, after build and tests pass, downloads **both** and publishes them sequentially to their independent R2 paths. Pull requests never publish to R2.
- **Canary:** produces channel-specific packages as GitHub CI artifacts for validation. The R2 publication job is present but explicitly disabled by `if: false`.

Each R2 publication validates the target channel/runtime and versioned MSIX, uploads immutable versioned MSIX/dependencies/certificate files, verifies them, and **only then** overwrites the stable `.appinstaller` descriptor. After the new pointer is verified, the script purges older version directories matching **only that exact channel and runtime**, leaving one version in each feed. This order achieves the requested storage cleanup **without an interval where the existing update URL points to a deleted package**. CI serializes concurrent publication jobs; stale workflow runs must not roll back the current version. The R2 bucket root, other channels, and pre-existing legacy download files are never purged.

The public endpoint must serve `.appinstaller` as `application/appinstaller`, `.msix` as `application/msix` and framework `.appx` as `application/appx`. The mutable descriptor should not be cached for long; versioned objects may be cached immutably. Code-signing certificates must be trusted by the target device for sideload installation.

## Verification checklist

1. Validate generated XML `Uri`, `MainPackage`, `Version`, signature and dependencies for Publish/R2R and **both** Alpha runtimes.
2. From a clean Windows machine, download/open the HTTPS `.appinstaller` per channel and verify installation and certificate trust.
3. Check the installed package's associated source with `Get-AppxPackageAutoUpdateSettings`, then publish a higher version and test `OnLaunch` auto-updates.
4. Confirm the in-app update check compares against the corresponding **online channel**, not a different channel's API, and that the in-app update button invokes Windows deployment.
5. Verify R2 contains only the latest version directory for **each** feed after publication, while its update descriptor stays accessible, and Canary never uploads R2 objects.
