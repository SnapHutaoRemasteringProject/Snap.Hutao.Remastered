using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Snap.Hutao.Plugin.Contracts;
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

internal sealed partial class PrototypeApp(string[] args) : Application
{
    private Window? window;
    private IHutaoPlugin? plugin;
    private ProbeContext? context;
    private IPluginView? view;
    private bool busy;

    protected override void OnLaunched(LaunchActivatedEventArgs launchArgs)
    {
        window = new Window { Title = "Hutao Plugin SDK G1 ABI prototype" };
        StackPanel panel = new() { Spacing = 12, Padding = new Thickness(16) };
        TextBlock log = new() { TextWrapping = TextWrapping.Wrap };
        ContentControl content = new();
        Button exercise = new() { Content = "Exercise async ABI (choose OK)", IsEnabled = false };
        Button restart = new() { Content = "Disable and recreate plugin", IsEnabled = false };
        TextBox dllPath = new() { Header = "Trusted native plugin DLL", PlaceholderText = "Full path to TestPlugin.Native.dll or TestPlugin.Aot.dll", Text = args.Length == 1 ? args[0] : string.Empty };
        Button load = new() { Content = "Load plugin" };
        StackPanel choices = new() { Orientation = Orientation.Horizontal, Spacing = 8 };
        Button nativeChoice = new() { Content = "C++ sample" };
        Button aotChoice = new() { Content = "C# AOT sample" };
        choices.Children.Add(nativeChoice);
        choices.Children.Add(aotChoice);
        panel.Children.Add(dllPath);
        panel.Children.Add(choices);
        panel.Children.Add(load);
        panel.Children.Add(content);
        panel.Children.Add(exercise);
        panel.Children.Add(restart);
        panel.Children.Add(log);
        window.Content = new ScrollViewer { Content = panel };
        window.Activate();

        void Write(string message)
        {
            log.Text += message + Environment.NewLine;
            System.Diagnostics.Debug.WriteLine(message);
        }

        string? activePath = null;

        void ChooseSample(bool native)
        {
            for (DirectoryInfo? directory = new(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
            {
                if (!File.Exists(Path.Combine(directory.FullName, "Build-Prototype.ps1"))) continue;
                string artifacts = Path.Combine(directory.FullName, "artifacts");
                string[] candidates = native
                    ? [Path.Combine(artifacts, "native", "Debug", "TestPlugin.Native.dll"),
                       Path.Combine(artifacts, "native", "Release", "TestPlugin.Native.dll"),
                       Path.Combine(artifacts, "native", "TestPlugin.Native.dll")]
                    : [Path.Combine(artifacts, "aot-plugin", "TestPlugin.Aot.dll")];
                dllPath.Text = candidates.FirstOrDefault(File.Exists) ?? candidates[0];
                return;
            }
            Write("Cannot locate the SDK directory. Enter the DLL path manually.");
        }

        void Start(string path)
        {
            if (plugin is not null) throw new InvalidOperationException("Stop the current plugin first.");
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Select a trusted native plugin DLL.");
            activePath = Path.GetFullPath(path);
            plugin = NativePluginLoader.Load(activePath);
            context = new ProbeContext(window, activePath, Write);
            plugin.OnLoad(context);
            plugin.OnInstall();
            plugin.OnEnable();
            view = context.ViewFactory?.CreateView("main") ?? throw new InvalidOperationException("No view.");
            content.Content = view.Element ?? throw new InvalidOperationException("Null UIElement.");
            if (context.Command is null || !context.NavigationRegistered) throw new InvalidOperationException("Missing contributions.");
            Write("Activation and UIElement transfer succeeded. Click plugin button and exercise async ABI.");
            exercise.IsEnabled = restart.IsEnabled = true;
            load.IsEnabled = dllPath.IsEnabled = nativeChoice.IsEnabled = aotChoice.IsEnabled = false;
        }

        void Stop()
        {
            exercise.IsEnabled = restart.IsEnabled = false;
            view?.OnClosing();
            content.Content = null;
            view = null;
            plugin?.OnDisable();
            if (context?.HasContributions == true) throw new InvalidOperationException("Contributions not revoked.");
            plugin?.OnUninstall();
            plugin = null;
            context = null;
            load.IsEnabled = dllPath.IsEnabled = nativeChoice.IsEnabled = aotChoice.IsEnabled = true;
        }

        nativeChoice.Click += (_, _) => ChooseSample(true);
        aotChoice.Click += (_, _) => ChooseSample(false);
        load.Click += (_, _) =>
        {
            try { Start(dllPath.Text); }
            catch (Exception ex) { Write(ex.ToString()); }
        };

        exercise.Click += async (_, _) =>
        {
            if (busy || context is null) return;
            busy = true;
            exercise.IsEnabled = restart.IsEnabled = false;
            try
            {
                await (context.Command ?? throw new InvalidOperationException()).ExecuteAsync("exercise");
                Write("Async exercise succeeded; Counter = " + context.GetInt32("Counter"));
            }
            catch (Exception ex) { Write(ex.ToString()); }
            finally { busy = false; exercise.IsEnabled = restart.IsEnabled = true; }
        };
        restart.Click += (_, _) =>
        {
            if (busy) return;
            try { Stop(); Start(activePath ?? throw new InvalidOperationException("No plugin path.")); }
            catch (Exception ex) { Write(ex.ToString()); }
        };
        window.Closed += (_, _) =>
        {
            if (!busy)
            {
                try { Stop(); }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); }
            }
        };
        if (args.Length == 1)
        {
            try { Start(args[0]); }
            catch (Exception ex) { Write(ex.ToString()); }
        }
    }
}
