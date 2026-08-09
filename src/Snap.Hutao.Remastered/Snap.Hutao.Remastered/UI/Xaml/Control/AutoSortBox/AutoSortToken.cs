// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;

namespace Snap.Hutao.Remastered.UI.Xaml.Control.AutoSortBox;

public sealed partial class AutoSortToken : ObservableObject
{
    public AutoSortToken(AutoSortTokenKind kind, string value, Uri? iconUri = null)
    {
        Kind = kind;
        Value = value;
        IconUri = iconUri;
    }

    public AutoSortTokenKind Kind { get; }

    public string Value { get; }

    public Uri? IconUri { get; }

    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    [ObservableProperty]
    public partial int SortOrder { get; set; }

    [ObservableProperty]
    public partial bool IsDescending { get; set; } = true;
}
