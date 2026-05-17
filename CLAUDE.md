# CLAUDE.md — Snap Hutao Remastered

## Project Overview

Snap Hutao (胡桃工具箱) is a C# WinUI 3 desktop application (MSIX-packaged) that enhances the Genshin Impact experience on Windows without modifying the game client. This is a **remastered fork**.

- **Target:** Windows 11 22H2+, Windows 10 22H2 (Jan 2025 update)
- **Framework:** .NET 10 + Windows App SDK / WinUI 3 (XAML)
- **Language:** C# with `LangVersion=preview`, nullable enabled, unsafe blocks allowed
- **Architecture:** MVVM (CommunityToolkit.Mvvm + ObservableObject)
- **License:** MIT

## Build & Run

```bash
# Restore (solution root: src/Snap.Hutao.Remastered/Snap.Hutao.Remastered.slnx)
dotnet restore src/Snap.Hutao.Remastered/Snap.Hutao.Remastered.slnx

# Build Debug
dotnet build src/Snap.Hutao.Remastered/Snap.Hutao.Remastered.slnx -c Debug

# Run (requires Visual Studio 2022 for MSIX packaging/debug)
# Open .slnx in VS2022, set Startup Project, select x64, F5

# Run tests (test project: Snap.Hutao.Remastered.Test)
dotnet test src/Snap.Hutao.Remastered/Snap.Hutao.Remastered.slnx

# Run specific test
dotnet test src/Snap.Hutao.Remastered/Snap.Hutao.Remastered.slnx --filter "FullyQualifiedName~YourTest"
```

Prerequisites: Visual Studio 2022 with .NET desktop dev + C++ desktop dev + Windows app dev workloads, MSIX Packaging Tools.

## Repository Layout

```
.github/                  — Issue templates, workflows (alpha/canary CI), copilot-instructions.md
res/                      — Assets and misc resources
src/
  Snap.Hutao.Remastered/
    Snap.Hutao.Remastered.slnx        — Solution file (new SLNX format)
    Snap.Hutao.Remastered/            — Main app project (WinUI 3)
      Core/                — Infrastructure, DI, lifecycle, IO/Http
      Extension/           — Extension methods, helpers
      Factory/             — Factory types
      Migrations/          — EF Core migrations
      Model/               — Domain models, DTOs
      Resource/            — Icons, fonts, images, localization (.resx)
      Service/             — Business logic: metadata, git, network, background, etc.
      UI/                  — XAML views, controls, windows
      ViewModel/           — MVVM ViewModels
      Web/                 — HTTP clients, endpoints, request/response handling
      Win32/               — P/Invoke, interop
    Snap.Hutao.Remastered.Test/       — Unit tests
  Snap.Hutao.Plugin.SDK/             — Plugin SDK
```

## Key Files

- `GlobalUsing.cs` — shared global usings
- `BannedSymbols.txt` — banned API analyzers
- `.editorconfig` / `settings.xamlstyler` — coding style rules
- `Package.appxmanifest` / `Package.development.appxmanifest` — MSIX identity/capabilities
- `Bootstrap.cs` — app entry point
- `App.xaml.cs` — app startup, DI registration, lifecycle

## Architecture & Patterns

### DI
- Services registered via `[Service(ServiceLifetime.X)]` attribute + source generator
- HTTP clients via `[HttpClient(HttpClientConfiguration.X)]` attribute
- `GeneratedConstructor` partial method for constructor DI injection

### MVVM
- Views in `UI/Xaml/View/`, ViewModels in `ViewModel/`
- ViewModels extend `Abstraction.ViewModel` (ObservableObject subclass)
- Commands via `[RelayCommand]`, observable properties via `[ObservableProperty]`
- `IMessenger` for cross-component communication (InfoBarMessage, etc.)

### HTTP Layer
- `RetryHttpHandler` (DelegatingHandler) — retries up to 3× on transport errors AND server errors (5xx)
- `HttpRequestMessageBuilder` — fluent builder pattern for HTTP requests
- `TypedHttpResponse<T>` — typed response wrapper
- Response validation via `ResponseValidator` + typed validators
- All HTTP clients configured through `ServiceCollectionExtension.AddConfiguredHttpClients()`

### Metadata Initialization
- `IMetadataService` (singleton) — async init via `InitializeAsync()` + `InitializepublicAsync()`
- `GitRepositoryService` — clones/updates metadata from remote git repos
- `BackgroundActivity` — observable activity status for UI binding
- `NetworkRetryCoordinator` — network-aware retry of failed startup operations

### Localization
- Base language: Chinese (Simplified). English maintained by core team.
- New UI strings: only edit `Resource/Localization/SH.resx`. Do NOT edit other locale files.
- Prefer resource binding over hard-coded strings.

## Coding Conventions

- **MVVM first:** UI logic in ViewModels, async for I/O, avoid UI thread blocking
- **Analyzers:** EnforceCodeStyleInBuild=true, fix StyleCop/source-gen analyzer violations
- **Guard clauses:** Use `Verify.Operation()`, `ArgumentNullException.ThrowIfNull()`, etc.
- **Exception handling:** `HutaoException.Throw()` for app-specific errors; capture network exceptions gracefully
- **Async:** `ValueTask<T>` preferred; `.ConfigureAwait(false)` for background work; `SafeForget()` for fire-and-forget
- **Nullability:** Enabled; `[NotNullWhen(true)]`, `MaybeNull`, etc. used with static analysis
- **Generated code:** Source generators for DI registration, constructors, MVVM; don't hand-wire these
- **Unsafe:** `AllowUnsafeBlocks=true` — used for Win32 interop and performance-sensitive paths

## Working in This Repo

- Always build and verify analyzer warnings are clean before proposing changes
- Don't change `.appxmanifest` capabilities unless the task explicitly requires it
- Don't downgrade SDK versions or LangVersion
- Prefer existing services/patterns over introducing new singletons or global state
- Target `develop` for PRs, not `main`
- Link issues with `Fixes #123` in PR descriptions
- Keep edits scoped and minimal
