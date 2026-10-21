// Copyright (c) Snap Hutao RP. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel;
using Snap.Hutao.Remastered.Core.AI;
using Snap.Hutao.Remastered.Service.AI.Models;

namespace Snap.Hutao.Remastered.Service.AI.Settings;

internal sealed partial class AgentSettings : ObservableObject, ICopyFrom<AgentSettings>, IObservableChildrenProvider
{
    public AppAgentOptions AgentOptions { get; set => SetProperty(ref field, value ?? new()); } = new();

    public ObservableSettingsCollection<ModelProviderProfile, Guid> ModelProviderProfiles { get; set => SetProperty(ref field, value ?? []); } = [];

    public Guid? SelectedModelProviderProfileId { get => ModelProviderProfiles.CurrentItemId; set => ModelProviderProfiles.CurrentItemId = value; }

    public IEnumerable<INotifyPropertyChanged> EnumerateObservableChildren()
    {
        yield return AgentOptions;
        foreach (INotifyPropertyChanged child in ModelProviderProfiles.EnumerateObservableChildren())
        {
            yield return child;
        }
    }

    public void CopyFrom(AgentSettings source)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (ReferenceEquals(this, source)) { return; }
        AgentOptions.CopyFrom(source.AgentOptions);
        ModelProviderProfiles.CopyFrom(source.ModelProviderProfiles);
        SelectedModelProviderProfileId = source.SelectedModelProviderProfileId;
    }
}
