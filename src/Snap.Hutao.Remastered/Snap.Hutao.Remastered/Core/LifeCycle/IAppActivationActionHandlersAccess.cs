// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.Core.LifeCycle;

public interface IAppActivationActionHandlersAccess
{
    ValueTask HandleLaunchGameActionAsync(string? uid = null);

    ValueTask NavigateMainWindowAsync(Type pageType);
}
