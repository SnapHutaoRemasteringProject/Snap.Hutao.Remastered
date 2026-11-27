// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Markup;
using Snap.Hutao.Remastered.Service.Plugin;
using System.Collections.Concurrent;

namespace Snap.Hutao.Remastered;

public sealed partial class App
{
    private readonly ConcurrentDictionary<string, (string Path, IXamlMetadataProvider Provider)> pluginMetadata = new(StringComparer.Ordinal);
    private PluginPriResources? pluginResources;

    private void OnPluginResourceManagerRequested(object? sender, ResourceManagerRequestedEventArgs args)
    {
        pluginResources ??= new PluginPriResources();
        args.CustomResourceManager = pluginResources.Manager;
    }

    internal void RegisterPluginMetadataProvider(string path, string name, IXamlMetadataProvider provider)
    {
        if (pluginMetadata.TryGetValue(name, out var existing))
        {
            if (!string.Equals(existing.Path, path, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("XAML metadata name collision: " + name);
            return;
        }

        (pluginResources ?? throw new InvalidOperationException("WinUI did not request its MRT resource manager.")).Register(path);
        if (!pluginMetadata.TryAdd(name, (path, provider)))
            throw new InvalidOperationException("XAML metadata already registered: " + name);
    }
}
