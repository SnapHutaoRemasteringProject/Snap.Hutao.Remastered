// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using ProtoBuf.Meta;
using Snap.Hutao.Remastered.Service.Metadata.Provider.Proto.Wire;

namespace Snap.Hutao.Remastered.Service.Metadata.Provider.Proto;

/// <summary>
/// Compile-time protobuf-net model for the metadata bundle. The domain models are the wire
/// contract: every class that the bundle carries is annotated with <c>[ProtoContract]</c> and
/// <c>[ProtoMember]</c> in <c>Model/Metadata</c>. Identity structs carry a scalar serializer declared
/// next to the structs; timestamps travel as ISO-8601 strings through a dedicated member.
/// </summary>
/// <remarks>
/// Only <c>protobuf-net.Core</c> is referenced, so no runtime/reflection model is available:
/// every read and write goes through <see cref="Instance"/>.
/// </remarks>
[ProtoModel]
[ProtoSerializable(typeof(MetadataBundle))]
public partial class MetadataProtoModel : TypeModel
{
}
