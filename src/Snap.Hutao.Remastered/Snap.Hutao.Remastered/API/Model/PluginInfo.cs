using CommunityToolkit.Mvvm.ComponentModel;

namespace Snap.Hutao.Remastered.API.Model;

/// <summary>
/// Describes an installed plugin independently of its assembly. A disabled plugin has its assemblies
/// released, so it exists as this description only and <see cref="Instance"/> stays <see langword="null"/>
/// until the plugin is enabled again.
/// </summary>
public sealed partial class PluginInfo : ObservableObject
{
    [ObservableProperty]
    public partial bool IsEnabled { get; set; }

    [ObservableProperty]
    public partial string IconPath { get; set; } = string.Empty;

    public required string Id { get; init; }

    public required PluginManifest Manifest { get; set; }

    /// <summary>
    /// The live plugin object, or <see langword="null"/> while the plugin is disabled and unloaded.
    /// </summary>
    public HutaoPlugin? Instance { get; set; }
}
