// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core.LifeCycle.InterProcess.BetterGenshinImpact.Cultivation;

namespace Snap.Hutao.Remastered.Core.LifeCycle.InterProcess.BetterGenshinImpact.Handler;

[Service(ServiceLifetime.Singleton, typeof(IPipeRequestHandler))]
public sealed class QueryCurrentCultivationProjectHandler : IPipeRequestHandler
{
    private readonly IAutomationCultivationService automationCultivationService;

    public QueryCurrentCultivationProjectHandler(IAutomationCultivationService automationCultivationService)
    {
        this.automationCultivationService = automationCultivationService;
    }

    public bool CanHandle(PipeRequestKind kind) => kind == PipeRequestKind.QueryCurrentCultivationProject;

    public async ValueTask<PipeResponse> HandleRequest(PipeRequest<JsonElement> request)
    {
        return new PipeResponse<AutomationCultivationProject>
        {
            Kind = PipeResponseKind.Object,
            Data = await automationCultivationService.GetCurrentProjectAsync().ConfigureAwait(false),
        };
    }
}
