// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core.ExceptionService;
using System.Collections.Immutable;

namespace Snap.Hutao.Remastered.Service.Metadata.Provider;

[Service(ServiceLifetime.Singleton, typeof(IMetadataProviderFactory))]
public sealed partial class MetadataProviderFactory : IMetadataProviderFactory
{
    private readonly IServiceProvider serviceProvider;

    [GeneratedConstructor]
    public partial MetadataProviderFactory(IServiceProvider serviceProvider);

    // Phase 1 listed only Git; the proto provider is registered in the same assembler now.
    public ImmutableArray<MetadataSourceKind> AvailableKinds { get => [MetadataSourceKind.Git, MetadataSourceKind.Proto]; }

    public IMetadataProvider GetOrCreate(MetadataSourceKind kind)
    {
        return kind switch
        {
            MetadataSourceKind.Git or MetadataSourceKind.Proto => serviceProvider.GetRequiredKeyedService<IMetadataProvider>(kind),
            _ => throw HutaoException.Argument("Auto is not a provider kind", nameof(kind)),
        };
    }
}
