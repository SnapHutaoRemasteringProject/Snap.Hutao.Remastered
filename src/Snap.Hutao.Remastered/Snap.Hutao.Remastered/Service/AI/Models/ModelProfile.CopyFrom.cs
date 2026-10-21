// Copyright (c) Snap Hutao RP. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.Service.AI.Models;

internal sealed partial class ModelProfile
{
    public void CopyFrom(ModelProfile source)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (ReferenceEquals(this, source)) { return; }
        Id = source.Id;
        Name = source.Name;
        ModelId = source.ModelId;
        AgentOptions.CopyFrom(source.AgentOptions);
    }
}
