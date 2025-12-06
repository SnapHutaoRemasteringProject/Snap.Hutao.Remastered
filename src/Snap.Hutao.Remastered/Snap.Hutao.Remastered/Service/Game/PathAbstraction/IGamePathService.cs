// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.Service.Game.PathAbstraction;

internal interface IGamePathService
{
    ValueTask<ValueResult<bool, string>> SilentLocateGamePathAsync();

    ValueTask SilentLocateAllGamePathAsync();
}