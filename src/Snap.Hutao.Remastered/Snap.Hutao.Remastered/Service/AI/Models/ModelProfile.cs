// Copyright (c) 2026 DGP Studio. All rights reserved.
// Licensed under the MIT license.
// Adapted from Snap.Nicole.

using CommunityToolkit.Mvvm.ComponentModel;
using Snap.Hutao.Remastered.Core.AI;
using Snap.Hutao.Remastered.Service.AI.Settings;

namespace Snap.Hutao.Remastered.Service.AI.Models;

[WinRT.GeneratedBindableCustomProperty]
internal sealed partial class ModelProfile : ObservableObject, IIdentifiable<Guid>, ICopyFrom<ModelProfile>, IObservableChildrenProvider
{
    [ObservableProperty]
    public partial Guid Id { get; set; } = Guid.NewGuid();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayName))]
    public partial string Name { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayName))]
    public partial string ModelId { get; set; } = string.Empty;

    [JsonInclude]
    public ModelProfileAgentOptions AgentOptions { get; internal set => SetProperty(ref field, value ?? new()); } = new();

    [JsonIgnore]
    public string DisplayName { get => !string.IsNullOrWhiteSpace(Name) ? Name : ModelId; }

    public static ModelProfile Create(OpenAI.Models.OpenAIModel model)
    {
        return new()
        {
            Name = model.Id,
            ModelId = model.Id,
        };
    }

    public static ModelProfile Create(Anthropic.Models.Models.ModelInfo model)
    {
        return new()
        {
            Name = string.IsNullOrWhiteSpace(model.DisplayName) ? model.ID : model.DisplayName,
            ModelId = model.ID,
        };
    }

    public IEnumerable<INotifyPropertyChanged> EnumerateObservableChildren()
    {
        yield return AgentOptions;
    }
}
