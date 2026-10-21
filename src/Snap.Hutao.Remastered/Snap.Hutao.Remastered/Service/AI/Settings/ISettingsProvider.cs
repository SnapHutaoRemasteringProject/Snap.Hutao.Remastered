// Copyright (c) 2026 DGP Studio. All rights reserved.
// Licensed under the MIT license.
// Adapted from Snap.Nicole.

using System.ComponentModel;

namespace Snap.Hutao.Remastered.Service.AI.Settings;

internal interface ISettingsProvider<out T>
    where T : class, INotifyPropertyChanged
{
    T CurrentValue { get; }
}
