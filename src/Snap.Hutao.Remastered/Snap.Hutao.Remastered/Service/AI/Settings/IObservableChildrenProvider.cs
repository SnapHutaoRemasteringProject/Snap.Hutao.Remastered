// Copyright (c) 2026 DGP Studio. All rights reserved.
// Licensed under the MIT license.
// Adapted from Snap.Nicole.

using System.Collections.Generic;
using System.ComponentModel;

namespace Snap.Hutao.Remastered.Service.AI.Settings;

internal interface IObservableChildrenProvider
{
    IEnumerable<INotifyPropertyChanged> EnumerateObservableChildren();
}
