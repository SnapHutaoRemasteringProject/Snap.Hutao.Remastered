using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Snap.Hutao.Plugin.Contracts;
using Snap.Hutao.Plugin.Packaging;
using Snap.Hutao.Remastered.Core;
using Snap.Hutao.Remastered.Service.Notification;
using System.IO;

namespace Snap.Hutao.Remastered.Service.Plugin;

internal sealed record PluginNavigationTarget(string PluginId, string ViewId, string ItemId);
internal sealed record PluginNavigationItem(PluginNavigationTarget Target, string Title, string IconGlyph);

[Service(ServiceLifetime.Singleton)]
internal sealed partial class PluginService
{
    private readonly IMessenger messenger;
    private readonly ILogger<PluginService> logger;

    [GeneratedConstructor]
    public partial PluginService(IServiceProvider serviceProvider);

    private sealed record Workspace(PluginIdentity Identity, string Entry, string Hash);
    private sealed record ActiveSession(PluginSession Session, string PackageHash)
    {
        internal Action? Detach { get; set; }
    }

    private readonly Dictionary<string, ActiveSession> active = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Workspace> workspaces = new(StringComparer.OrdinalIgnoreCase);
    private readonly SemaphoreSlim operationGate = new(1, 1);
    private readonly string workspaceId = Environment.ProcessId + "-" + Guid.NewGuid().ToString("N");
    private WeakReference<XamlRoot>? root;
    private FileStream? instanceLock;
    private bool started;
    private bool shuttingDown;
    private bool recovery;
    internal bool RecoveryMode => recovery;
    internal event Action? Changed;

    private string CrashMarker => Path.Combine(PluginPackageStore.StateRoot, "running");
    private Version HostVersion => new(HutaoRuntime.Version.Major, HutaoRuntime.Version.Minor, Math.Max(0, HutaoRuntime.Version.Build));

    internal void AttachRoot(XamlRoot value) => root = new(value);
    private XamlRoot? GetRoot() => root is not null && root.TryGetTarget(out XamlRoot? value) ? value : null;
    private DispatcherQueue Queue => DispatcherQueue.GetForCurrentThread() ?? throw new InvalidOperationException("Plugin operation requires the UI thread.");

    private void EnsureStore()
    {
        if (instanceLock is not null) return;
        string directory = PluginPackageStore.StateRoot;
        PackagePaths.RejectReparseAncestors(directory);
        Directory.CreateDirectory(directory);
        string lockPath = Path.Combine(directory, "host.lock");
        PackagePaths.RejectReparseAncestors(lockPath);
        instanceLock = new FileStream(lockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        PackagePaths.RejectReparseAncestors(CrashMarker);
        recovery = File.Exists(CrashMarker);
    }

    internal bool IsActive(string id) => active.ContainsKey(id);
    internal Task<InstalledPlugin[]> ScanAsync() => Task.Run(PluginPackageStore.Scan);
    internal IReadOnlyList<PluginNavigationItem> Navigation => active.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
        .SelectMany(x => x.Value.Session.Navigation.OrderBy(n => n.Id, StringComparer.OrdinalIgnoreCase)
            .Select(n => new PluginNavigationItem(new(x.Key, n.ViewId, n.Id), n.Title, n.IconGlyph))).ToArray();

    internal IReadOnlyList<CommandContribution> Commands(string id) => GetActive(id).Session.Commands;
    internal IReadOnlyList<RegisteredPluginSetting> Settings(string id) => GetActive(id).Session.Settings;
    internal Task SetSettingAsync(string id, string name, object value) => GetActive(id).Session.SetSettingAsync(name, value);
    internal Task ResetSettingAsync(string id, string name) => GetActive(id).Session.ResetSettingAsync(name);
    internal Task ExecuteAsync(string id, string command) => GetActive(id).Session.ExecuteAsync(command);

    private ActiveSession GetActive(string id) => active.TryGetValue(id, out ActiveSession? value) ? value : throw new InvalidOperationException("Plugin is not active: " + id);

    internal UIElement OpenView(PluginNavigationTarget target, Action detach)
    {
        ActiveSession value = GetActive(target.PluginId);
        CloseView(target.PluginId);
        UIElement element = value.Session.OpenView(target.ViewId);
        value.Detach = detach;
        return element;
    }

    internal void CloseView(string id, Action? owner = null)
    {
        if (!active.TryGetValue(id, out ActiveSession? value) || (owner is not null && value.Detach != owner)) return;
        Action? detach = value.Detach;
        value.Detach = null;
        try { value.Session.CloseView(); }
        finally { detach?.Invoke(); }
    }

    internal async Task StartAsync()
    {
        _ = Queue;
        if (started || shuttingDown) return;
        started = true;
        try
        {
            EnsureStore();
            if (recovery || Environment.IsPrivilegedProcess) return;
            foreach (InstalledPlugin item in await ScanAsync())
            {
                if (!item.Enabled || item.Error is not null || item.Hash is null || item.Manifest?.UiMode == "compiled") continue;
                try
                {
                    PluginDiskState state = PluginPackageStore.ReadState(item.Id);
                    if (state.PendingHash is not null) continue; // Explicit review/commit after restart.
                    if (string.Equals(state.ApprovedHash, item.Hash, StringComparison.OrdinalIgnoreCase))
                        await EnableAsync(item.Id, item.Hash);
                }
                catch (Exception error) { Log(NotificationKind.Error, item.Id + ": " + error); }
            }
        }
        catch (Exception error) { Log(NotificationKind.Error, error.ToString()); }
        finally { Changed?.Invoke(); }
    }

    internal async Task InstallAsync(string source, VerifiedPackage verified)
    {
        await operationGate.WaitAsync();
        try
        {
            EnsureStore();
            string id = verified.Manifest.Id;
            if ((await ScanAsync()).Any(item => item.Id.Equals(id, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("Plugin already exists. Use Update or uninstall the legacy package first.");
            if (verified.Manifest.MinHostVersion > HostVersion) throw new InvalidDataException("Host version is too old.");
            await Task.Run(() => PluginPackageStore.CopyApproved(source, PluginPackageStore.PathFor(id, false), verified.Sha256));
        }
        finally { operationGate.Release(); Changed?.Invoke(); }
    }

    // The caller obtains the approved hash from the trust dialog; automatic startup
    // supplies the same durable approval. A changed archive is never silently trusted.
    internal async Task EnableAsync(string id, string approvedHash)
    {
        _ = Queue;
        await operationGate.WaitAsync();
        try
        {
            EnsureStore();
            if (shuttingDown) throw new InvalidOperationException("Application is closing.");
            if (IsActive(id)) return;
            InstalledPlugin item = await Task.Run(() => PluginPackageStore.Locate(id));
            if (item.Error is not null || item.Manifest is not { } manifest || item.Hash is not { } hash)
                throw new InvalidDataException(item.Error ?? "Incompatible plugin.");
            if (!hash.Equals(approvedHash, StringComparison.OrdinalIgnoreCase)) throw new UnauthorizedAccessException("Package has changed; approve the new SHA256.");
            if (manifest.MinHostVersion > HostVersion) throw new InvalidDataException("Host version is too old.");
            if (manifest.UiMode == "compiled") throw new NotSupportedException("Compiled XAML is experimental in PrototypeHost; production hosting is not enabled.");
            string canonicalId = item.Id;
            PluginDiskState state = PluginPackageStore.ReadState(canonicalId);
            if (state.PendingHash is not null) throw new InvalidOperationException("A staged update requires review after restart.");
            state = state with { ApprovedHash = hash, LastError = null };
            PluginPackageStore.WriteState(canonicalId, state);
            if (!workspaces.TryGetValue(canonicalId, out Workspace? workspace))
            {
                string directory = Path.Combine(PluginPackageStore.StateRoot, "workspaces", workspaceId, canonicalId);
                string settings = Path.Combine(PluginPackageStore.Root, canonicalId);
                await Task.Run(() =>
                {
                    PluginPackage.ExtractApproved(item.Path, hash, directory, HostVersion);
                    PackagePaths.RejectReparseAncestors(settings);
                    Directory.CreateDirectory(Path.Combine(settings, "data"));
                });
                PluginIdentity identity = new(manifest.Id, manifest.Name, manifest.Version.ToString(), directory, Path.Combine(settings, "data"),
                    settings, manifest.UiMode, manifest.Capabilities, manifest.Files.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase), manifest.MetadataProvider);
                workspace = new(identity, PackagePaths.Resolve(directory, manifest.Entry), hash);
                workspaces.Add(canonicalId, workspace);
            }
            else
            {
                if (!workspace.Hash.Equals(hash, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Another version is mapped; restart required.");
                await Task.Run(() => PluginPackageStore.VerifyWorkspace(workspace.Identity, manifest));
            }

            // An abrupt process exit leaves the marker and suppresses the next automatic activation.
            using (FileStream marker = new(CrashMarker, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read))
                marker.Flush(flushToDisk: true);
            void MarkInstalled()
            {
                state = state with { HookHash = hash };
                PluginPackageStore.WriteState(canonicalId, state);
            }
            void Commit()
            {
                if (!item.Enabled) File.Move(item.Path, PluginPackageStore.PathFor(canonicalId, true));
            }

            try
            {
                PluginSession session = await PluginSession.ActivateTrustedAsync(workspace.Entry, workspace.Identity,
                    !string.Equals(state.HookHash, hash, StringComparison.OrdinalIgnoreCase), MarkInstalled, Queue, GetRoot,
                    Log, Notify, hash, commitActivation: Commit);
                active.Add(canonicalId, new(session, hash));
            }
            catch (Exception error)
            {
                string enabled = PluginPackageStore.PathFor(canonicalId, true);
                string disabled = PluginPackageStore.PathFor(canonicalId, false);
                if (File.Exists(enabled) && !File.Exists(disabled)) File.Move(enabled, disabled);
                PluginPackageStore.WriteState(canonicalId, state with { LastError = error.ToString() });
                throw;
            }
        }
        finally { operationGate.Release(); Changed?.Invoke(); }
    }

    internal async Task DisableAsync(string id, bool uninstall = false)
    {
        _ = Queue;
        await operationGate.WaitAsync();
        try
        {
            EnsureStore();
            InstalledPlugin item = await Task.Run(() => PluginPackageStore.Locate(id));
            string disabled = PluginPackageStore.PathFor(item.Id, false);
            if (item.Enabled) File.Move(item.Path, disabled);
            if (active.TryGetValue(item.Id, out ActiveSession? value))
            {
                try { await value.Session.DisableAsync(() => { value.Detach?.Invoke(); value.Detach = null; }, uninstall); }
                finally { active.Remove(item.Id); }
            }
            if (uninstall)
            {
                File.Delete(disabled);
                // Keep data/settings. A disabled uninstall never runs third-party code.
                PluginDiskState state = PluginPackageStore.ReadState(item.Id);
                PluginPackageStore.WriteState(item.Id, state with { ApprovedHash = null, HookHash = null, LastError = null });
            }
        }
        finally { operationGate.Release(); Changed?.Invoke(); }
    }

    internal async Task StageUpdateAsync(string id, string source, VerifiedPackage verified)
    {
        await operationGate.WaitAsync();
        try
        {
            EnsureStore();
            InstalledPlugin item = await Task.Run(() => PluginPackageStore.Locate(id));
            if (item.Manifest is null) throw new InvalidOperationException("Legacy package: uninstall without deleting settings, then install its SDK 2.0 replacement.");
            if (!verified.Manifest.Id.Equals(item.Id, StringComparison.OrdinalIgnoreCase) || verified.Manifest.Version <= item.Manifest.Version)
                throw new InvalidDataException("Update must have the same ID and a higher version.");
            if (verified.Manifest.MinHostVersion > HostVersion || verified.Manifest.UiMode == "compiled") throw new InvalidDataException("Update is incompatible with this host.");
            PluginDiskState state = PluginPackageStore.ReadState(item.Id);
            if (state.PendingHash is not null || state.BackupHash is not null) throw new InvalidOperationException("Resolve the existing pending update or rollback first.");
            string pending = Path.Combine(PluginPackageStore.StateDirectory(item.Id), "pending.hutao");
            await Task.Run(() => PluginPackageStore.CopyApproved(source, pending, verified.Sha256));
            PluginPackageStore.WriteState(item.Id, state with { PendingHash = verified.Sha256 });
        }
        finally { operationGate.Release(); Changed?.Invoke(); }
    }

    // Explicitly apply only in a clean process; keep the previous archive for rollback.
    internal async Task ApplyUpdateAsync(string id)
    {
        await operationGate.WaitAsync();
        try
        {
            EnsureStore();
            if (active.ContainsKey(id) || workspaces.ContainsKey(id)) throw new InvalidOperationException("Restart before applying an update.");
            InstalledPlugin item = await Task.Run(() => PluginPackageStore.Locate(id));
            PluginDiskState state = PluginPackageStore.ReadState(item.Id);
            if (state.PendingHash is not { } pendingHash || item.Hash is not { } oldHash) throw new InvalidOperationException("No verified pending update.");
            string directory = PluginPackageStore.StateDirectory(item.Id);
            string pending = Path.Combine(directory, "pending.hutao"), backup = Path.Combine(directory, "backup.hutao");
            // Verify both files before any replacement and keep durable backup metadata.
            await Task.Run(() => PluginPackageStore.CopyApproved(item.Path, backup, oldHash));
            PluginPackageStore.WriteState(item.Id, state with { BackupHash = oldHash });
            string disabled = PluginPackageStore.PathFor(item.Id, false);
            if (item.Enabled) File.Move(item.Path, disabled);
            await Task.Run(() => PluginPackageStore.CopyApproved(pending, disabled, pendingHash, overwrite: true));
            PluginPackageStore.WriteState(item.Id, state with { ApprovedHash = pendingHash, PendingHash = null, BackupHash = oldHash });
            File.Delete(pending);
        }
        finally { operationGate.Release(); Changed?.Invoke(); }
    }

    internal async Task RollbackAsync(string id)
    {
        await operationGate.WaitAsync();
        try
        {
            EnsureStore();
            if (active.ContainsKey(id) || workspaces.ContainsKey(id)) throw new InvalidOperationException("Restart before rollback.");
            InstalledPlugin item = await Task.Run(() => PluginPackageStore.Locate(id));
            PluginDiskState state = PluginPackageStore.ReadState(item.Id);
            if (state.BackupHash is not { } hash) throw new InvalidOperationException("No verified backup.");
            string directory = PluginPackageStore.StateDirectory(item.Id);
            string backup = Path.Combine(directory, "backup.hutao");
            string disabled = PluginPackageStore.PathFor(item.Id, false);
            if (item.Enabled) File.Move(item.Path, disabled);
            await Task.Run(() => PluginPackageStore.CopyApproved(backup, disabled, hash, overwrite: true));
            PluginPackageStore.WriteState(item.Id, state with { ApprovedHash = null, BackupHash = null, PendingHash = null, LastError = null });
            File.Delete(backup);
            File.Delete(Path.Combine(directory, "pending.hutao"));
        }
        finally { operationGate.Release(); Changed?.Invoke(); }
    }

    internal async Task ShutdownAsync()
    {
        shuttingDown = true;
        await operationGate.WaitAsync();
        bool clean = true;
        try
        {
            foreach (var item in active.Reverse().ToArray())
            {
                try { await item.Value.Session.DisableAsync(() => item.Value.Detach?.Invoke()); }
                catch (Exception error) { clean = false; Log(NotificationKind.Error, error.ToString()); }
            }
            active.Clear();
            if (instanceLock is not null && clean) File.Delete(CrashMarker);
        }
        finally
        {
            instanceLock?.Dispose();
            instanceLock = null;
            operationGate.Release();
        }
    }

    private void Log(NotificationKind kind, string message)
    {
        if (kind == NotificationKind.Error) logger.LogError("{PluginMessage}", message);
        else if (kind == NotificationKind.Warning) logger.LogWarning("{PluginMessage}", message);
        else logger.LogInformation("{PluginMessage}", message);
    }

    private void Notify(NotificationKind kind, string message)
    {
        InfoBarSeverity severity = kind switch
        {
            NotificationKind.Success => InfoBarSeverity.Success,
            NotificationKind.Warning => InfoBarSeverity.Warning,
            NotificationKind.Error => InfoBarSeverity.Error,
            _ => InfoBarSeverity.Informational,
        };
        messenger.Send(InfoBarMessage.Any(severity, message));
    }
}
