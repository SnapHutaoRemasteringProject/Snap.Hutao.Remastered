// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;

namespace Snap.Hutao.Remastered.Model.Metadata.Avatar;

[ProtoContract]
[WinRT.GeneratedBindableCustomProperty([nameof(Title), nameof(Context)], [])]
public sealed partial class Fetter : ObservableObject
{
    [ProtoMember(1)]
    public string Title { get; init => SetProperty(ref field, value); }

    [ProtoMember(2)]
    public string Context { get; init => SetProperty(ref field, value); }
}
