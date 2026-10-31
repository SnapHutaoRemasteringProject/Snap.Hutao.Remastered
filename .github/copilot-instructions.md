Your task is to “onboard” this repository to the Copilot coding agent by adding this .github/copilot-instructions.md file. It gives the agent the minimum, durable context it needs to understand the project, validate changes, and open mergeable PRs.

These instructions are repository-wide and task-agnostic. If something here contradicts ad-hoc guidance in an issue or PR, prefer the ad-hoc guidance for that task.

<Goals>
- Keep generated PRs mergeable by aligning with repository conventions; build and packaging validation will be performed by GitHub Actions.
- Minimize broken shell or PowerShell steps by pinning prerequisites and the order of operations.
- Reduce unnecessary codebase exploration by pointing to the right files and patterns first.
</Goals>

<Limitations>
- Do not include issue-specific plans or debugging transcripts.
- Keep guidance concise (≈2 pages). Link to existing scripts or configs in-repo rather than inlining them.
- Localization policy: Base language is Chinese (Simplified). English is also maintained by the core team; all other languages are community-contributed via Crowdin. If a PR introduces new UI strings, only add the new strings to the Chinese resource file at src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Resource/Localization/SH.resx. Do not edit any other locale files (including English) in that PR; leave them untranslated.
</Limitations>

---

## High-level overview

- What this is. Snap Hutao (胡桃工具箱) is a C# WinUI 3 desktop application packaged as MSIX. It is an open-source toolkit that enhances the Genshin Impact experience on Windows without modifying the game client.
- Target OS. Runs on Windows 11 (22H2+) and Windows 10 22H2 with the January 2025 update. Agent work should assume a modern, fully updated Windows dev box.
- Key tech. .NET 11 (no `global.json` SDK pin: the installed 11.x SDK is used, and CI installs major `11`), Windows App SDK / WinUI 3 (XAML), GitHub Actions CI, StyleCop and the repository source generator, plus validation utilities such as Microsoft.VisualStudio.Validation (see usage policy below).
- License and scope. MIT. The app integrates official game data and community features; it must not introduce destructive client modifications.

Rule of thumb: let the repository's `.vsconfig` provision the Visual Studio workloads and the Single-project MSIX packaging extension, and use the .NET SDK CLI for restore, build, and test.

---

## Repository layout (what to look for first)

- Top level
  - .github  — issue templates and workflows; this file lives here.
  - res      — assets and miscellaneous resources.
  - src/Snap.Hutao.Remastered  — the solution root for the desktop app (primary code lives here).
  - Build and CI configuration in the root (for example build.cake, NuGet.Config, CI YAML).

- Inside src/Snap.Hutao.Remastered
  - Solution: Snap.Hutao.Remastered.slnx (primary; SLNX format).
  - Main app project: Snap.Hutao.Remastered (WinUI 3 XAML and C#).
  - Common folders you will work in:
    - UI (XAML views, controls) and ViewModel (MVVM presentation).
    - Service (application and business services; network, storage, background work).
    - Model (domain and data models).
    - Core and Extension (infrastructure, helpers, extensions).
    - Web (HTTP or API integrations) and Win32 (interop).
    - API (the plugin contract handed to third-party plugins).
  - Important config:
    - App.xaml and App.xaml.cs (app startup and resources), Package.appxmanifest (MSIX).
    - .editorconfig, settings.xamlstyler, GlobalUsing.cs, BannedSymbols.txt (coding standards and guardrails).

Search shortcuts: Package.appxmanifest for capabilities or identity, .editorconfig and settings.xamlstyler for style rules, GlobalUsing.cs for shared namespaces, *.xaml plus ViewModel for MVVM entry points.

---

## Branching and PR policy (follow strictly)

- Work on dev for feature and bug-fix branches; target PRs to dev.
- CI builds dev (alpha workflow, on push and on PRs into dev) and every other branch except main, release, and dependabot/** (canary workflow). Alpha artifacts may be produced by CI for testing.
- Link issues with closing keywords in PR descriptions when appropriate, for example `Fixes #123`.

---

## Prerequisites (Windows only)

1) Windows: Windows 11 22H2+ or Windows 10 22H2 with the latest updates. Enable Developer Mode.
2) Visual Studio 2022 with workloads:
   - .NET desktop development
   - Desktop development with C++
   - Windows application development
3) MSIX tooling: Single-project MSIX Packaging Tools (the exact extension is listed in `.vsconfig`) if your Visual Studio installation does not include it.
4) SDK policy: A .NET 11 SDK is all that is required. The repo deliberately has no `global.json`, so any `11.x` feature band works; CI installs only the major version (`actions/setup-dotnet`, `dotnet-version: 11`). Do not add a `global.json` SDK pin, downgrade SDK versions, or lower LangVersion.
5) Optional runtime UX: WebView2 Runtime, Segoe Fluent Icons font, MSVC Runtime if required by features.

If the agent runs headless, prefer CLI restore, build, and test for validation and rely on CI to produce MSIX artifacts.

---

## Build, run, and validate (minimal, reliable sequence)

Use this order on a clean clone. Local build is recommended for rapid feedback but not required; GitHub Actions will perform the authoritative validation.

1) Restore
   dotnet restore src/Snap.Hutao.Remastered/Snap.Hutao.Remastered.slnx

2) Build (Debug)
   dotnet build src/Snap.Hutao.Remastered/Snap.Hutao.Remastered.slnx -c Debug

   Expect WinUI 3 XAML compilation and analyzer checks. Fix analyzer violations before proposing changes.

3) Run for development
   - Open src/Snap.Hutao.Remastered/Snap.Hutao.Remastered.slnx in Visual Studio.
   - Set the main project as startup, select x64, press F5. Packaged debugging will register the app locally.
   - The built exe can also be launched directly (unpackaged) from
     src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/bin/x64/Debug/net11.0-windows10.0.26100.0/win-x64/.

4) Package when needed
   - Use Visual Studio Publish or Package for MSIX.
   - CI may also produce Alpha packages for dev. Local install of CI-signed packages can require installing the provided certificate.

5) Tests
   dotnet test src/Snap.Hutao.Remastered/Snap.Hutao.Remastered.slnx

   Note: the test project has no ProjectReference to the app; it only compiles a fixed set of production files linked through `<Compile Include>` in Snap.Hutao.Remastered.Test.csproj. Add or update tests for non-UI logic when you change behavior, and add a link if the file is not covered yet.

Common validation before opening a PR:
- Solution builds cleanly and analyzer warnings are resolved per repository policy.
- App starts in Debug; smoke-test the feature you touched.
- Do not change Package.appxmanifest capabilities unless required by the task and documented in the PR.

---

## Coding style and contribution patterns

- MVVM: Put UI behavior in ViewModels. Use async APIs for I/O and keep the UI thread responsive.
- Consistency first: Mirror existing folder structure, naming, and patterns. Prefer existing services/helpers over new global/singleton patterns.
- Analyzers: Fix StyleCop and other analyzer diagnostics. Follow the rules in .editorconfig and settings.xamlstyler (naming, docs, layout).
- Validation & errors: Use established guard/validation utilities; fail fast on bad inputs; handle network/storage exceptions gracefully.
- XAML: Reuse existing styles/resources; keep bindings simple and observable; avoid UI thread blocking.
- Security & privacy: Don’t log secrets or game tokens; follow existing storage conventions.

---

## Localization and strings (enforced)

- Base language is Chinese (Simplified). English is maintained by the core team; other languages are translated by the community via Crowdin.
- When adding new UI strings in a PR, only add them to the Chinese resource file:
  src/Snap.Hutao.Remastered/Snap.Hutao.Remastered/Resource/Localization/SH.resx
- Do not create or modify any other locale resource files (including English) in that PR; leave them untranslated. The core team and Crowdin will handle downstream updates.
- Prefer resource bindings over hard-coded strings. Reuse existing keys where possible; introduce new keys only when needed and name them consistently.

---

## Where to implement changes (quick map)

- New UI feature → UI (XAML) plus ViewModel (presentation) plus Service (logic or data) plus Model (types).
- Network or API work → Web (HTTP clients, DTOs) plus relevant Service.
- Interop → Win32 (P/Invoke or wrappers). Follow precedent conservatively to avoid regressions.
- Cross-cutting → Core and Extension for helpers, dependency injection, and reusable infrastructure.

---

## CI interaction (what the agent should expect)

- Pushes to feature branches trigger build checks; some branches produce Alpha artifacts.
- Build and packaging validation is performed by GitHub Actions and is the source of truth.
- If CI fails on analyzers or formatting, align code with the repository’s rules rather than suppressing diagnostics.
- Keep local steps minimal and deterministic; rely on CI for signing and distribution.

---

## Agent operating guidelines

- Trust this file first. Explore or run extra commands only if information is missing or build errors indicate a mismatch.
- Do not downgrade SDKs or language level. Avoid changing packaging or signing settings unless the task explicitly requires it.
- Keep edits scoped. Touch the smallest set of files; update tests or docs alongside code when behavior changes.
- PR hygiene. Use clear commit messages, link issues with keywords, and summarize the validation steps you performed such as build, tests, and manual smoke checks.

---
