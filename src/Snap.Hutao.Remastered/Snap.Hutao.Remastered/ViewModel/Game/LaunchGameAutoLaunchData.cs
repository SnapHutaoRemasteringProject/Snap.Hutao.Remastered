// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Service.Navigation;

namespace Snap.Hutao.Remastered.ViewModel.Game;

public sealed class LaunchGameAutoLaunchData : NavigationExtraData<string>
{
    private LaunchGameAutoLaunchData(string? uid)
        : base(uid ?? string.Empty)
    {
    }

    public static INavigationCompletionSource CreateForLaunch(string? uid)
    {
        return new LaunchGameAutoLaunchData(uid);
    }
}
