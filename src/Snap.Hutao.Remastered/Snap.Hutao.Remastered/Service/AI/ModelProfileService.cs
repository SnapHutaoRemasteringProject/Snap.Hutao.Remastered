// Copyright (c) 2026 DGP Studio. All rights reserved.
// Licensed under the MIT license.
// Adapted from Snap.Nicole.

using Anthropic;
using Anthropic.Core;
using Anthropic.Models.Models;
using OpenAI;
using OpenAI.Models;
using Sentry;
using Snap.Hutao.Remastered.Core.AI;
using Snap.Hutao.Remastered.Core.AI.Diagnostics;
using Snap.Hutao.Remastered.Service.AI.Models;
using System.ClientModel;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Snap.Hutao.Remastered.Service.AI;

[Service(ServiceLifetime.Scoped, typeof(IModelProfileService))]
internal sealed class ModelProfileService : IModelProfileService
{
    public Task<IReadOnlyList<ModelProfile>> GetModelsAsync(ModelProviderProfile providerProfile, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(providerProfile);
        return GetModelsCoreAsync(providerProfile, cancellationToken);
    }

    private static async Task<IReadOnlyList<ModelProfile>> GetModelsCoreAsync(ModelProviderProfile providerProfile, CancellationToken cancellationToken)
    {
        using SentryDiagnosticSpan span = SentryDiagnostics.StartSpan(SentryOperations.SettingsModelProfilesFetch, "Fetch model list");
        span.SetTag(SentryTags.AIProvider, providerProfile.ProviderType.Value.ToString());

        try
        {
            IReadOnlyList<ModelProfile> result = providerProfile.ProviderType.Value switch
            {
                ModelProviderType.OpenAIChatCompletion or ModelProviderType.OpenAIResponses => await GetOpenAIModelsAsync(providerProfile, cancellationToken),
                ModelProviderType.Anthropic => await GetAnthropicModelsAsync(providerProfile, cancellationToken),
                _ => throw new NotSupportedException($"Unsupported model provider type: {providerProfile.ProviderType.Value}"),
            };

            span.SetData(SentryData.AIModelCount, result.Count);
            return result;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            span.Finish(SpanStatus.Cancelled);
            throw;
        }
        catch (Exception ex)
        {
            span.Finish(ex);
            throw;
        }
    }

    private static async Task<IReadOnlyList<ModelProfile>> GetOpenAIModelsAsync(ModelProviderProfile providerProfile, CancellationToken cancellationToken)
    {
        ApiKeyCredential apiKeyCredential = new(providerProfile.ApiKey!);
        OpenAIClientOptions options = new()
        {
            Endpoint = AgentEndpoint.Validate(providerProfile.Endpoint),
        };

        OpenAIModelClient client = new OpenAIClient(apiKeyCredential, options).GetOpenAIModelClient();
        ClientResult<OpenAIModelCollection> result = await client.GetModelsAsync(cancellationToken);

        IEnumerable<ModelProfile> models = result.Value
            .Where(static model => !string.IsNullOrWhiteSpace(model.Id))
            .Select(ModelProfile.Create);
        return [.. models];
    }

    private static async Task<IReadOnlyList<ModelProfile>> GetAnthropicModelsAsync(ModelProviderProfile providerProfile, CancellationToken cancellationToken)
    {
        ClientOptions clientOptions = new()
        {
            ApiKey = providerProfile.ApiKey,
            BaseUrl = AgentEndpoint.Validate(providerProfile.Endpoint)?.AbsoluteUri ?? EnvironmentUrl.Production,
        };

        using (AnthropicClient client = new(clientOptions))
        {
            List<ModelProfile> result = [];
            ModelListParams @params = new()
            {
                Limit = 1000,
            };

            ModelListPage page = await client.Models.List(@params, cancellationToken);
            await foreach(ModelInfo model in page.Paginate(cancellationToken))
            {
                if (string.IsNullOrWhiteSpace(model.ID))
                {
                    continue;
                }

                result.Add(ModelProfile.Create(model));
            }

            return result;
        }
    }

}
