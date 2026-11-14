// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core.IO;
using Snap.Hutao.Remastered.Factory.IO;
using Snap.Hutao.Remastered.Service.Game;
using System.Buffers;
using System.Collections.Specialized;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace Snap.Hutao.Remastered.Service.GachaLog.QueryProvider;

[Service(ServiceLifetime.Transient, typeof(IGachaLogQueryProvider), Key = RefreshOptionKind.WebCache)]
public sealed partial class GachaLogQueryWebCacheProvider : IGachaLogQueryProvider
{
    private readonly IMemoryStreamFactory memoryStreamFactory;
    private readonly IGameService gameService;
    private readonly CultureOptions cultureOptions;

    [GeneratedConstructor]
    public partial GachaLogQueryWebCacheProvider(IServiceProvider serviceProvider);

    [GeneratedRegex("^[1-9]+?\\.[0-9]+?\\.[0-9]+?\\.[0-9]+?$")]
    private static partial Regex VersionRegex { get; }

    public static string GetCacheFile(string path)
    {
        string exeName = Path.GetFileName(path);
        string dataFolder = exeName is GameConstants.GenshinImpactFileName
            ? GameConstants.GenshinImpactData
            : GameConstants.YuanShenData;

        string? directory = Path.GetDirectoryName(path);
        ArgumentNullException.ThrowIfNull(directory);
        DirectoryInfo webCacheFolder = new(Path.Combine(directory, dataFolder, "webCaches"));
        if (webCacheFolder.Exists)
        {
            DirectoryInfo? latestVersionCacheFolder = webCacheFolder
                .EnumerateDirectories()
                .Where(dir => VersionRegex.IsMatch(dir.Name))
                .MaxBy(dir => new Version(dir.Name));

            latestVersionCacheFolder ??= webCacheFolder;
            return Path.Combine(latestVersionCacheFolder.FullName, @"Cache\Cache_Data\data_2");
        }

        return string.Empty;
    }

    public async ValueTask<ValueResult<bool, GachaLogQuery>> GetQueryAsync()
    {
        (bool isOk, string path) = await gameService.GetGamePathAsync().ConfigureAwait(false);
        if (!isOk || string.IsNullOrEmpty(path))
        {
            return new(false, GachaLogQuery.Invalid(SH.ServiceGachaLogUrlProviderCachePathInvalid));
        }

        string cacheFile = GetCacheFile(path);
        if (!File.Exists(cacheFile))
        {
            return new(false, GachaLogQuery.Invalid(SH.FormatServiceGachaLogUrlProviderCachePathNotFound(cacheFile)));
        }

        // Must copy the file to avoid the following exception:
        // System.IO.IOException: The process cannot access the file
        TempFileStream fileStream;
        try
        {
            fileStream = TempFileStream.CopyFrom(cacheFile, FileMode.Open, FileAccess.Read);
        }
        catch (UnauthorizedAccessException)
        {
            return new(false, GachaLogQuery.Invalid(SH.ServiceGachaLogUrlProviderCopyWebCacheUnauthorizedAccess));
        }

        using (fileStream)
        {
            using (MemoryStream memoryStream = await memoryStreamFactory.GetStreamAsync(fileStream).ConfigureAwait(false))
            {
                string? url = Match(memoryStream, cacheFile.Contains(GameConstants.GenshinImpactData, StringComparison.OrdinalIgnoreCase));
                if (string.IsNullOrEmpty(url))
                {
                    return new(false, GachaLogQuery.Invalid(SH.ServiceGachaLogUrlProviderCacheUrlNotFound));
                }

                if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri))
                {
                    return new(false, GachaLogQuery.Invalid(SH.ServiceGachaLogUrlProviderCacheUrlNotFound));
                }

                NameValueCollection query = HttpUtility.ParseQueryString(uri.Query);
                if (query["authkey"] is not { Length: > 0 })
                {
                    return new(false, GachaLogQuery.Invalid(SH.ServiceGachaLogUrlProviderCacheUrlNotFound));
                }

                query.Set("lang", cultureOptions.LanguageCode);
                return new(true, new(query.ToString()!));
            }
        }
    }

    private static string? Match(MemoryStream stream, bool isOversea)
    {
        using (IMemoryOwner<byte> memoryOwner = MemoryPool<byte>.Shared.RentExactly((int)stream.Length))
        {
            Span<byte> span = memoryOwner.Memory.Span;
            stream.ReadExactly(span);

            ReadOnlySpan<byte> match = isOversea
                ? "https://gs.hoyoverse.com/genshin/event/e20190909gacha"u8
                : "https://webstatic.mihoyo.com/hk4e/event/e20190909gacha"u8;

            int index = span.LastIndexOf(match);
            if (index >= 0)
            {
                ReadOnlySpan<byte> target = span[index..];
                int terminator = target.IndexOf((byte)0);
                return terminator < 0 ? null : Encoding.UTF8.GetString(target[..terminator]);
            }
            return null;
        }
    }
}
