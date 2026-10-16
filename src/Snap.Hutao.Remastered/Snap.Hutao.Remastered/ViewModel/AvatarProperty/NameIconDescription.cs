// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;
using Snap.Hutao.Remastered.Model;

namespace Snap.Hutao.Remastered.ViewModel.AvatarProperty;

public abstract partial class NameIconDescription : NameDescription, INameIcon<Uri>
{
    [ObservableProperty]
    public partial Uri Icon { get; set; } = default!;
}
