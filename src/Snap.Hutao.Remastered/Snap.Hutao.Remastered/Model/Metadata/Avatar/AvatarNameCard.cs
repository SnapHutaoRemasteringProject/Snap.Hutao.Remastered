// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.Model.Metadata.Avatar;

public sealed partial class AvatarNameCard : NameDescription
{
    public required string Icon { get; init; }

    public required string PicturePrefix { get; init; }
}