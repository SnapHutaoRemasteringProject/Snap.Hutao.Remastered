// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core;
using System.IO;

namespace Snap.Hutao.Remastered.Service.Metadata.Provider.Git;

/// <summary>
/// Local layout of the git/JSON metadata source.
/// </summary>
[Service(ServiceLifetime.Singleton)]
public sealed partial class GitMetadataProviderOptions
{
    private readonly CultureOptions cultureOptions;

    [GeneratedConstructor]
    public partial GitMetadataProviderOptions(IServiceProvider serviceProvider);

    public string RepositoryName { get; } = "Snap.Metadata";

    [field: MaybeNull]
    public string LocalizedDataFolder
    {
        get
        {
            if (field is null)
            {
                field = Path.Combine(HutaoRuntime.GetDataRepositoryDirectory(), RepositoryName, "Genshin", cultureOptions.LocaleName);
                Directory.CreateDirectory(field);
            }

            return field;
        }
    }

    public string GetLocalizedLocalPath(string fileNameWithExtension)
    {
        return Path.Combine(LocalizedDataFolder, fileNameWithExtension);
    }
}
