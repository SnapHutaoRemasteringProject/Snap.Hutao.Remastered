using System.IO;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Snap.Hutao.Plugin.Contracts;
using Snap.Hutao.Plugin.Packaging;
using Windows.Foundation;
using System.Security.Cryptography;

namespace Snap.Hutao.Remastered.Service.Plugin;

// UI-thread confined owner of one activated instance. Module residency is process-wide.
// Package approval/transaction and install-marker persistence belong to the caller.
public sealed class PluginSession
{
    private readonly DispatcherQueue queue;
    private readonly PluginHostContext context;
    private readonly string id;
    private IHutaoPlugin? plugin;
    private IPluginView? view;
    private IAsyncAction? operation;
    private Task? operationTask;
    private bool enableEntered;
    private bool closing;

    private PluginSession(DispatcherQueue queue, PluginHostContext context, string id)
    {
        this.queue = queue;
        this.context = context;
        this.id = id;
    }

    public IReadOnlyList<NavigationContribution> Navigation => context.Navigation;
    public bool IsClosing => closing;
    internal IReadOnlyList<CommandContribution> Commands => context.Commands;
    internal IReadOnlyList<RegisteredPluginSetting> Settings => context.SettingsSnapshot;
    internal Task SetSettingAsync(string name, object value) => context.SetSettingAsync(name, value);
    internal Task ResetSettingAsync(string name) => context.ResetAsync(name).AsTask();

    // Explicitly approved native DLL, never call this while enumerating packages.
    public static async Task<PluginSession> ActivateTrustedAsync(string dllPath, PluginIdentity identity, bool runInstallHook,
        Action markInstallCompleted, DispatcherQueue queue, Func<XamlRoot?> xamlRoot,
        Action<NotificationKind, string> log, Action<NotificationKind, string> notify, string? approvedPackageHash = null,
        Action<string, string, IXamlMetadataProvider>? registerMetadataProvider = null, Action? commitActivation = null,
        Func<Task<string>>? locateGamePath = null)
    {
        if (!queue.HasThreadAccess) throw new InvalidOperationException("Activate on the UI thread.");
        if (identity.UiMode == "compiled" && (registerMetadataProvider is null || identity.MetadataProvider is null))
            throw new NotSupportedException("Compiled XAML requires explicit host opt-in and a metadata provider.");
        PackagePaths.ValidateIdentifier(identity.Id);
        PackagePaths.RejectReparseAncestors(dllPath);
        var preparation = await Task.Run(() =>
        {
            using FileStream input = File.OpenRead(dllPath);
            string entryHash = Convert.ToHexStringLower(SHA256.HashData(input));
            return (Snapshot: SettingStore.Read(identity.SettingsDirectory), Hash: entryHash);
        });
        var snapshot = preparation.Snapshot;
        if (!queue.HasThreadAccess) throw new InvalidOperationException("Host dispatcher synchronization context is required.");
        var context = new PluginHostContext(identity, queue, xamlRoot, log, notify, snapshot, locateGamePath);
        PluginSession session = new(queue, context, identity.Id);
        PluginResidency.Acquire(identity.Id, dllPath, identity.Version, (approvedPackageHash ?? "development") + ":" + preparation.Hash);
        try
        {
            session.plugin = NativePluginLoader.Load(dllPath);
            if (identity.UiMode == "compiled")
                registerMetadataProvider!(Path.GetFullPath(dllPath), identity.MetadataProvider!,
                    NativePluginLoader.GetMetadataProvider(dllPath, identity.MetadataProvider!));
            session.plugin.OnLoad(context);
            if (runInstallHook)
            {
                context.BeginInstall();
                session.plugin.OnInstall();
                markInstallCompleted();
            }
            context.BeginEnable();
            session.enableEntered = true;
            session.plugin.OnEnable();
            context.ValidateContributions();
            commitActivation?.Invoke();
            context.Commit();
            return session;
        }
        catch
        {
            try { context.BeginClose(); if (session.enableEntered) session.plugin?.OnDisable(); }
            catch (Exception error) { log(NotificationKind.Error, error.ToString()); }
            finally
            {
                try { context.Close(); }
                finally { session.plugin = null; PluginResidency.Release(identity.Id, quarantine: true); }
            }
            throw;
        }
    }

    public UIElement OpenView(string viewId)
    {
        CheckThread();
        if (closing) throw new InvalidOperationException("Session closing.");
        CloseView();
        IPluginView created = context.OpenView(viewId);
        try
        {
            UIElement element = created.Element ?? throw new InvalidOperationException("Plugin returned null UIElement.");
            view = created;
            return element;
        }
        catch { created.OnClosing(); throw; }
    }

    // Host must detach the visual from its content presenter after calling this.
    public void CloseView()
    {
        CheckThread();
        IPluginView? current = view;
        view = null;
        current?.OnClosing();
    }

    public async Task ExecuteAsync(string commandId)
    {
        CheckThread();
        if (closing || operation is not null) throw new InvalidOperationException("Session closing or command already active.");
        IAsyncAction current = context.Execute(commandId);
        operation = current;
        Task task = current.AsTask();
        operationTask = task;
        try { await task; }
        finally
        {
            if (ReferenceEquals(operation, current)) { operation = null; operationTask = null; }
        }
    }

    public int ReadInt32(string name) => context.GetInt32(name);

    public static async Task<PluginSession> ActivateApprovedPackageAsync(string packagePath, string approvedSha256,
        string workspace, string settingsDirectory, Version hostVersion, bool runInstallHook, Action markInstallCompleted,
        DispatcherQueue queue, Func<XamlRoot?> xamlRoot, Action<NotificationKind, string> log, Action<NotificationKind, string> notify,
        Action<string, string, IXamlMetadataProvider>? registerMetadataProvider = null, Func<Task<string>>? locateGamePath = null)
    {
        if (!queue.HasThreadAccess) throw new InvalidOperationException("Activate on the UI thread.");
        var verified = await Task.Run(() => PluginPackage.ExtractApproved(packagePath, approvedSha256, workspace, hostVersion));
        var manifest = verified.Manifest;
        if (manifest.UiMode == "compiled" && registerMetadataProvider is null)
            throw new NotSupportedException("Compiled XAML is not enabled by this host.");
        string data = Path.Combine(settingsDirectory, "data");
        PackagePaths.RejectReparseAncestors(data);
        Directory.CreateDirectory(data);
        PluginIdentity identity = new(manifest.Id, manifest.Name, manifest.Version.ToString(), workspace, data, settingsDirectory,
            manifest.UiMode, manifest.Capabilities, manifest.Files.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase), manifest.MetadataProvider);
        return await ActivateTrustedAsync(PackagePaths.Resolve(workspace, manifest.Entry), identity, runInstallHook,
            markInstallCompleted, queue, xamlRoot, log, notify, verified.Sha256, registerMetadataProvider, locateGamePath: locateGamePath);
    }

    public async Task DisableAsync(Action detachContent, bool uninstall = false)
    {
        CheckThread();
        if (closing) throw new InvalidOperationException("Disable already in progress.");
        if (plugin is null) return;
        closing = true;
        List<Exception> errors = [];
        try { context.BeginClose(); } catch (Exception error) { errors.Add(error); }
        try { CloseView(); } catch (Exception error) { errors.Add(error); }
        try { detachContent(); } catch (Exception error) { errors.Add(error); }
        IAsyncAction? running = operation;
        Task? runningTask = operationTask;
        if (running is not null)
        {
            try { running.Cancel(); } catch (Exception error) { errors.Add(error); }
            try { if (runningTask is not null) await runningTask.WaitAsync(TimeSpan.FromSeconds(5)); }
            catch (Exception error) { errors.Add(error); }
        }
        try { await context.DrainAsync().WaitAsync(TimeSpan.FromSeconds(5)); } catch (Exception error) { errors.Add(error); }
        try { if (enableEntered) plugin.OnDisable(); } catch (Exception error) { errors.Add(error); }
        try { if (uninstall) plugin.OnUninstall(); } catch (Exception error) { errors.Add(error); }
        finally
        {
            try { context.Close(); }
            catch (Exception error) { errors.Add(error); }
            finally
            {
                plugin = null;
                operation = null;
                operationTask = null;
                PluginResidency.Release(id, quarantine: errors.Count != 0);
            }
        }
        if (errors.Count != 0) throw new AggregateException("Plugin disabled with errors; restart may be required.", errors);
    }

    private void CheckThread()
    {
        if (!queue.HasThreadAccess) throw new InvalidOperationException("Plugin session is UI-thread confined.");
    }
}
