// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;

namespace Snap.Hutao.Remastered.Model.Metadata.Avatar;

public sealed partial class Fetter : ObservableObject
{
    public required string Title { get; init => SetProperty(ref field, value); }

    public required string Context { get; init => SetProperty(ref field, value); }
}
