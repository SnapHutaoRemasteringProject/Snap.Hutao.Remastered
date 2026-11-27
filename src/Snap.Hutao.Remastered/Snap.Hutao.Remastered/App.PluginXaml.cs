// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Markup;
using Snap.Hutao.Remastered.Service.Plugin;
using System.Collections.Concurrent;

namespace Snap.Hutao.Remastered;

public sealed partial class App
{
    // Keep the XAML compiler's automatic discovery of all referenced control libraries.
    // The generated provider type is available during both XAML compiler passes.
    private readonly IXamlMetadataProvider hostMetadata = new global::Snap.Hutao.Remastered.Snap_Hutao_Remastered_XamlTypeInfo.XamlMetaDataProvider();
    private readonly ConcurrentDictionary<string, (string Path, IXamlMetadataProvider Provider)> pluginMetadata = new(StringComparer.Ordinal);
    private PluginPriResources? pluginResources;

    private void OnPluginResourceManagerRequested(Application sender, ResourceManagerRequestedEventArgs args)
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

    // WinUI queries IXamlMetadataProvider, not the public forwarding methods generated
    // for App in pass 2. Explicit implementation preserves those methods unchanged.
    IXamlType IXamlMetadataProvider.GetXamlType(Type type)
    {
        IXamlType? result = hostMetadata.GetXamlType(type);
        if (result?.IsConstructible == true) return result;
        foreach (var entry in pluginMetadata.Values)
        {
            IXamlType? candidate = entry.Provider.GetXamlType(type);
            if (candidate?.IsConstructible == true) return candidate;
            result ??= candidate;
        }
        return result!;
    }

    IXamlType IXamlMetadataProvider.GetXamlType(string fullName)
    {
        IXamlType? result = hostMetadata.GetXamlType(fullName);
        if (result?.IsConstructible == true) return result;
        foreach (var entry in pluginMetadata.Values)
        {
            IXamlType? candidate = entry.Provider.GetXamlType(fullName);
            if (candidate?.IsConstructible == true) return candidate;
            result ??= candidate;
        }
        return result!;
    }

    XmlnsDefinition[] IXamlMetadataProvider.GetXmlnsDefinitions()
    {
        return [.. hostMetadata.GetXmlnsDefinitions(), .. pluginMetadata.Values.SelectMany(static entry => entry.Provider.GetXmlnsDefinitions())];
    }
}
