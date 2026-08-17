// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core.LifeCycle.InterProcess.BetterGenshinImpact.Handler;

namespace Snap.Hutao.Remastered.Core.LifeCycle.InterProcess.BetterGenshinImpact;

[Service(ServiceLifetime.Singleton)]
public sealed partial class BetterGenshinImpactNamedPipeServer
{
    private readonly IEnumerable<IPipeRequestHandler> handlers;

    [GeneratedConstructor]
    public partial BetterGenshinImpactNamedPipeServer(IServiceProvider serviceProvider);

    public async ValueTask<PipeResponse> DispatchRequest(PipeRequest<JsonElement>? request)
    {
        if (request is not null)
        {
            foreach (IPipeRequestHandler handler in handlers)
            {
                if (handler.CanHandle(request.Kind))
                {
                    return await handler.HandleRequest(request).ConfigureAwait(false);
                }
            }
        }

        return PipeResponse.CreateNone();
    }
}
