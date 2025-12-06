// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core.Property;

namespace Snap.Hutao.Remastered.ViewModel.Abstraction;

internal interface IViewUnloadAware
{
    IProperty<bool> IsViewUnloaded { get; }
}