using System.IO;
namespace Snap.Hutao.Remastered.Service.Plugin;

// A mapped native library can never be replaced or unloaded in this process.
// Reserve before LoadLibrary, reject concurrent sessions and quarantine failed sessions.
internal static class PluginResidency
{
    private sealed class Slot(string path, string version, string hash)
    {
        internal string Path { get; } = path;
        internal string Version { get; } = version;
        internal string Hash { get; } = hash;
        internal bool Active { get; set; }
        internal bool Quarantined { get; set; }
    }

    private static readonly object Gate = new();
    private static readonly Dictionary<string, Slot> Slots = new(StringComparer.OrdinalIgnoreCase);

    internal static void Acquire(string id, string path, string version, string hash)
    {
        path = System.IO.Path.GetFullPath(path);
        lock (Gate)
        {
            if (!Slots.TryGetValue(id, out Slot? slot))
            {
                slot = new(path, version, hash);
                Slots.Add(id, slot);
            }
            if (slot.Active || slot.Quarantined || !string.Equals(slot.Path, path, StringComparison.OrdinalIgnoreCase) ||
                slot.Version != version || !string.Equals(slot.Hash, hash, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Plugin ID is already active, quarantined or mapped from another build. Restart required: " + id);
            slot.Active = true;
        }
    }

    internal static void Release(string id, bool quarantine, bool removeUnmapped = false)
    {
        lock (Gate)
        {
            if (removeUnmapped && !quarantine)
            {
                Slots.Remove(id);
            }
            else if (Slots.TryGetValue(id, out Slot? slot))
            {
                slot.Active = false;
                slot.Quarantined |= quarantine;
            }
        }
    }
}
