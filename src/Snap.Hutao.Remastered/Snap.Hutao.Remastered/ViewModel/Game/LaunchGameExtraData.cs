// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Service.Navigation;

namespace Snap.Hutao.Remastered.ViewModel.Game;

public sealed class LaunchGameExtraData : NavigationExtraData<string>
{
    private LaunchGameExtraData(string uid, bool directLaunch = false)
        : base(uid)
    {
        DirectLaunch = directLaunch;
    }

    public bool DirectLaunch { get; }

    public static INavigationCompletionSource CreateForUid(string? uid)
    {
        return uid is null ? Default : new LaunchGameExtraData(uid);
    }

    public static LaunchGameExtraData CreateForDirectLaunch(string? uid)
    {
        return new LaunchGameExtraData(uid ?? string.Empty, directLaunch: true);
    }
}