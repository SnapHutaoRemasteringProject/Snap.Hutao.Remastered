// Copyright (c) 2026 DGP Studio. All rights reserved.
// Licensed under the MIT license.
// Adapted from Snap.Nicole.

using Snap.Hutao.Remastered.Resource.Localization.AI;

namespace Snap.Hutao.Remastered.ViewModel.AI.Settings;

internal sealed record SettingsItem<T>
{
    public SettingsItem(StringResourceValue label, T value)
    {
        Label = label;
        Value = value;
    }

    public StringResourceValue Label { get; }

    public T Value { get; }
}
