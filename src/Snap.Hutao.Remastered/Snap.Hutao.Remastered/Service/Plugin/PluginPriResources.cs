using Microsoft.Windows.ApplicationModel.Resources;
using System;
using System.Collections.Concurrent;
using System.IO;
using Snap.Hutao.Plugin.Packaging;

namespace Snap.Hutao.Remastered.Service.Plugin;

// Use the real MRT Core manager, not a replacement IResourceMap implementation.
// Only a missing resource in a registered component namespace is routed to its PRI.
internal sealed class PluginPriResources
{
    private sealed record Component(string Path, ResourceManager Manager);
    private readonly ConcurrentDictionary<string, Component> components = new(StringComparer.OrdinalIgnoreCase);
    private readonly ResourceManager host = new();

    internal PluginPriResources()
    {
        host.ResourceNotFound += ResolveMissingResource;
    }

    internal ResourceManager Manager => host;

    internal string? LastResolvedResource { get; private set; }

    internal void Register(string modulePath)
    {
        string name = Path.GetFileNameWithoutExtension(modulePath);
        string priPath = Path.ChangeExtension(modulePath, ".pri");
        if (components.TryGetValue(name, out Component? existing))
        {
            if (!string.Equals(existing.Path, priPath, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("PRI component name collision: " + name);
            return;
        }

        PackagePaths.RejectReparseAncestors(priPath);
        if (!File.Exists(priPath)) throw new FileNotFoundException("Compiled plugin PRI missing.", priPath);
        ResourceManager manager = new(priPath);
        if (manager.MainResourceMap.ResourceCount == 0)
            throw new InvalidDataException("Compiled plugin PRI has no resources: " + priPath);
        if (!components.TryAdd(name, new(priPath, manager)))
            throw new InvalidOperationException("PRI component already registered: " + name);
        System.Diagnostics.Debug.WriteLine("[PluginPRI] Registered " + name + ": " + priPath);
    }

    private void ResolveMissingResource(ResourceManager sender, ResourceNotFoundEventArgs args)
    {
        // WinUI uses Files/<component>/<path>, optionally as an ms-resource URI.
        string key = args.Name;
        if (key.StartsWith("ms-resource://", StringComparison.OrdinalIgnoreCase))
        {
            int slash = key.IndexOf('/', "ms-resource://".Length);
            if (slash < 0) return;
            key = key[(slash + 1)..];
        }

        const string files = "Files/";
        if (!key.StartsWith(files, StringComparison.OrdinalIgnoreCase)) return;
        int separator = key.IndexOf('/', files.Length);
        if (separator < 0) return;
        string componentName = key[files.Length..separator];
        if (!components.TryGetValue(componentName, out Component? component)) return;

        // MRT contexts belong to their creating manager. Copy qualifier values rather
        // than passing the host's native context handle to a different PRI manager.
        ResourceContext context = component.Manager.CreateResourceContext();
        foreach (var qualifier in args.Context.QualifierValues)
        {
            if (!string.IsNullOrEmpty(qualifier.Value) && context.QualifierValues.ContainsKey(qualifier.Key))
                context.QualifierValues[qualifier.Key] = qualifier.Value;
        }

        ResourceMap map = component.Manager.MainResourceMap;
        ResourceCandidate? candidate = map.TryGetValue(key, context);
        // A standalone component PRI may index paths relative to its own root.
        candidate ??= map.TryGetValue(files + key[(separator + 1)..], context);
        if (candidate is null) return;

        // This experiment serves embedded files only. Never resolve arbitrary filesystem
        // paths from the PRI or silently reintroduce a physical XBF fallback.
        if (candidate.Kind != ResourceCandidateKind.EmbeddedData)
        {
            System.Diagnostics.Debug.WriteLine("[PluginPRI] Not embedded: " + key + " (" + candidate.Kind + ")");
            return;
        }

        args.SetResolvedCandidate(candidate);
        LastResolvedResource = key;
        System.Diagnostics.Debug.WriteLine("[PluginPRI] EmbeddedData: " + key);
    }
}
