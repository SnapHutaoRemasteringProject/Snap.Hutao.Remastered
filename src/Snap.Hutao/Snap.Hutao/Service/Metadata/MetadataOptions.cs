// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Core;
using System.IO;

namespace Snap.Hutao.Service.Metadata;

[Service(ServiceLifetime.Singleton)]
internal sealed partial class MetadataOptions
{
    private readonly CultureOptions cultureOptions;

    [GeneratedConstructor]
    public partial MetadataOptions(IServiceProvider serviceProvider);

    [field: MaybeNull]
    public string LocalizedDataFolder
    {
        get
        {
            if (field is null)
            {
                field = Path.Combine(HutaoRuntime.GetDataRepositoryDirectory(), "Snap.Metadata", "Genshin", cultureOptions.LocaleName);
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