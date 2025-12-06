// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core.Abstraction;
using Snap.Hutao.Remastered.UI.Xaml;

namespace Snap.Hutao.Remastered.ViewModel.Abstraction;

internal interface IViewModel : IPageScoped, IResurrectable, IViewUnloadAware
{
    CancellationToken CancellationToken { get; set; }

    SemaphoreSlim CriticalSection { get; }

    IDeferContentLoader? DeferContentLoader { get; set; }

    void Uninitialize();
}