# Metadata bundle schema

`*.proto` is the wire contract of `GenshinMetadata.*.hutaobin`, produced by
[`MetadataBuilder`](https://github.com/SnapHutaoRemasteringProject/Snap.Hutao.Remastered)
(`D:\SnapHutaoRemasteringProject\MetadataBuilder` on the build machine).

| Item | Value |
|---|---|
| Source | `MetadataBuilder/Proto` @ `cb1eb30` (2026-11-22), with the identifier/`uint` field-type change described below |
| Generator | `Grpc.Tools` 2.72.0 (`protoc` 30.0) |
| Committed output | `Proto/Generated/*.cs` — compiled as normal source; the build does **not** run protoc |
| Regeneration | `powershell -File scripts/Generate-MetadataProto.ps1` (`-Check` fails when the committed output is stale) |
| Deviations from the builder schema | `package snap.hutao.metadata;` → `package snap.hutao.remastered.metadata;` and `option csharp_namespace = "MetadataBuilder.Proto";` removed, so the generated namespace is `Snap.Hutao.Remastered.Metadata` instead of `MetadataBuilder.Proto`. The package name only affects descriptor identifiers (no `Any` is used), so the wire format is unchanged. |

## Why the generated C# is committed

The application used to generate from `.proto` at build time. `Grpc.Tools` only adds its generated
files to `@(Compile)` when protoc actually ran in that build: after a build that generated but did not
compile (interrupted build, `bin` cleaned but `obj` kept), every later incremental build failed with
`CS0234: namespace Snap.Hutao.Remastered.Metadata does not exist`. Committing the generated sources
makes the build deterministic and offline-friendly, and it is the fallback documented in
[`docs/proto-metadata.md`](../../../../../docs/proto-metadata.md) §10.5.

Do **not** hand-edit `Proto/Generated`. Change the schema in `MetadataBuilder/Proto`, copy the files
here, run the script, and commit both the `.proto` files and the regenerated `Generated` folder.

## Identifier fields are `uint32`

The builder's original schema used `int64`/`int32` for identifiers, levels, ranks and counts, while the
application model stores them as `uint` (its generated identity structs wrap `uint`). The schema now
declares those fields as `uint32`, so a mapping cannot receive a value the model cannot represent and
does not need overflow checks. `MetadataBuilder`'s `ProtoMapper`/`ProtoJsonMapper` convert to
`uint`/`ulong` for these fields. Values below `2^31` encode identically to the previous `int64`
declaration, which is why an existing bundle remains readable while the producer is rebuilt.

Rules:

- Any schema change that renames, removes, renumbers or changes field semantics must bump
  `MetadataInfo.schema_version` (still missing on the producer side, see
  [`docs/proto-metadata.md`](../../../../../docs/proto-metadata.md) §10.1). Until it exists the client
  validates structure (language, `LanguageMetadata` field coverage) and cannot detect a version skew,
  so producer and consumer must ship together.
- Client category coverage is asserted by `ProtoCategoryTable` (30 adapters + 5 ignored categories must
  equal every message field of `LanguageMetadata`).
