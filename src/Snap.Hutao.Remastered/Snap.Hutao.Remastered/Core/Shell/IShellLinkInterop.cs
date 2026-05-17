// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.Core.Shell;

public interface IShellLinkInterop
{
    bool TryCreateDesktopShortcut();

    bool TryCreateGameLaunchShortcut();
}