// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core;
using Snap.Hutao.Remastered.Core.Setting;
using System.IO;

namespace Snap.Hutao.Remastered.Service.Metadata.Provider.Proto;

/// <summary>
/// Local layout and limits of the proto bundle, plus the developer override path used to test a
/// bundle that has not been published to the metadata endpoint yet.
/// </summary>
[Service(ServiceLifetime.Singleton)]
public sealed partial class MetadataBundleOptions
{
    public const string BundleFileName = MetadataBundleConstants.BundleFileName;
    public const string TempFileSuffix = MetadataBundleConstants.TempFileSuffix;

    /// <summary>
    /// A single-locale bundle is a few MiB; the combined bundle is under 64 MiB. Anything larger is
    /// treated as a corrupt or malicious payload.
    /// </summary>
    public const int MaxEncryptedBytes = MetadataBundleConstants.MaxEncryptedBytes;

    public static TimeSpan DownloadTimeout
    {
        get => MetadataBundleConstants.DownloadTimeout;
    }

    private readonly CultureOptions cultureOptions;

    [GeneratedConstructor]
    public partial MetadataBundleOptions(IServiceProvider serviceProvider);

    /// <summary>
    /// Developer switch: an absolute bundle path that bypasses the download. Not exposed in the UI.
    /// </summary>
    public string? OverrideBundlePath
    {
        get => LocalSetting.Get(SettingKeys.MetadataBundlePath, string.Empty) is { Length: > 0 } path ? path : null;
    }

    [field: MaybeNull]
    public string LocalizedDirectory
    {
        get
        {
            if (field is null)
            {
                field = Path.Combine(HutaoRuntime.DataDirectory, "Metadata", cultureOptions.LocaleName);
                Directory.CreateDirectory(field);
            }

            return field;
        }
    }

    public string BundlePath
    {
        get => Path.Combine(LocalizedDirectory, BundleFileName);
    }
}
