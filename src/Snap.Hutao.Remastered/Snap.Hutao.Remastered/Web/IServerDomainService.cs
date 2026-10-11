// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.Web;

public interface IServerDomainService
{
    /// <summary>
    /// Syncs the mode from an external source, e.g. after AppOptions loaded the persisted value.
    /// </summary>
    void SetMode(ServerDomainMode mode);

    bool IsBackupMode();

    string GetHomaRoot();

    string GetApiRoot();

    string GetRootDomain();

    /// <summary>
    /// Auto fallback: switches to the backup domain in-memory only, without persisting the setting.
    /// </summary>
    void TryAutoFallback();

    /// <summary>
    /// Rewrites the host of <paramref name="uri"/> to the domain of the current mode when it points to
    /// the other known domain. Scheme, port, path, query and fragment are preserved as written.
    /// </summary>
    Uri? RewriteHostToCurrentMode(Uri? uri);
}
