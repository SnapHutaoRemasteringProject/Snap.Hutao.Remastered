// Copyright (c) 2026 DGP Studio. All rights reserved.
// Licensed under the MIT license.
// Adapted from Snap.Nicole.

using Snap.Hutao.Remastered.Resource.Localization.AI;

namespace Snap.Hutao.Remastered.ViewModel.AI.Settings;

[WinRT.GeneratedBindableCustomProperty([nameof(Value)], [])]
internal sealed partial class SettingsItem
{
    public SettingsItem(StringResourceValue label, object? value)
    {
        Label = label;
        Value = value;
    }

    public StringResourceValue Label { get; }

    public object? Value { get; }
}
