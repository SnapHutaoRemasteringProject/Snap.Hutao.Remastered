// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;
using ProtoBuf;

namespace Snap.Hutao.Remastered.Model;

[ProtoContract(IgnoreUnknownSubTypes = true)]
public partial class NameDescription : ObservableObject
{
    public NameDescription()
    {
    }

    public NameDescription(string name, string description)
    {
        Name = name;
        Description = description;
    }

    public static NameDescription Default { get; } = new(SH.ModelNameValueDefaultName, SH.ModelNameValueDefaultDescription);

    [ObservableProperty]
    [ProtoMember(1)]
    public partial string Name { get; set; } = default!;

    [ObservableProperty]
    [ProtoMember(2)]
    public partial string Description { get; set; } = default!;
}
