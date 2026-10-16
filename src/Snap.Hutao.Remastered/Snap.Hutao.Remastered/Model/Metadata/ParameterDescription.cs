// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;

namespace Snap.Hutao.Remastered.Model.Metadata;

public sealed partial class ParameterDescription : ObservableObject
{
    public ParameterDescription(string parameter, string description)
    {
        Parameter = parameter;
        Description = description;
    }

    public string Parameter { get; }

    public string Description { get; }

    [ObservableProperty]
    public partial bool IsSpecial { get; set; }
}
