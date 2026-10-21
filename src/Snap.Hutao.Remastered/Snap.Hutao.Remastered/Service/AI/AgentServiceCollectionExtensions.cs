// Copyright (c) Snap Hutao RP. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.ObjectPool;
using Snap.Hutao.Remastered.Core.AI.Text.Json;
using System.Text;

namespace Snap.Hutao.Remastered.Service.AI;

internal static class AgentServiceCollectionExtensions
{
    public static IServiceCollection AddAccountInsightsAgent(this IServiceCollection services)
    {
        // These configured infrastructure instances have no service attribute equivalent.
        // Agent services and the settings provider are registered by the service generator.
        services.AddJsonSerializerOptions();
        services.TryAddSingleton<ObjectPool<StringBuilder>>(new DefaultObjectPool<StringBuilder>(new StringBuilderPooledObjectPolicy()));
        return services;
    }
}
