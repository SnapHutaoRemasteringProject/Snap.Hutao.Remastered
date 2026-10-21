// Copyright (c) Snap Hutao RP. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.Service.AI.Models;

internal sealed partial class ModelProviderProfile
{
    public void CopyFrom(ModelProviderProfile source)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (ReferenceEquals(this, source)) { return; }
        Id = source.Id;
        Name = source.Name;
        ProviderType = source.ProviderType;
        Endpoint = source.Endpoint;
        ApiKey = source.ApiKey;
        ModelListDocumentationLink = source.ModelListDocumentationLink;
        ModelProfiles.CopyFrom(source.ModelProfiles);
        SelectedModelProfileId = source.SelectedModelProfileId;
    }
}
