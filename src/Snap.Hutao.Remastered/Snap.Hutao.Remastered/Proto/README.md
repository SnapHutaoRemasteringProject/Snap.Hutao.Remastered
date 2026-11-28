# Metadata bundle schema

`*.proto` under this folder is a **reference contract** for `GenshinMetadata.*.hutaobin`,
produced by `MetadataBuilder` (`D:\SnapHutaoRemasteringProject\MetadataBuilder` on the build
machine). It is no longer compiled: the wire contract is expressed as `protobuf-net` attributes on
`Model/Metadata` (the application) and on `MetadataBuilder/Model` (the producer).

| Item | Value |
|---|---|
| Runtime serializer | `protobuf-net.Core` 3.4.51, compile-time model (`[ProtoModel]`), no reflection model |
| Client model | `[ProtoContract]` / `[ProtoMember]` on `Model/Metadata`, plus `MetadataProtoModel` (surrogates) and `Wire/MetadataBundle` |
| Producer model | `[ProtoContract]` / `[ProtoMember]` on `MetadataBuilder/Model`, plus `MetadataProtoModel` and `Core/Proto/Wire/MetadataBundle` |
| Generated code | None. `Proto/Generated` and `scripts/Generate-MetadataProto.ps1` were removed |
| Build-time tools | No `protoc` / `Grpc.Tools`; the app and the producer only need `protobuf-net.Core` |
| `[Experimental]` | `[ProtoModel]` is gated by `PBN9001`; both projects add it to `NoWarn` |

## Field numbering

Field numbers are taken from the previous `.proto` schema except where the application model's
inheritance forces a different layout:

- `Material` is the flattened `DisplayItem` (1..8) plus `material_type` 9 and `effect_description` 10;
  the shared members keep the `DisplayItem` numbers because protobuf-net emits base members first.
- `Skill` (talents) uses the `AvatarTalent` numbers 1..6. `ProudSkill` is a standalone model with the
  original `AvatarSkill` numbers (`group_id` 1, `proud` 2, `id` 3, `name` 4, `description` 5,
  `special_description` 6, `special_energy_type` 7, `icon` 8, `cd_time` 9).
- `IdLevelParameters` uses `id` 1, `level` 2, `parameters` 3; `WeaponTypeValue` uses `init_value` 1,
  `type` 2, `value` 3.
- 44 identity structs are scalar `uint32` varints; `DateTimeOffset` is an ISO-8601 string (a public
  `*Wire` member keeps the strongly typed property).
- The producer encodes every `double` as `float` (a model-level scalar surrogate), because the client
  model uses `float` for all metadata numbers.

## Producer / consumer must ship together

The bundle was rewritten under the new layout, so an older bundle is **not** readable and an older
client cannot read a new bundle. There is still no `schema_version`/manifest on the producer side
(`docs/proto-metadata.md` §10.1); until it exists, producer and consumer ship in lockstep.

Category coverage is asserted by `ProtoCategoryTable` (30 consumed categories; furniture and
`ReliquaryAffixWeight` are intentionally ignored and are no longer written into the bundle).
