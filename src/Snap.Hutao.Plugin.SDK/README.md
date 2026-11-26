# Plugin SDK 2.0 — isolated G1 prototype

Source baseline: `a142ae67236b1d324a6fd369c3a247abab41df96` (`aot/plugin`).
The design document describes the earlier main branch. This branch has already removed
the former SDK and plugin service source files. No production code is changed here.

**Windows builds and execution are NotRun.** This is prototype source, not a validated
production implementation. The original IDL is copied from section 3 of the design
(14 UUIDs and signatures unchanged). No IPC, managed assembly loading, alternate COM
contract or resource hooks. Central versions remain unchanged: TFM
net11.0-windows10.0.26100.0, CsWinRT 2.3.1, Windows App SDK 2.6.6-experimental.
The host is a separate unpackaged/self-contained WinUI test app. Plugins are trusted
test DLLs explicitly selected by path; this is not a package installer.

## Build and debug in Visual Studio 2026

Open `Snap.Hutao.Plugin.SDK.slnx`, select Debug or Release and x64, restore NuGet and build the solution. The Projection project invokes MIDL and cppwinrt through an MSBuild target before CsWinRT projection generation; the native project is a Visual Studio 2026 C++ DLL project (VCProjectVersion 18.0, v145 toolset). The script reads the projection project's `obj/project.assets.json` to find WinMDs in the exact restored Windows App SDK component packages (including input, composition and dispatching metadata), not just the WinUI package and not unrelated cached versions. The resolver includes versioned metadata directories (for example InteractiveExperiences) using the target platform version, and the restored WebView2 WinMD if present. MIDL's C preprocessor is located automatically from the VS instance, developer environment or vswhere; no developer shell is required for Visual Studio builds. The installed Windows SDK defaults to 10.0.28000.0; use `HutaoWindowsSdkVersion` to override the projection kit and align the native project's Windows SDK version if necessary.

Set `PrototypeHost` as startup project and press F5. Choose **C++ sample** or **C# AOT sample**, inspect the selected trusted DLL path and click Load plugin. C++ Debug/Release DLLs are built by the solution. The managed C# project Build output is not a native plugin: right-click `TestPlugin.Aot` → Publish → `NativeAot` folder profile first. For the two release hosts, publish `PrototypeHost` using the `JitRelease` and `NativeAot` profiles. Start each published executable or attach Visual Studio's native debugger; the Debug F5 host is not an AOT release validation. The published host also supports the command-line path below. No manually supplied metadata paths are necessary for the solution build.

This VS integration and automatic metadata resolution have not been compiled on Windows yet.

## Alternative: command-line build

The preferred route is the solution above. For the standalone command-line workflow, open an x64 Native Tools developer PowerShell with the repository's .NET SDK,
C++ toolchain and Windows SDK (override the kit version if needed). Restore the
exact Windows App SDK 2.6.6-experimental component packages and collect the native
WinMD paths, including Microsoft.UI.Xaml.winmd and its metadata dependencies.
With no parameters the script restores the Projection project and resolves its metadata closure automatically. You can also pass explicit `.winmd` files, metadata directories, or a mixture of both from one resolved package version set, not managed projection DLLs. Directories are expanded non-recursively; other files (including XML documentation) are ignored. Duplicate WinMD filenames are rejected.

```powershell
.\Build-Prototype.ps1
```

For example, the WinUI package's metadata directory can now be passed directly:

```powershell
$metadata = "$env:USERPROFILE\.nuget\packages\microsoft.windowsappsdk.winui\2.6.6-experimental\metadata"
.\Build-Prototype.ps1 -WinAppSdkWinmd $metadata
```

This selects Microsoft.UI.Text.winmd and Microsoft.UI.Xaml.winmd when present. If the
compilers report missing types from other Windows App SDK components, pass those
components' metadata directories or individual WinMD files from the exact restored
dependency versions as additional array entries. The script does not guess versions.

The script stops on compiler failure, generates a single contract and C++ headers,
prints metadata hashes, builds the C++ DLL, publishes the C# Native AOT shared library
and publishes both host modes. Generated binaries are not checked in. MIDL syntax,
reference resolution, CsWinRT projection and AOT compilation must be verified with the
real toolchain; do not silently change the public contract to resolve failures.

## Run all four combinations (separate processes)

```powershell
$root = (Resolve-Path .\artifacts).Path
& "$root\jit-host\PrototypeHost.exe" "$root\native\TestPlugin.Native.dll"
& "$root\jit-host\PrototypeHost.exe" "$root\aot-plugin\TestPlugin.Aot.dll"
& "$root\aot-host\PrototypeHost.exe" "$root\native\TestPlugin.Native.dll"
& "$root\aot-host\PrototypeHost.exe" "$root\aot-plugin\TestPlugin.Aot.dll"
```

Close each host before launching the next. Check activation; click the actual
plugin-owned Button (label and notification must change); run Exercise async ABI
and choose OK in the actual ContentDialog; disable and recreate, repeat.
The exercise checks service lookup, primitive settings round trips, worker-to-UI
dispatch, agility and async enum results. The loader probes wrong major and null
output pointer, transfers owned COM references and retains modules until exit.
Capture logs, compiler errors and screenshots. The harness intentionally recreates
a test instance including install/uninstall callbacks; this is not production
enable/disable transaction semantics.

## Gates

| Gate | Status |
| --- | --- |
| G0 branch/version/source audit | Source inspection only |
| G1 MIDL/cppwinrt/CsWinRT generation | NotRun |
| G1 JIT host / C++ plugin | NotRun |
| G1 JIT host / C# AOT plugin | NotRun |
| G1 AOT host / C++ plugin | NotRun |
| G1 AOT host / C# AOT plugin | NotRun |
| G1 golden ABI snapshot, malformed-export/QI fixtures, explicit lifetime and error-thread tests | NotRun; not implemented yet |
| G2/G3/G4/G5 | Not started |

The probe stores settings in memory, tracks contributions in a test registry, and
logs notifications. It does not implement real navigation, persistence, package
validation, production lifecycle rollback, cancellation/drain, or full XAML binding.
Programmatic WinUI Button, dialog, callbacks and interface calls are actual code paths.
The separate Native AOT DLL installs its own managed dispatcher synchronization
context for its async continuations. ABI, agility, projection and initialization
remain risks until Windows execution. If any gate fails, preserve the real compiler
or runtime error; do not change the design or claim support based on compilation alone.
