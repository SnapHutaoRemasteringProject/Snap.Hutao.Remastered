// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.Core.LifeCycle.InterProcess.BetterGenshinImpact.Handler;

public interface IPipeRequestHandler
{
    bool CanHandle(PipeRequestKind kind);

    ValueTask<PipeResponse> HandleRequest(PipeRequest<JsonElement> request);
}
