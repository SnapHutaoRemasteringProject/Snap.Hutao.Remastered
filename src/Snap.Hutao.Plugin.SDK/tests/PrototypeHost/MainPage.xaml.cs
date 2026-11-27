using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Snap.Hutao.Plugin.Contracts;
using Snap.Hutao.Plugin.Packaging;
using Snap.Hutao.Remastered.Service.Plugin;

namespace PrototypeHost;

public sealed partial class MainPage : Page
{
    private PluginSession? session;
    private NavigationContribution[] availableViews = [];
    private CommandContribution[] availableCommands = [];
    private bool busy;
    private bool restartRequired;
    private string? entryPath;
    private string? approvalKey;
    private string? approvedPackageHash;
    private PluginIdentity? identity;
    private readonly HashSet<string> installed = new(StringComparer.OrdinalIgnoreCase);
    internal static readonly string TestRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Snap.Hutao.Plugin.SDK.Tests");


    public MainPage()
    {
        InitializeComponent();
        string[] args = Environment.GetCommandLineArgs();
        if (args.Length == 2) pathBox.Text = args[1];
        void Write(NotificationKind kind, string message)
        {
            log.Text += $"[{kind}] {message}" + Environment.NewLine;
            System.Diagnostics.Debug.WriteLine(message);
        }

        void UpdateButtons()
        {
            bool active = session is not null;
            load.IsEnabled = pathBox.IsEnabled = nativeChoice.IsEnabled = compiledChoice.IsEnabled = aotChoice.IsEnabled = !busy && !active && !restartRequired;
            views.IsEnabled = !busy && active && !restartRequired;
            commands.IsEnabled = exercise.IsEnabled = !busy && active && !restartRequired && availableCommands.Length > 0;
            restart.IsEnabled = uninstall.IsEnabled = !busy && active && !restartRequired;
        }

        void Choose(string packageName)
        {
            for (DirectoryInfo? directory = new(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
            {
                if (!File.Exists(Path.Combine(directory.FullName, "Snap.Hutao.Plugin.SDK.slnx"))) continue;
                string path = Path.Combine(directory.FullName, "artifacts", packageName);
                pathBox.Text = path;
                if (!File.Exists(path)) Write(NotificationKind.Warning, "Package not found. Build and pack samples: " + path);
                return;
            }
            Write(NotificationKind.Warning, "Enter the full package path.");
        }

        async Task StartAsync(string input, bool reenable)
        {
            if (!reenable)
            {
                string path = Path.GetFullPath(input);
                entryPath = null; identity = null; approvalKey = null; approvedPackageHash = null;
                if (path.EndsWith(".hutao", StringComparison.OrdinalIgnoreCase))
                {
                    VerifiedPackage verified = await Task.Run(() => PluginPackage.Verify(path));
                    Write(NotificationKind.Information, $"Package: {path}\nID: {verified.Manifest.Id}; UI: {verified.Manifest.UiMode}; entry: {verified.Manifest.Entry}\nSHA256: {verified.Sha256}");
                    ContentDialog confirm = new()
                    {
                        XamlRoot = panel.XamlRoot,
                        Title = "Trust native plugin?",
                        Content = $"{verified.Manifest.Name} ({verified.Manifest.Id})\nSHA256: {verified.Sha256}\nThis plugin executes arbitrary native code in this process.",
                        PrimaryButtonText = "Trust this package",
                        CloseButtonText = "Cancel"
                    };
                    if (await confirm.ShowAsync() != ContentDialogResult.Primary) return;
                    string workspace = Path.Combine(TestRoot, "workspaces", Environment.ProcessId.ToString(), Guid.NewGuid().ToString("N"));
                    string settings = Path.Combine(TestRoot, "settings", verified.Manifest.Id);
                    string key = verified.Manifest.Id + ":" + verified.Sha256;
                    session = await PluginSession.ActivateApprovedPackageAsync(path, verified.Sha256, workspace, settings, new Version(1, 0, 0),
                        !installed.Contains(key), () => installed.Add(key), DispatcherQueue, () => panel.XamlRoot, Write, Write, App.CurrentHost.RegisterMetadataProvider);
                    var manifest = verified.Manifest;
                    identity = new(manifest.Id, manifest.Name, manifest.Version.ToString(), workspace, Path.Combine(settings, "data"),
                        settings, manifest.UiMode, manifest.Capabilities, manifest.Files.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase), manifest.MetadataProvider);
                    entryPath = PackagePaths.Resolve(workspace, manifest.Entry);
                    approvalKey = key;
                    approvedPackageHash = verified.Sha256;
                }
                else
                {
                    if (!path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Choose a .hutao package or development DLL.");
                    if (Path.GetFileName(path).Equals("TestPlugin.Native.dll", StringComparison.OrdinalIgnoreCase) || Path.GetFileName(path).Equals("TestPlugin.Native.Core.dll", StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException("Pack the native sample first: its resources require the verified package.");
                    ContentDialog confirm = new()
                    {
                        XamlRoot = panel.XamlRoot,
                        Title = "Trust development DLL?",
                        Content = path + "\nThis DLL executes native code in this process.",
                        PrimaryButtonText = "Load",
                        CloseButtonText = "Cancel"
                    };
                    if (await confirm.ShowAsync() != ContentDialogResult.Primary) return;
                    string id = Path.GetFileNameWithoutExtension(path);
                    PackagePaths.ValidateIdentifier(id);
                    string settings = Path.Combine(TestRoot, "settings", id);
                    identity = new(id, id, "2.0.0", Path.GetDirectoryName(path)!, Path.Combine(settings, "data"), settings, "loose",
                        new HashSet<string>(StringComparer.Ordinal) { "settings", "notifications", "navigation", "commands", "dialogs", "xaml" },
                        new HashSet<string>(StringComparer.OrdinalIgnoreCase));
                    entryPath = path;
                    approvalKey = id + ":" + path;
                }
            }
            if (session is null)
            {
                PluginIdentity trusted = identity ?? throw new InvalidOperationException("No trusted plugin.");
                string key = approvalKey!;
                Directory.CreateDirectory(trusted.DataDirectory);
                session = await PluginSession.ActivateTrustedAsync(entryPath!, trusted, !installed.Contains(key), () => installed.Add(key),
                    DispatcherQueue, () => panel.XamlRoot, Write, Write, approvedPackageHash, App.CurrentHost.RegisterMetadataProvider);
            }
            try
            {
                availableViews = session.Navigation.ToArray();
                availableCommands = session.Commands.ToArray();
                commands.Items.Clear();
                foreach (CommandContribution item in availableCommands) commands.Items.Add(item.Title);
                commands.SelectedIndex = availableCommands.Length > 0 ? 0 : -1;
                views.Items.Clear();
                foreach (NavigationContribution item in availableViews) views.Items.Add(item.Title);
                views.SelectedIndex = availableViews.Length > 0 ? 0 : -1;
                OpenSelectedView();
                Write(NotificationKind.Success, "Activated through the production host sources.");
            }
            catch
            {
                try { await StopAsync(false); } catch (Exception error) { Write(NotificationKind.Error, error.ToString()); }
                throw;
            }
        }

        void OpenSelectedView()
        {
            if (session is null || views.SelectedIndex < 0 || views.SelectedIndex >= availableViews.Length) return;
            session.CloseView();
            content.Content = null;
            string viewId = availableViews[views.SelectedIndex].ViewId;
            Write(NotificationKind.Information, $"Opening {identity?.Id}/{viewId}");
            content.Content = session.OpenView(viewId);
        }

        async Task StopAsync(bool remove)
        {
            PluginSession? active = session;
            if (active is null) return;
            try { await active.DisableAsync(() => content.Content = null, remove); }
            catch { restartRequired = true; throw; }
            finally { session = null; availableViews = []; availableCommands = []; views.Items.Clear(); commands.Items.Clear(); }
        }

        async Task RunAsync(Func<Task> action)
        {
            if (busy || restartRequired) return;
            busy = true; UpdateButtons();
            try { await action(); }
            catch (Exception error) { Write(NotificationKind.Error, error.ToString()); }
            finally { busy = false; UpdateButtons(); }
        }

        nativeChoice.Click += (_, _) => Choose("TestPlugin.Native.Core.hutao");
        compiledChoice.Click += (_, _) => Choose("TestPlugin.Native.hutao");
        aotChoice.Click += (_, _) => Choose("TestPlugin.hutao");
        views.SelectionChanged += (_, _) =>
        {
            if (busy || session is null) return;
            try { OpenSelectedView(); }
            catch (Exception error) { Write(NotificationKind.Error, error.ToString()); }
        };
        load.Click += async (_, _) => await RunAsync(() => StartAsync(pathBox.Text, false));
        exercise.Click += async (_, _) => await RunAsync(async () =>
        {
            if (session is null || commands.SelectedIndex < 0 || commands.SelectedIndex >= availableCommands.Length) return;
            CommandContribution command = availableCommands[commands.SelectedIndex];
            await session.ExecuteAsync(command.Id);
            Write(NotificationKind.Success, "Command completed: " + command.Title);
        });
        restart.Click += async (_, _) => await RunAsync(async () =>
        {
            await StopAsync(false);
            await StartAsync(entryPath!, true);
        });
        uninstall.Click += async (_, _) => await RunAsync(async () =>
        {
            await StopAsync(true);
            if (approvalKey is not null) installed.Remove(approvalKey);
            Write(NotificationKind.Information, "Session uninstalled. Test data is retained. Restart to load a different package.");
            restartRequired = true;
        });

        UpdateButtons();
    }

    internal async Task StopOnCloseAsync()
    {
        if (session is not { IsClosing: false } active) return;
        try { await active.DisableAsync(() => content.Content = null); }
        catch (Exception error) { System.Diagnostics.Debug.WriteLine(error); }
        finally { session = null; }
    }
}
