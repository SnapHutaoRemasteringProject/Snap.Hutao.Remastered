// Copyright (c) 2026 DGP Studio. All rights reserved.
// Licensed under the MIT license.
// Adapted from Snap.Nicole.

using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System.Collections.Generic;
using System.Globalization;

namespace Snap.Hutao.Remastered.Resource.Localization.AI;

internal sealed class StringResourceProxy : ObservableObject
{
    public static IReadOnlyList<string> SupportedCultures { get; } = ["zh-Hans", "en"];

    public static StringResourceProxy Default { get; } = new();

    public CultureInfo CurrentCulture
    {
        get;
        set
        {
            if (field == value)
            {
                return;
            }

            field = value;

            OnPropertyChanged("Item[]");
        }
    } = CultureInfo.CurrentCulture;

    public string this[string name]
    {
        get => SH.ResourceManager.GetString(string.Intern(name), CurrentCulture) ?? string.Empty;
    }

    public string this[SHName name]
    {
        get => this[$"{name}"];
    }

    public BindingBase CreateBinding(string name)
    {
        return new Binding
        {
            Source = this,
            Path = new PropertyPath(string.Intern($"[{name}]")),
            Mode = BindingMode.OneWay,
        };
    }
}
