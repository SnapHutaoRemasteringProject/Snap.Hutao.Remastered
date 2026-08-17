// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.Core.LifeCycle.InterProcess.BetterGenshinImpact.Handler;

[Service(ServiceLifetime.Singleton, typeof(IPipeRequestHandler))]
public sealed class BeginSwitchToNextGameAccountHandler : IPipeRequestHandler
{
    public bool CanHandle(PipeRequestKind kind) => kind == PipeRequestKind.BeginSwitchToNextGameAccount;

    public ValueTask<PipeResponse> HandleRequest(PipeRequest<JsonElement> request)
    {
        // TODO: Implement
        return ValueTask.FromResult<PipeResponse>(PipeResponse.CreateNone());
    }
}
