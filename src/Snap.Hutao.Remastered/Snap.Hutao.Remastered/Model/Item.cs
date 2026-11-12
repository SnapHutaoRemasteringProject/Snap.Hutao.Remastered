// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;
using Snap.Hutao.Remastered.Model.Intrinsic;

namespace Snap.Hutao.Remastered.Model;

public partial class Item : ObservableObject, INameIcon<Uri>
{
    public string Name { get; init; } = default!;

    public Uri Icon { get; init; } = default!;

    public Uri Badge { get; init; } = default!;

    public QualityType Quality { get; init; }

    public uint Id { get; init; }
}
