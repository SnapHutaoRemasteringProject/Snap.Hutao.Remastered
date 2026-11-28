// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

#pragma warning disable CS8618

namespace Snap.Hutao.Remastered.Model.Metadata.Avatar;

[ProtoContract]
public sealed partial class AvatarNameCard : NameDescription
{
    [ProtoMember(3)]
    public string Icon { get; init; }

    [ProtoMember(4)]
    public string PicturePrefix { get; init; }
}
