// Copyright (c) 2026 DGP Studio. All rights reserved.
// Licensed under the MIT license.
// Adapted from Snap.Nicole.

using CommunityToolkit.Mvvm.ComponentModel;
using Snap.Hutao.Remastered.Core.AI;
using Snap.Hutao.Remastered.Service.AI.Settings;
using System.Collections.Generic;
using System.ComponentModel;

namespace Snap.Hutao.Remastered.Service.AI.Models;

internal sealed partial class ModelProviderProfile : ObservableObject, IIdentifiable<Guid>, ICopyFrom<ModelProviderProfile>, IObservableChildrenProvider
{
    [ObservableProperty]
    public partial Guid Id { get; set; } = Guid.NewGuid();

    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

    [ObservableProperty]
    public partial EnumBox<ModelProviderType> ProviderType { get; set; } = EnumBox.Of(ModelProviderType.OpenAIChatCompletion);

    [ObservableProperty]
    public partial string Endpoint { get; set; } = string.Empty;

    [ObservableProperty]
    [JsonIgnore]
    public partial string? ApiKey { get; set; }

    [ObservableProperty]
    public partial string? ModelListDocumentationLink { get; set; }

    public ObservableSettingsCollection<ModelProfile, Guid> ModelProfiles { get; set => SetProperty(ref field, value ?? []); } = [];

    public Guid? SelectedModelProfileId
    {
        get => ModelProfiles.CurrentItemId;
        set => ModelProfiles.CurrentItemId = value;
    }

    public IEnumerable<INotifyPropertyChanged> EnumerateObservableChildren()
    {
        return ModelProfiles.EnumerateObservableChildren();
    }
}
