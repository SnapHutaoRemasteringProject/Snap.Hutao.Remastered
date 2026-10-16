// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;

namespace Snap.Hutao.Remastered.Model;

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
    public partial string Name { get; set; } = default!;

    [ObservableProperty]
    public partial string Description { get; set; } = default!;
}
