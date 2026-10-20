# CLAUDE.md — Snap Hutao Remastered

> **This file is a contract, not a wish list.** Every statement below was checked against the
> file named beside it. Where reality does not yet match what we want, the gap is recorded in
> [Known drift & TODO](#8-known-drift--todo) with the relevant `CODE_REVIEW.md` ID — it is
> **not** written up here as if it were already true.
>
> **Rule zero:** when this document and the code disagree, the code is the fact and this
> document is the bug. Fix this file in the same PR that changes the fact (`CODE_REVIEW.md#ENG-10`).
>
> Last audited: 2026-10-11 against `dev` @ `ca60671c4` (see [`CODE_REVIEW.md`](CODE_REVIEW.md) §14).

## 1. Project Overview

- **What it is** — Snap Hutao (胡桃工具箱): a C# WinUI 3 desktop application that enhances the
  Genshin Impact experience on Windows without modifying the game client. This repository is the
  **remastered** fork of DGP Studio's upstream (`SnapHutaoRemasteringProject/Snap.Hutao.Remastered`).
- **Runtime** — **.NET 11**. [`global.json`](global.json) pins SDK `11.0.100-rc.2`
  (`rollForward: latestFeature`, `allowPrerelease: true`). ⚠️ that file is currently only in the
  working tree, **not committed** to `dev` (TODO #18) — until it is, a clean clone builds with
  whatever SDK is installed.
- **Target frameworks** — main app
  [`net11.0-windows10.0.26100.0`](src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Snap.Hutao.Remastered.csproj)
  (`x64` only, WinUI 3, `SelfContained`), test project
  [`net11.0`](src/Snap.Hutao.Remastered/Snap.Hutao.Remastered.Test/Snap.Hutao.Remastered.Test.csproj).
- **Declared OS floor** — both manifests declare
  `<TargetDeviceFamily Name="Windows.Desktop" MinVersion="10.0.19041.0" MaxVersionTested="10.0.22621.0" />`
  ([`Package.appxmanifest`](src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Package.appxmanifest),
  [`Package.development.appxmanifest`](src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Package.development.appxmanifest)).
  The public *support statement* (Windows 11 22H2+, Windows 10 22H2 with the Jan-2025 update) is
  policy, not enforced by the manifest — see TODO #7.
- **Packaging** — MSIX (`Package.appxmanifest` for Release, `Package.development.appxmanifest` for
  Debug; single-project MSIX tooling). The app also runs **unpackaged**, detected at runtime
  (`App.xaml.cs`: `TryRegisterUnpackagedNotification`, `TryDetectUnpackagedActivation`). Any
  packaging-dependent code must handle both modes.
- **Language** — C#, `LangVersion=preview`, `Nullable=enable`, `AllowUnsafeBlocks=true`,
  `ImplicitUsings=disable` (all shared imports live in
  [`GlobalUsing.cs`](src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/GlobalUsing.cs)).
- **Architecture** — MVVM (CommunityToolkit.Mvvm 8.4.2 + this repo's own source generator).
- **License** — MIT (`LICENSE`; every upstream file keeps the DGP Studio header).

## 2. Build & Run

```bash
# Restore (solution: 2 projects — the WinUI app and the test project)
dotnet restore src/Snap.Hutao.Remastered/Snap.Hutao.Remastered.slnx

# Build Debug (x64 is the only declared platform)
dotnet build src/Snap.Hutao.Remastered/Snap.Hutao.Remastered.slnx -c Debug

# Test (see the caveat below — the test project does NOT reference the app)
dotnet test src/Snap.Hutao.Remastered/Snap.Hutao.Remastered.slnx

# Single test
dotnet test src/Snap.Hutao.Remastered/Snap.Hutao.Remastered.slnx --filter "FullyQualifiedName~YourTest"
```

- **Run / debug** — open
  [`Snap.Hutao.Remastered.slnx`](src/Snap.Hutao.Remastered/Snap.Hutao.Remastered.slnx) in Visual
  Studio (with the **Single-project MSIX Packaging Tools** extension listed in
  [`src/Snap.Hutao.Remastered/.vsconfig`](src/Snap.Hutao.Remastered/.vsconfig)), set the app project
  as startup, select `x64`, `F5`. The Debug build deploys
  `Package.development.appxmanifest`; the built exe is also directly runnable (unpackaged) at
  `src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/bin/x64/Debug/net11.0-windows10.0.26100.0/win-x64/Snap.Hutao.Remastered.exe`.
- **Plugin SDK** — has its own solution,
  [`src/Snap.Hutao.Plugin.SDK/Snap.Hutao.Plugin.SDK.slnx`](src/Snap.Hutao.Plugin.SDK/Snap.Hutao.Plugin.SDK.slnx),
  which pulls in the main app project, the SDK project and the `TestPlugin` sample.
- **Build quirks (do not "clean up" blindly)**
  - The app csproj contains a `UsePatchedXamlCompiler` target that byte-patches
    `XamlCompiler.exe` on `dotnet build` (MSBuild Core) to work around
    [`microsoft/microsoft-ui-xaml#11157`](https://github.com/microsoft/microsoft-ui-xaml/issues/11157)
    — without it, CLI builds die with `WMC9999: Could not find any resources...` while Visual
    Studio builds succeed. Keep the comment in the csproj if you touch it.
  - A `DeleteLibsAfterBuild` target removes stray `*.lib` from the output; a
    `CopyContentForUnpackaged` target copies `Resource/` and `Assets/` next to the exe for the
    unpackaged mode.
- **Test caveat (important)** — the test project has **no `ProjectReference` to the app**
  (`CODE_REVIEW.md#BUG-02`). It compiles a hand-picked set of 39 production files through
  `<Compile Include="..\Snap.Hutao.Remastered\...">` links and holds 167 `[TestMethod]`s across
  `BaseClassLibrary/`, `RuntimeBehavior/`, `IncomingFeature/` and `PlatformExtensions/`.
  Consequence: **a new test can only touch code that is already source-linked**; anything else
  needs another `<Compile Include>` link. Windows dependencies must either be absent or supplied
  by explicit test doubles, as in `UserInitializationThreadingTest` (real initialization service,
  delayed HTTP responses and a dedicated UI-consumer thread; no real XAML controls).
- **Prerequisites** — the .NET SDK pinned by `global.json`; the workloads and the Single-project MSIX
  packaging extension in `.vsconfig` (`ManagedDesktop`, `NativeDesktop`, `Universal`) for
  packaging/interop work; the WebView2 Runtime on the target machine (the app hosts WebView2 windows
  and `Microsoft.Web.WebView2` is a direct dependency).

## 3. Repository Layout

```
.github/                       Issue templates, 8 workflows (alpha/canary/publish/signpath/…), dependabot.yml,
                               copilot-instructions.md
res/                           Assets and misc resources
Installer/                     Inno Setup installer (installer.iss, ChineseSimplified.isl, VC_redist)
publish/                       Last published Setup exe
tools/                         Vendored cake/NuGet tooling used by the cake scripts
*.cake                         build / build-installer / publish / publish_signpath scripts
src/
  Snap.Hutao.Remastered/
    Snap.Hutao.Remastered.slnx        Solution (app + test), x64 only
    .editorconfig  .vsconfig  settings.xamlstyler  ResXManager.config.xml
    Snap.Hutao.Remastered/            Main app project (WinUI 3)
      API/                 Plugin contract handed to third parties: HutaoPlugin, PluginManifest,
                           PluginContext, [PluginMain], PluginSetting, PluginScopedPage
      Assets/              MSIX tile/splash images, Logo.ico
      Core/                Infrastructure: DI, lifecycle, IO/Http, threading, settings, logging, scripting
      Extension/           Extension methods and helpers
      Factory/             Factory types (process, QR code, …)
      Migrations/          EF Core migrations
      Model/               Domain models, DTOs, EF entities (Model/Entity/Database)
      Properties/          AssemblyInfo.cs, launchSettings.json, FolderProfile.pubxml
      Resource/            Icons, fonts, images, Localization/*.resx
      Service/             Business logic: metadata, git, network, background, plugins, backpack, …
      UI/                  Xaml/ (View, Control, Behavior, Data, Markup, Media), Shell/, Windowing/, Input/
      ViewModel/           MVVM ViewModels (+ ViewModel/Abstraction)
      Web/                 HTTP clients, endpoints, request builders, response validation
      Win32/               P/Invoke, interop
      App.xaml(.cs)  Bootstrap.cs  GlobalUsing.cs  XamlApplicationLifetime.cs
      Package.appxmanifest  Package.development.appxmanifest  app.manifest
      BannedSymbols.txt  CodeMetricsConfig.txt  ApiEndpoints.csv  IdentityStructs.json
    Snap.Hutao.Remastered.Test/       Test project (net11.0, source-linked production files)
  Snap.Hutao.Plugin.SDK/
    Snap.Hutao.Plugin.SDK.slnx        Plugin SDK solution (app + SDK + sample)
    Snap.Hutao.Plugin.SDK/            SDK package (build/*.targets, README)
    TestPlugin/                       Sample plugin (manifest.json, TestPluginPage.hxaml, inject/)
```

## 4. Key Files

- [`GlobalUsing.cs`](src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/GlobalUsing.cs) — the only place
  shared imports may be declared (`ImplicitUsings` is disabled).
- [`src/Snap.Hutao.Remastered/.editorconfig`](src/Snap.Hutao.Remastered/.editorconfig) — one level
  **above** the app project, so it also applies to the test project. 200 lines, all severities
  `suggestion`/`none` (no `error`, no `warning` for StyleCop) — see TODO #5.
- [`settings.xamlstyler`](src/Snap.Hutao.Remastered/settings.xamlstyler) — XAML formatting rules.
- [`BannedSymbols.txt`](src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/BannedSymbols.txt) — banned
  APIs (`List<T>.ForEach`, `ContentDialog.ShowAsync`, `UIElement.RasterizationScale`).
- [`CodeMetricsConfig.txt`](src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/CodeMetricsConfig.txt),
  `ApiEndpoints.csv`, `IdentityStructs.json` — additional analyzer inputs wired in the csproj.
- [`Bootstrap.cs`](src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Bootstrap.cs) — app entry point
  (`<StartupObject>`).
- [`App.xaml.cs`](src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/App.xaml.cs) — `Application`, DI
  bootstrap, activation, exception handling.
- [`XamlApplicationLifetime.cs`](src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/XamlApplicationLifetime.cs)
  — process-wide XAML lifecycle flags + `ShutdownToken`. Use its token for work that must stop on exit.
- [`Core/DependencyInjection/DependencyInjection.cs`](src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Core/DependencyInjection/DependencyInjection.cs)
  — container construction (`ValidateOnBuild = true`, `ValidateScopes = true`), culture and
  notification bootstrap.

## 5. Architecture & Patterns

### 5.1 Dependency injection

Everything is registered by the `Snap.Hutao.Remastered.SourceGeneration` source generator
(package `1.3.16`, `PrivateAssets=all`). **Do not hand-wire registrations.** Attribute forms that
actually exist in the repo (counts as of the audit):

| Attribute | Forms in use | Count |
|---|---|---|
| `[Service(ServiceLifetime.X)]` | concrete self-registration: `Singleton` (40) / `Scoped` (40) / `Transient` (21) | 101 |
| `[Service(ServiceLifetime.X, typeof(IFoo))]` | interface registration, e.g. `[Service(ServiceLifetime.Singleton, typeof(IMetadataService))]` | 145 (of which 29 are keyed) |
| `[Service(..., Key = <enum/typeof>)]` + `[FromKeyed(...)]` | keyed registrations: `IHutaoEndpoints`×3, `IUIGFExportService`/`IUIGFImportService` per UIGF version, `IGamePackageOperation`, `IGameLocator`, `IGachaLogQueryProvider`, `IApiEndpoints`, `ICurrentXamlWindowReference`; consumed via `[FromKeyed(...)]` | 29 registered / 19 consumed |
| `[HttpClient(HttpClientConfiguration.Default)]` | typed `HttpClient` (31) | 44 total |
| `[HttpClient(HttpClientConfiguration.XRpc)]` / `XRpc2` / `XRpc3` / `XRpc5` / `XRpc6` | game-record endpoints | 13 |
| `[GeneratedConstructor]` | plain (205) | 273 total |
| `[GeneratedConstructor(CallBaseConstructor = true)]` | derived class forwards `IServiceProvider` to base (38) | — |
| `[GeneratedConstructor(InitializeComponent = true)]` | XAML root types, e.g. `App` (30) | — |

**`[GeneratedConstructor]` contract:** annotate a **partial constructor whose signature declares
`IServiceProvider serviceProvider`** (e.g.
`[GeneratedConstructor] public partial Foo(IServiceProvider serviceProvider);`). The generator emits
the body and resolves each declared field with `serviceProvider.GetRequiredService<T>()`. A partial
ctor with only non-provider parameters does **not** work (the generator emits an `Error-*.g.cs` and
the build fails with `CS9275`); write a normal constructor in that case.

- `[FromKeyed(...)]` (aliased in `GlobalUsing.cs` from `FromKeyedServicesAttribute`) injects a keyed
  registration; the consumer declares it as a field like any other dependency.
- The container is validated at build time (`ValidateOnBuild = true`), so an unresolvable
  registration fails at startup, not at first use.
- `Ioc.Default` is configured (`DependencyInjection.Initialize(isDefault: true)`) and is used in a
  handful of places that cannot be injected. **Do not add new `Ioc.Default` usages** — inject the
  service (`CODE_REVIEW.md#ARCH-01`).
- Prefer constructor parameters over injecting `IServiceProvider` into new types; the existing
  `*ScopeContext` types are legacy (`ARCH-01`), not a pattern to copy.

### 5.2 Commands and observable properties (MVVM)

- **Commands are `[Command("XxxCommand")]`, never `[RelayCommand]`** (`[RelayCommand]` appears **0**
  times in this repo; CommunityToolkit's generator is not what produces them). The attribute is
  `Snap.Hutao.Remastered.Core.Annotation.CommandAttribute` and the string argument is the
  authoritative generated property name:

  ```csharp
  [Command("LoadCommand")]
  protected virtual Task LoadAsync() { … }          // ⇒ public IAsyncRelayCommand LoadCommand
  ```

  - Async methods generate an `IAsyncRelayCommand`, sync methods an `IRelayCommand`; both are
    `public` and are bound from XAML as `Command="{x:Bind …LoadCommand, Mode=OneWay}"`.
  - The name in the string and the name in XAML must match exactly — convention is
    `<Verb>Command`. 224 uses today (e.g. `LoadCommand`, `RefreshCommand`, `FilterCommand`).
  - The generated command property is not observable state, and nothing in this repo wires a
    can-execute member (`NotifyCanExecuteChanged*` = 0 uses). Gate the UI with an
    `[ObservableProperty]` bound to `IsEnabled` (`[NotifyPropertyChangedFor]`, 65 uses) or validate
    inside the command body and return early.
- **Observable properties use the partial-property form** — all 312 uses follow it:

  ```csharp
  [ObservableProperty]
  public partial bool IsInitialized { get; protected set; }
  ```

  Field-style `[ObservableProperty] private bool x;` is **not** used anywhere; new code must use the
  partial-property form (it also lets the setter be `protected`).
- **Native-backed hotkey settings publish last** — `HotKeyCombination.IsEnabled` applies the native
  change and saves it before raising `PropertyChanged`. Binding handlers run synchronously and can
  re-enter the setter; publishing first lets the older invocation overwrite the newer choice in
  the native action and persisted setting after the handler returns.
- **Collection views expose both notification contracts** — `IAdvancedCollectionView<T>` carries
  `INotifyCollectionChanged` as well as WinRT vector events. Publish filtered/sorted view indices
  and the actual removed item; an ItemsRepeater consuming a bindable adapter must observe removals.
- **Two ViewModel base classes exist** (see TODO #4):
  - [`ViewModel/Abstraction/ViewModel.cs`](src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/ViewModel/Abstraction/ViewModel.cs)
    — full lifecycle: `Initialization` TCS, `Resurrect()`, `Uninitialize()`, `CriticalSection`,
    `CancellationToken`, `IsViewUnloaded`, `DeferContentLoader`, `[Command("LoadCommand")] LoadAsync`
    → `LoadOverrideAsync`, `UninitializeOverride`, `EnterCriticalSectionAsync`, `MakeSubViewModel`.
  - [`ViewModel/Abstraction/ViewModelSlim.cs`](src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/ViewModel/Abstraction/ViewModelSlim.cs)
    — light: `IsInitialized`, `IsViewUnloaded`, `ServiceProvider`, `[Command("LoadCommand")]`;
    `ViewModelSlim<TPage>` adds `[Command("NavigateCommand")]`.
- **Views** live in `UI/Xaml/View/` (`Page/`, `Card/`, `Dialog/`, `Window/`, `Specialized/`, `Shell/`);
  ViewModels in `ViewModel/`; the mapping is by `DataContext`.
- **Messaging** — `IMessenger` (singleton `WeakReferenceMessenger`) carries `InfoBarMessage`
  (`Error`/`Warning`/`Success`/`Information`, plus overloads with title/action button) to
  [`InfoBarView`](src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/UI/Xaml/View/InfoBarView.xaml.cs)
  via [`Service/Notification/InfoBarService.cs`](src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Service/Notification/InfoBarService.cs).
  This is the standard non-modal feedback channel (modal questions go through
  `IContentDialogFactory` / `IContentDialogQueue`).

### 5.3 Page / window lifetime

- **Material controls own their theme brushes** — a visible liquid-glass control resolves its
  default `Foreground` through its own style. Do not bind that value to a detached native peer;
  forwarding a resolved brush bypasses the visible control's theme resource expression. Preserve
  explicit caller colors separately. `ThemeSynchronization` still synchronizes inactive peers.
- Pages derive from
  [`UI/Xaml/Control/ScopedPage.cs`](src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/UI/Xaml/Control/ScopedPage.cs).
  `Loading` creates an `IServiceScope` from `XamlRoot.XamlContext().ServiceProvider`; a page's
  `InitializeDataContext<TViewModel>()` resolves the VM **from that scope**, then calls
  `Resurrect()`, sets `CancellationToken` (page `viewCts`) and `DeferContentLoader`, and assigns
  `DataContext`. `Unloaded` cancels the page token, takes the VM `CriticalSection`, calls
  `Uninitialize()`, clears `DataContext`, then disposes the token source and the scope.
- A VM instance is **reused** across activations (`Resurrect()` resets `Initialization`,
  `IsViewUnloaded` and the load task), so per-activation state must be re-established in
  `LoadOverrideAsync`, not in the constructor.
- Windows are controller-managed (`XamlWindowController`, `UI/Windowing`), also scope-based.
  Services that need a scope call `IServiceProvider.CreateScope()` explicitly (171 sites — legacy,
  `CODE_REVIEW.md#ARCH-02`).
- Work that outlives a page must use `XamlApplicationLifetime.ShutdownToken`; never block on the UI
  dispatcher without a timeout (`CODE_REVIEW.md#BUG-20`).

### 5.4 HTTP layer

- [`Core/IO/Http/RetryHttpHandler.cs`](src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Core/IO/Http/RetryHttpHandler.cs)
  — `DelegatingHandler`, **3 attempts total, idempotent methods only**
  (`GET/HEAD/OPTIONS/PUT/DELETE`); `POST`/`PATCH` fail fast so sign-in / uploads are never applied
  twice. Exponential backoff with jitter (`~200 ms`, `~400 ms`, cancellable). Retries on transport
  errors and on 5xx. On a TLS failure it also rewrites the URI to the current server domain
  (`IServerDomainService`). Opt out per request with
  `request.Options.Set(RetryHttpHandler.DisableRetry, true)`.
- `IServerDomainService` (singleton) owns the current domain mode; **never** mutate domain state
  from a handler or a static field. `ServerDomainRedirectHandler` reuses
  `RewriteHostToCurrentMode` (host-only rewrite, query preserved).
- Request construction: `HttpRequestMessageBuilder` (fluent) via `IHttpRequestMessageBuilderFactory`.
- Responses: `TypedHttpResponse<T>`, validated with
  [`Web/Response/ResponseValidator.cs`](src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Web/Response/ResponseValidator.cs)
  (`DefaultResponseValidator`, `TypedResponseValidator<TData>` resolved through
  `ICommonResponseValidator<T>` / `ITypedResponseValidator<T>`, registered by
  `AddResponseValidation()`).
- All clients are configured in one place:
  `ServiceCollectionExtension.AddConfiguredHttpClients()`
  ([`ServiceCollectionExtension.HttpClient.cs`](src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Core/DependencyInjection/ServiceCollectionExtension.HttpClient.cs)),
  called from `DependencyInjection.Initialize`.

### 5.5 Metadata initialization

- [`Service/Metadata/IMetadataService.cs`](src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Service/Metadata/IMetadataService.cs)
  — singleton (`[Service(ServiceLifetime.Singleton, typeof(IMetadataService))]`), exposes
  `InitializeAsync()`, `FromCacheOrFileAsync<T>(MetadataFileStrategy, CancellationToken)` and the
  underlying `IMemoryCache`.
- `serviceProvider.GetRequiredService<IMetadataService>().InitializeAsync()` at startup; pages then
  read through the `GetContextAsync` extensions
  (`Service/Metadata/ContextAbstraction/MetadataServiceContextExtension.cs`, dictionary/list caches —
  see the PERF items in `CODE_REVIEW.md`).
- Supporting services: `GitRepositoryService` (clones/updates the metadata repositories),
  [`Service/BackgroundActivity/BackgroundActivity.cs`](src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Service/BackgroundActivity/BackgroundActivity.cs)
  (observable activity state for the UI),
  [`Service/Network/NetworkRetryCoordinator.cs`](src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Service/Network/NetworkRetryCoordinator.cs)
  (re-runs startup work after the network recovers — this one is the best-quality recent code in
  the repo and a good template).
- ⚠️ `IMetadataServiceInitialization` still exposes the mechanically renamed
  **`InitializepublicAsync`** — a defect, not a convention (TODO #1).

### 5.6 Plugin system

- The plugin-facing contract lives in
  [`API/`](src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/API): `HutaoPlugin` (base class),
  `PluginManifest`, `PluginContext`, `[PluginMain]` (marks the entry type), `PluginSetting`,
  `PluginScopedPage`, `PluginInfo`.
- A plugin package is a directory with a `manifest.json` plus its assemblies; the entry type is the
  class tagged `[PluginMain]` and **must have a public parameterless constructor**.
- [`Service/Plugin/PluginAssemblyLoadContext.cs`](src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Service/Plugin/PluginAssemblyLoadContext.cs)
  gives **every plugin its own collectible `AssemblyLoadContext`** (`isCollectible: true`, name
  `HutaoPlugin:<id>`): assemblies shipped in the package load there and die with it, while anything
  the plugin does not ship resolves from the host. The host contract assembly is *always* forced
  back to the host so a package can never introduce a second `HutaoPlugin` identity.
  **Never load plugin assemblies with `Assembly.LoadFrom`** (that was the old bug,
  `CODE_REVIEW.md#BUG-11`); go through `PluginAssemblyLoadContext`.
- Enabling/disabling/uninstalling goes through `IPluginService` and is observable via its
  `PluginsChanged` event (raised on the UI thread), because a page may be open before the background
  scan finishes. `GetAllPluginInfos()` = installed (enabled and disabled);
  `GetAllPlugins()` = live instances only.
- Plugins receive the **root** service provider through `PluginContext` (full trust — document it
  when writing plugin docs).
- The SDK ships as a NuGet package built from
  [`src/Snap.Hutao.Plugin.SDK/`](src/Snap.Hutao.Plugin.SDK); `TestPlugin/` is the reference sample.

### 5.7 Localization

- Base language is **Chinese (Simplified)**; English is maintained by the core team; every other
  locale is community-maintained (Crowdin, see `crowdin.yml`).
- One key set, 15 locale files: `Resource/Localization/SH.resx` (source) + `SH.<locale>.resx`,
  and a separate `SHRegex.resx` family for regex strings.
- **New UI strings go into `SH.resx` only.** Do not edit `SH.<locale>.resx` in the same PR; the
  core team and Crowdin handle downstream locales.
- Accessing strings:
  - C#: the resx generator emits a static `SH` class — `SH.SomeKey` for plain strings,
    `SH.FormatSomeKey(arg0, …)` for parameterized ones. Never build UI text by concatenation.
  - XAML: `{shuxm:ResourceString Name=SomeKey}` (the repo's markup extension, ~1350 uses).
  - `SH.Culture` is set during container bootstrap (`InitializeCulture`).
- Hard-coded Chinese literals in `.cs`/XAML are known debt (`CODE_REVIEW.md#LOC-01/02/03`); do not
  add more.

### 5.8 Threading & async

- `ITaskContext` (singleton) is the way to get onto the UI thread: `SwitchToMainThreadAsync()` /
  `BeginInvokeOnMainThread(...)`. Any UI-object mutation after `ConfigureAwait(false)` must go
  through it (`CODE_REVIEW.md#BUG-18` is the cautionary tale).
- `SafeForget()` ([`Core/Threading/TaskExtension.cs`](src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Core/Threading/TaskExtension.cs))
  is `async void` + Sentry mechanism — acceptable only at lifecycle boundaries (activation,
  fire-and-forget UI callbacks), never to hide a failure you could handle.
- Primitives: `AsyncLock`/`AsyncSemaphore`/`AsyncKeyedLock` (semaphores that do not leak), plus
  `SemaphoreSlim` extensions (`EnterAsync`, `Enter()`).
- `ValueTask` is preferred over `Task` where a synchronous completion is common; the generator's own
  analyzer (`SH003`) enforces this.

## 6. Coding Conventions (rules, not descriptions)

- **MVVM first** — UI logic in ViewModels; code-behind only for view mechanics. No business
  decisions in `Page`/`Window` code-behind (`CODE_REVIEW.md#ARCH-04`).
- **Async for I/O**, no UI-thread blocking, no `.Result`/`.Wait()` without a timeout.
- **Guard clauses** — `ArgumentNullException.ThrowIfNull(x)` (237 uses) and
  `Verify.Operation(condition, "why")` (14 uses; `Verify`/`Requires` come from
  `Microsoft.VisualStudio.Validation` via `global using Microsoft;`). Prefer these over `!`,
  which the generator-side analyzer `SH007` also rejects.
- **Exceptions** — app-specific failures go through
  [`HutaoException`](src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Core/ExceptionService/HutaoException.cs)
  helpers: `HutaoException.Throw`, `ThrowIf`, `ThrowIfNot`, `InvalidOperation`, `IO`,
  `OperationCanceled`, `Argument`, `InvalidCast`, `UnauthorizedAccess`. `catch` must log, report or
  rethrow — a bare `catch { }` is a review blocker (`CODE_REVIEW.md#BUG-19`).
- **Nullability** — enabled and enforced by the compiler; use
  `[NotNullWhen(true)]`/`[MaybeNull]`/`[field: MaybeNull]` rather than `!`.
- **Analyzer discipline** — `EnforceCodeStyleInBuild=true` is set in the **app csproj only**
  (not in the test project). There is no `TreatWarningsAsErrors`, and `.editorconfig` leaves almost
  every rule at `suggestion`/`none`, so the build being green does **not** mean the analyzers agree.
  Therefore: do not introduce new warnings, and **do not add `[SuppressMessage]` / `#pragma`
  suppressions** — 171 + 90 already exist (`CODE_REVIEW.md#ENG-03`). If you must suppress, add a
  real `Justification` (`SA1404` is disabled, so this is by convention, not by analyzer).
- **Generated code is generated** — DI registrations, constructors, command properties, observable
  properties, resx accessors and dependency properties come from the source generator. Never
  hand-write what a generator produces, and never edit files under `obj/`.
- **Visibility** — use the narrowest visibility that works. `public` is required only where it is
  genuinely part of a contract: the plugin API (`API/`), XAML-facing types, and members bound from
  XAML. For implementation details, `internal`/`private` are acceptable and preferred (the
  generator's `SH001` even demands `internal` for non-contract types).
  **Never perform a repository-wide visibility rewrite** — the last one (a single commit touching
  2017 files) is what produced `InitializepublicAsync` and 12 `SH001` suppressions
  (`CODE_REVIEW.md#BUG-10`).
- **`unsafe`** is allowed for Win32 interop and hot paths only; keep it inside a small, documented
  API surface.
- **Public API naming** — identifiers must read as English words. `public` must never appear inside
  an identifier (`Readpublic`, `WritepublicAsync`, `InitializepublicAsync` are all rename damage,
  `BUG-10`). When refactoring, prefer `Core`/`Async` suffixes.

## 7. Working in This Repo

- **Branches** — `dev` is the integration branch and the target for all PRs (local and
  `origin/dev` exist; `alpha.yml` builds `dev` and PRs into `dev`, `canary.yml` builds everything
  except `main`/`release`/`dependabot/**`). Canary uses `scripts/PrepareCanarySource.ps1` to merge
  the exact triggering commit and other open, non-draft PR branches from this repository into
  `dev`; it verifies trigger ancestry and records the compiled source commit in the run summary.
  A feature-branch push is included even before its PR exists. `main` is the default/release branch; do not open PRs
  against `main`. `backup`/`main-backup` are archival.
- **Ignore build output** — `bin/`, `obj/`, `TestResults/` and the stray
  `src/Snap.Hutao.Remastered/{Snap.Hutao.Plugin.SDK,TestPlugin}/{bin,obj}` directories are
  artifacts, not sources.
- **Validate before proposing** — build the solution, run `dotnet test`, and smoke-test the feature
  in the app (F5 or the built exe). State explicitly what you did and did not run.
- **Keep edits scoped** — one `CODE_REVIEW.md` ID per PR where practical; reference it in the PR
  description (`Refs BUG-03`) and link issues with `Fixes #123`.
- **Do not** change `.appxmanifest` capabilities, downgrade the SDK/`LangVersion`, or replace
  existing services with new singletons/global state.
- **Documentation is part of the change** — if you change a fact this file asserts (framework
  version, branch, attribute, command name, threshold), update this file in the same PR, and append
  a row to `CODE_REVIEW.md` §14.

## 8. Known drift & TODO

Reality that does **not** match the rules above. Every entry is deliberate debt with its tracking
ID — do not "fix" these by documenting them as if they were intended.

| # | Drift | Where | ID |
|---|---|---|---|
| 1 | `IMetadataServiceInitialization.InitializepublicAsync` is a mechanical-rename artifact; the target name is `InitializeAsync` (public API change → next major) | `Service/Metadata/IMetadataServiceInitialization.cs`, callers in `MetadataService`, `LegacyMetadataService`, `AppActivation`, `MainViewModel` | BUG-10 |
| 2 | Same rename damage in the Zstandard streams: `Readpublic`, `ReadpublicAsync`, `Writepublic`, `WritepublicAsync`, `Flushpublic`, `FlushpublicAsync` → `*Core`/`*Async` | `Core/IO/Compression/Zstandard/*` | BUG-10 |
| 3 | Repository-wide `internal` removal (0 occurrences left) + 12 `SH001` suppressions; `public` was mandated uniformly, which is what caused #1/#2 | whole repo | BUG-10 |
| 4 | Two ViewModel lifecycle state machines (`ViewModel` vs `ViewModelSlim`), derived classes assign `IsInitialized` on their own | `ViewModel/Abstraction/*` | ARCH-03 |
| 5 | Analyzer severities are advisory: 54 `suggestion`, 18 `none`, 2 `warning`, **0** `error`; no `TreatWarningsAsErrors`/`AnalysisLevel`; `NoWarn` swallows `CA2012`; `SA1404` disabled | `src/Snap.Hutao.Remastered/.editorconfig`, app csproj | ENG-01, ENG-02 |
| 6 | Test project cannot reference the app (0 `ProjectReference`, 39 source links, TFM `net11.0` vs `net11.0-windows…`) | `Snap.Hutao.Remastered.Test.csproj` | BUG-02 |
| 7 | Manifest allows Windows 10 `10.0.19041.0` while the public support statement says Windows 10 22H2 (Jan-2025) / Windows 11 22H2 — reconcile one of the two | `Package*.appxmanifest`, project docs site | — |
| 8 | No CI quality gate: none of the 8 workflows runs `dotnet test` or `dotnet format --verify-no-changes` | `.github/workflows/` | ENG-05 |
| 9 | No central package management (`Directory.Build.props` / `Directory.Packages.props` absent, 61 `PackageReference`s across the 4 csproj files carry inline versions) | `src/**/*.csproj` | ENG-04 |
| 10 | `dependabot.yml` NuGet entry still points at the pre-rename path `/src/Snap.Hutao` → no NuGet updates since the rename | `.github/dependabot.yml` | ENG-06 |
| 11 | Dead code / stale config: `LegacyMetadataService` (396 lines, `[Service]` commented out), `build.cake` targets a non-existent `.sln`, commented-out `#pragma` block | `Service/Metadata/LegacyMetadataService.cs`, `build.cake`, `Web/Hoyolab/SaltConstants.cs` | ENG-08 |
| 12 | Normal builds do not emit generator source copies; `AotAnalysis=true` enables diagnostics only, without another generated-source output tree | app csproj | ENG-09 |
| 13 | 171 `[SuppressMessage]` + 90 `#pragma warning disable`, several without `Justification` | repo-wide | ENG-03 |
| 14 | Hard-coded Chinese UI text still present in `.cs` string literals and XAML attribute values (`TestPage`, `LaunchGamePage` worst) and no gate against new ones | repo-wide | LOC-01/02/03 |
| 15 | `ScopedPage` creates its `IServiceScope` only on the first `Loading` (handler unsubscribed) and disposes a `CancellationTokenSource` field it keeps handing out — a second activation throws | `UI/Xaml/Control/ScopedPage.cs` | BUG-05 |
| 16 | Legacy scope plumbing: 272 constructors/partial ctors take `IServiceProvider`, 171 `CreateScope()` calls, 4 `*ScopeContext` bags, 12 `Ioc.Default` uses | repo-wide | ARCH-01, ARCH-02 |
| 17 | `.github/copilot-instructions.md` and `CONTRIBUTING.md` had pre-rename paths / wrong branch names (`src/Snap.Hutao`, `Snap.Hutao.sln`, `develop`, `dev-master`); corrected on 2026-10-11, keep them in sync | `.github/copilot-instructions.md`, `CONTRIBUTING.md` | ENG-10 |
| 18 | `global.json` (the SDK pin referenced in §1) and `CODE_REVIEW.md` exist only as untracked files in the working tree — commit them, otherwise the pin and this review trail do not exist for a clean clone | `global.json`, `CODE_REVIEW.md` | — |

## 9. Changelog of this document

| Date | Change |
|---|---|
| 2026-10-20 | Separated JSON wire policy from reflection, added generated infrastructure/metadata collection contracts, static enum converters, typed ID/HTTP metadata APIs, and finite cultivation navigation. |
| 2026-10-20 | Adopted source-generated settings/database JSON and EF's compiled model; preserved plugin/script support, storage formats, and migrations. Removed extra experiment scripts; retained optional AOT diagnostics. |
| 2026-10-18 | Document Canary's explicit trigger-commit merge, ancestry verification and compiled-source summary, including pushes made before a PR exists. |
| 2026-10-18 | Recounted source-linked production files and test methods after adding account-initialization thread-affinity regression tests; documented their explicit Windows dependency doubles. |
| 2026-10-11 | Full "spec vs reality" audit (`CODE_REVIEW.md#ENG-10`): .NET 10→11, `[RelayCommand]`→`[Command("…")]`, observable partial properties, both ViewModel base classes, `develop`→`dev`, accurate `RetryHttpHandler` semantics, plugin/ALC section, DI attribute matrix, localization mechanism, build quirks, test-project caveat, visibility rule rewritten, §8 Known drift added. |
