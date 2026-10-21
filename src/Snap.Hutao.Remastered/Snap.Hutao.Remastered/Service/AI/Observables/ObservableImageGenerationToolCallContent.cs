// Copyright (c) 2026 DGP Studio. All rights reserved.
// Licensed under the MIT license.
// Adapted from Snap.Nicole.

using Microsoft.Extensions.AI;

namespace Snap.Hutao.Remastered.Service.AI.Observables;

internal sealed class ObservableImageGenerationToolCallContent : ObservableToolCallContent
{
    public static ObservableImageGenerationToolCallContent Create(ImageGenerationToolCallContent toolCallContent)
    {
        return new()
        {
            CallId = toolCallContent.CallId,
        };
    }
}
