// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.Core.LifeCycle.InterProcess.BetterGenshinImpact.Handler;

[Service(ServiceLifetime.Singleton, typeof(IPipeRequestHandler))]
public sealed class LogHandler : IPipeRequestHandler
{
    private readonly ILogger<BetterGenshinImpactNamedPipeServer> logger;

    public LogHandler(ILogger<BetterGenshinImpactNamedPipeServer> logger)
    {
        this.logger = logger;
    }

    public bool CanHandle(PipeRequestKind kind) => kind == PipeRequestKind.Log;

    public ValueTask<PipeResponse> HandleRequest(PipeRequest<JsonElement> request)
    {
        logger.LogInformation("BGI: {log}", request.Data.GetString());
        return ValueTask.FromResult<PipeResponse>(PipeResponse.CreateNone());
    }
}
