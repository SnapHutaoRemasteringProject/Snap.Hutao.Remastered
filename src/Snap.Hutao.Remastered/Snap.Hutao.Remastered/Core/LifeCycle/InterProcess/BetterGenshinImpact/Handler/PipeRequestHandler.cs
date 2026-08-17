// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.Core.LifeCycle.InterProcess.BetterGenshinImpact.Handler;

public abstract class PipeRequestHandler<TRequest> : IPipeRequestHandler
{
    private readonly PipeRequestKind kind;

    protected PipeRequestHandler(PipeRequestKind kind)
    {
        this.kind = kind;
    }

    public bool CanHandle(PipeRequestKind kind) => kind == this.kind;

    public async ValueTask<PipeResponse> HandleRequest(PipeRequest<JsonElement> request)
    {
        if (request.Data.Deserialize<TRequest>() is { } data)
        {
            return await HandleAsync(data).ConfigureAwait(false);
        }

        return PipeResponse.CreateNone();
    }

    protected abstract ValueTask<PipeResponse> HandleAsync(TRequest data);
}
