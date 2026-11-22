// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.Web.Endpoint.Hutao;

public interface IInfrastructureMetadataEndpoints : IInfrastructureRootAccess
{
    string MetadataBundle(string locale)
    {
        return $"{Root}/static/metadata/GenshinMetadata.{locale}.hutaobin";
    }
}
