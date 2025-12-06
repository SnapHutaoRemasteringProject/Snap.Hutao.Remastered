// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.Core.LifeCycle;

internal interface IAppActivationActionHandlersAccess
{
    ValueTask HandleLaunchGameActionAsync(string? uid = null);
}