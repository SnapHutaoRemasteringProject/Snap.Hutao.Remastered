// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Model.InterChange.Achievement;
using Snap.Hutao.Remastered.Model.InterChange.Inventory;
using Snap.Hutao.Remastered.ViewModel.Game;

namespace Snap.Hutao.Remastered.Service.Yae;

internal interface IYaeService
{
    ValueTask<UIAF?> GetAchievementAsync(IViewModelSupportLaunchExecution viewModel);

    ValueTask<UIIF?> GetInventoryAsync(IViewModelSupportLaunchExecution viewModel);
}