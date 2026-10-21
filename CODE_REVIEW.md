# Implementation verification log

The historical review report referenced by CLAUDE.md was absent from this checkout. This file records new verification only; it does not reconstruct the older findings.

## 14. Verification history

| Date | Scope | Verified | Not verified |
|---|---|---|---|
| 2026-10-20 | Default R2R delivery for local Debug and CI packaging | CLI/VS MSBuild Debug builds; bin/AppX/publish image identity; unsigned MSIX payload; four Cake task graphs; 167 environment-independent tests; deployed-image JSON/EF/plugin/script checks | Remote CI, signing/installation, interactive F5, startup performance |
| 2026-10-20 | Calendar material icons outside material-switch branches | Valid cached PNG and populated Image source inspected; isolated Debug build; generated bindings rooted on Grid; one non-hit-test icon outside Case and both flyouts retained | Rendering after full redeploy in the user's running app; alignment/URI-reset experiments did not resolve the old layout and were not retained |
