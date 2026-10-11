// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.Web;

[Service(ServiceLifetime.Singleton, typeof(IServerDomainService))]
public sealed class ServerDomainService : IServerDomainService
{
    private const string PrimaryDomain = "snaphutaorp.org";
    private const string BackupDomain = "hutaorp.org";

    private static readonly Lazy<IServerDomainService> Shared = new(static () => Ioc.Default.GetRequiredService<IServerDomainService>());

    private volatile ServerDomainMode currentMode = ServerDomainMode.Primary;

    /// <summary>
    /// Gets the instance owned by the container.
    /// </summary>
    /// <remarks>
    /// Only for the consumers that cannot take constructor injection: XAML value converters (their instances
    /// and static field initializers are owned by the XAML parser) and the static system proxy singleton,
    /// which is created while the container is still being built. Everything else must inject
    /// <see cref="IServerDomainService"/>.
    /// </remarks>
    public static IServerDomainService Current
    {
        get => Shared.Value;
    }

    public void SetMode(ServerDomainMode mode)
    {
        currentMode = mode;
    }

    public bool IsBackupMode()
    {
        return currentMode is ServerDomainMode.Backup;
    }

    public string GetHomaRoot()
    {
        return IsBackupMode() ? "https://homa.hutaorp.org" : "https://homa.snaphutaorp.org";
    }

    public string GetApiRoot()
    {
        return IsBackupMode() ? "https://api.hutaorp.org" : "https://api.snaphutaorp.org";
    }

    public string GetRootDomain()
    {
        return IsBackupMode() ? "https://hutaorp.org" : "https://snaphutaorp.org";
    }

    public void TryAutoFallback()
    {
        if (currentMode is ServerDomainMode.Primary)
        {
            currentMode = ServerDomainMode.Backup;
        }
    }

    /// <remarks>
    /// Only the host is touched: a plain <see cref="string.Replace(string, string)"/> over the whole URL
    /// would also rewrite query values and could produce a host such as <c>api.snapsnaphutaorp.org</c>,
    /// because <c>snaphutaorp.org</c> itself ends with <c>hutaorp.org</c>.
    /// </remarks>
    public Uri? RewriteHostToCurrentMode(Uri? uri)
    {
        if (uri is null || !uri.IsAbsoluteUri)
        {
            return uri;
        }

        (string from, string to) = IsBackupMode() ? (PrimaryDomain, BackupDomain) : (BackupDomain, PrimaryDomain);
        if (!TryReplaceHostDomain(uri.Host, from, to, out string host))
        {
            return uri;
        }

        // Rewrite the authority textually instead of through UriBuilder, which would always
        // materialize the port ("https://api.snaphutaorp.org:443/x") and thus change the URL.
        string original = uri.OriginalString;
        int schemeSeparator = original.IndexOf("://", StringComparison.Ordinal);
        if (schemeSeparator < 0)
        {
            return uri;
        }

        int authorityStart = schemeSeparator + 3;
        int authorityEnd = original.IndexOfAny(['/', '?', '#'], authorityStart);
        if (authorityEnd < 0)
        {
            authorityEnd = original.Length;
        }

        string authority = original[authorityStart..authorityEnd];
        int hostStart = authority.LastIndexOf('@') + 1; // skip user info, if any
        int portSeparator = authority.IndexOf(':', hostStart);
        int hostEnd = portSeparator < 0 ? authority.Length : portSeparator;

        return new(
            string.Concat(original.AsSpan(0, authorityStart + hostStart), host, original.AsSpan(authorityStart + hostEnd)),
            UriKind.Absolute);
    }

    private static bool TryReplaceHostDomain(string host, string from, string to, [NotNullWhen(true)] out string result)
    {
        if (host.Equals(from, StringComparison.OrdinalIgnoreCase))
        {
            result = to;
            return true;
        }

        // Only a dot separated sub-domain matches: "api.hutaorp.org" → "api.snaphutaorp.org",
        // while "snaphutaorp.org" must not be treated as a sub-domain of "hutaorp.org".
        int separatorIndex = host.Length - from.Length - 1;
        if (separatorIndex >= 0
            && host[separatorIndex] is '.'
            && host.AsSpan(separatorIndex + 1).Equals(from, StringComparison.OrdinalIgnoreCase))
        {
            result = string.Concat(host.AsSpan(0, separatorIndex + 1), to);
            return true;
        }

        result = host;
        return false;
    }
}
