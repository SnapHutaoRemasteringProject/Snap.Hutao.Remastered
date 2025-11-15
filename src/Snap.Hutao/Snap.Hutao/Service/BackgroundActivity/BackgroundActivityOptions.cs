// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;

namespace Snap.Hutao.Service.BackgroundActivity;

[Service(ServiceLifetime.Singleton)]
internal sealed partial class BackgroundActivityOptions : ObservableObject
{
    [GeneratedConstructor]
    public partial BackgroundActivityOptions(IServiceProvider serviceProvider);

    public BackgroundActivity Default { get; } = new(string.Empty, string.Empty);

    public BackgroundActivity MetadataInitialization { get; } = new(SH.ServiceBackgroundActivityMetadataInitialization, SH.ServiceBackgroundActivityMetadataInitializationDescription);

    public BackgroundActivity FullTrustInitialization { get; } = new(SH.ServiceBackgroundActivityFullTrustInitialization, SH.ServiceBackgroundActivityFullTrustInitializationDescription);
}