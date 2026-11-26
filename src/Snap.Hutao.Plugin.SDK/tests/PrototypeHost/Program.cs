using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Snap.Hutao.Plugin.Contracts;
using Snap.Hutao.Plugin.Host;
using Snap.Hutao.Plugin.Packaging;
using WinRT;

namespace PrototypeHost;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        ComWrappersSupport.InitializeComWrappers();
        Application.Start(initialization =>
        {
            SynchronizationContext.SetSynchronizationContext(new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread()));
            _ = new PrototypeApp(args);
        });
    }
}

internal sealed partial class PrototypeApp(string[] args) : Application, IXamlMetadataProvider
{
    // Compiled-XAML integration harness only. The WinUI schema may cache types;
    // keep metadata providers alive until process exit, just like the DLLs.
    private readonly Dictionary<string, (string Path, IXamlMetadataProvider Provider)> providers = new(StringComparer.Ordinal);

    private void RegisterMetadataProvider(string path, string className, IXamlMetadataProvider provider)
    {
        if (providers.TryGetValue(className, out var existing))
        {
            if (!string.Equals(existing.Path, path, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("XAML metadata name collision: " + className);
            return;
        }
        providers.Add(className, (path, provider));
    }

    public IXamlType? GetXamlType(Type type)
    {
        foreach (var item in providers.Values)
            if (item.Provider.GetXamlType(type) is { } result) return result;
        return null;
    }

    public IXamlType? GetXamlType(string fullName)
    {
        foreach (var item in providers.Values)
            if (item.Provider.GetXamlType(fullName) is { } result) return result;
        return null;
    }

    public XmlnsDefinition[] GetXmlnsDefinitions() => providers.Values.SelectMany(item => item.Provider.GetXmlnsDefinitions()).ToArray();

    private Window? window;
    private PluginSession? session;
    private bool busy;
    private bool restartRequired;
    private string? entryPath;
    private string? approvalKey;
    private string? approvedPackageHash;
    private PluginIdentity? identity;
    private readonly HashSet<string> installed = new(StringComparer.OrdinalIgnoreCase);
    private static readonly string TestRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Snap.Hutao.Plugin.SDK.Tests");

    protected override void OnLaunched(LaunchActivatedEventArgs launchArgs)
    {
        window = new Window { Title = "Hutao Plugin SDK 2.0 integration host" };
        StackPanel panel = new() { Spacing = 12, Padding = new Thickness(16) };
        TextBlock log = new() { TextWrapping = TextWrapping.Wrap };
        ContentControl content = new();
        TextBox pathBox = new() { Header = "Trusted development DLL or .hutao package", Text = args.Length == 1 ? args[0] : "" };
        Button load = new() { Content = "Inspect and load" };
        Button nativeChoice = new() { Content = "C++ sample" };
        Button aotChoice = new() { Content = "C# AOT sample" };
        Button exercise = new() { Content = "Exercise ABI", IsEnabled = false };
        Button restart = new() { Content = "Disable and re-enable", IsEnabled = false };
        Button uninstall = new() { Content = "Uninstall session", IsEnabled = false };
        StackPanel choices = new() { Orientation = Orientation.Horizontal, Spacing = 8 };
        choices.Children.Add(nativeChoice); choices.Children.Add(aotChoice);
        panel.Children.Add(pathBox); panel.Children.Add(choices); panel.Children.Add(load);
        panel.Children.Add(content); panel.Children.Add(exercise); panel.Children.Add(restart); panel.Children.Add(uninstall); panel.Children.Add(log);
        window.Content = new ScrollViewer { Content = panel };
        window.Activate();

        void Write(NotificationKind kind, string message)
        {
            log.Text += $"[{kind}] {message}" + Environment.NewLine;
            System.Diagnostics.Debug.WriteLine(message);
        }

        void UpdateButtons()
        {
            bool active = session is not null;
            load.IsEnabled = pathBox.IsEnabled = nativeChoice.IsEnabled = aotChoice.IsEnabled = !busy && !active && !restartRequired;
            exercise.IsEnabled = restart.IsEnabled = uninstall.IsEnabled = !busy && active && !restartRequired;
        }

        void Choose(bool native)
        {
            for (DirectoryInfo? directory = new(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
            {
                if (!File.Exists(Path.Combine(directory.FullName, "Snap.Hutao.Plugin.SDK.slnx"))) continue;
                string artifacts = Path.Combine(directory.FullName, "artifacts");
                string[] candidates = native
                    ? [Path.Combine(artifacts, "TestPlugin.Native.hutao"), Path.Combine(artifacts, "native", "Debug", "TestPlugin.Native.dll"), Path.Combine(artifacts, "native", "Release", "TestPlugin.Native.dll")]
                    : [Path.Combine(artifacts, "TestPlugin.hutao"), Path.Combine(artifacts, "aot-plugin", "TestPlugin.Aot.dll")];
                pathBox.Text = candidates.FirstOrDefault(File.Exists) ?? candidates[1];
                return;
            }
            Write(NotificationKind.Warning, "Enter the full DLL or package path.");
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
                    ContentDialog confirm = new()
                    {
                        XamlRoot = panel.XamlRoot, Title = "Trust native plugin?",
                        Content = $"{verified.Manifest.Name} ({verified.Manifest.Id})\nSHA256: {verified.Sha256}\nThis plugin executes arbitrary native code in this process.",
                        PrimaryButtonText = "Trust this package", CloseButtonText = "Cancel"
                    };
                    if (await confirm.ShowAsync() != ContentDialogResult.Primary) return;
                    string workspace = Path.Combine(TestRoot, "workspaces", Environment.ProcessId.ToString(), Guid.NewGuid().ToString("N"));
                    string settings = Path.Combine(TestRoot, "settings", verified.Manifest.Id);
                    string key = verified.Manifest.Id + ":" + verified.Sha256;
                    session = await PluginSession.ActivateApprovedPackageAsync(path, verified.Sha256, workspace, settings, new Version(1,0,0),
                        !installed.Contains(key), () => installed.Add(key), window.DispatcherQueue, () => panel.XamlRoot, Write, Write, RegisterMetadataProvider);
                    var manifest = verified.Manifest;
                    identity = new(manifest.Id, manifest.Name, manifest.Version.ToString(), workspace, Path.Combine(settings,"data"),
                        settings, manifest.UiMode, manifest.Capabilities, manifest.Files.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase), manifest.MetadataProvider);
                    entryPath = PackagePaths.Resolve(workspace, manifest.Entry);
                    approvalKey = key;
                    approvedPackageHash = verified.Sha256;
                }
                else
                {
                    if (!path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Choose a .hutao package or development DLL.");
                    if (Path.GetFileName(path).Equals("TestPlugin.Native.dll", StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException("Pack the native sample first: the compiled XAML requires its verified XBF payload.");
                    ContentDialog confirm = new()
                    {
                        XamlRoot = panel.XamlRoot, Title = "Trust development DLL?",
                        Content = path + "\nThis DLL executes native code in this process.",
                        PrimaryButtonText = "Load", CloseButtonText = "Cancel"
                    };
                    if (await confirm.ShowAsync() != ContentDialogResult.Primary) return;
                    string id = Path.GetFileNameWithoutExtension(path);
                    PackagePaths.ValidateIdentifier(id);
                    string settings = Path.Combine(TestRoot, "settings", id);
                    identity = new(id, id, "2.0.0", Path.GetDirectoryName(path)!, Path.Combine(settings,"data"), settings, "loose",
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
                    window.DispatcherQueue, () => panel.XamlRoot, Write, Write, approvedPackageHash, RegisterMetadataProvider);
            }
            try
            {
                if (session.Navigation.FirstOrDefault() is { } item)
                    content.Content = session.OpenView(item.ViewId);
                Write(NotificationKind.Success, "Activated through the shared Host runtime.");
            }
            catch
            {
                try { await StopAsync(false); } catch (Exception error) { Write(NotificationKind.Error, error.ToString()); }
                throw;
            }
        }

        async Task StopAsync(bool remove)
        {
            PluginSession? active = session;
            if (active is null) return;
            try { await active.DisableAsync(() => content.Content = null, remove); }
            catch { restartRequired = true; throw; }
            finally { session = null; }
        }

        async Task RunAsync(Func<Task> action)
        {
            if (busy || restartRequired) return;
            busy = true; UpdateButtons();
            try { await action(); }
            catch (Exception error) { Write(NotificationKind.Error, error.ToString()); }
            finally { busy = false; UpdateButtons(); }
        }

        nativeChoice.Click += (_, _) => Choose(true);
        aotChoice.Click += (_, _) => Choose(false);
        load.Click += async (_, _) => await RunAsync(() => StartAsync(pathBox.Text, false));
        exercise.Click += async (_, _) => await RunAsync(async () =>
        {
            if (session is null) return;
            await session.ExecuteAsync("exercise");
            Write(NotificationKind.Success, "Counter = " + session.ReadInt32("Counter"));
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
        window.Closed += async (_, _) =>
        {
            if (session is not null && !session.IsClosing)
            {
                try { await StopAsync(false); }
                catch (Exception error) { System.Diagnostics.Debug.WriteLine(error); }
            }
        };
        UpdateButtons();
    }
}
