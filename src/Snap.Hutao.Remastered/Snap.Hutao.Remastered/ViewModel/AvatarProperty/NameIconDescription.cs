// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Model;

namespace Snap.Hutao.Remastered.ViewModel.AvatarProperty;

internal abstract class NameIconDescription : NameDescription, INameIcon<Uri>
{
    public Uri Icon { get; set; } = default!;
}