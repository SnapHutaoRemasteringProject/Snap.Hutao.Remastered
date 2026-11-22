// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core.Property;
using Snap.Hutao.Remastered.Core.Setting;
using Snap.Hutao.Remastered.Service.Abstraction;

namespace Snap.Hutao.Remastered.Service.Metadata;

/// <summary>
/// Persisted metadata source preference. <see cref="MetadataService"/> watches the property and
/// switches sources; this type only owns the stored value.
/// </summary>
[Service(ServiceLifetime.Singleton)]
public sealed partial class MetadataSourceOptions : DbStoreOptions
{
    [GeneratedConstructor(CallBaseConstructor = true)]
    public partial MetadataSourceOptions(IServiceProvider serviceProvider);

    [field: MaybeNull]
    public IObservableProperty<MetadataSourceKind> Source
    {
        get => field ??= CreateProperty(SettingKeys.MetadataSource, MetadataSourceKind.Auto);
    }
}
