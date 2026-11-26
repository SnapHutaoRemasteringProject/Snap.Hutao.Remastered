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

        void Start()
        {
            if (args.Length != 1) throw new ArgumentException("Pass one trusted native plugin DLL.");
            plugin = NativePluginLoader.Load(args[0]);
            context = new ProbeContext(window, Path.GetFullPath(args[0]), Write);
            plugin.OnLoad(context);
            plugin.OnInstall();
            plugin.OnEnable();
            view = context.ViewFactory?.CreateView("main") ?? throw new InvalidOperationException("No view.");
            content.Content = view.Element ?? throw new InvalidOperationException("Null UIElement.");
            if (context.Command is null || !context.NavigationRegistered) throw new InvalidOperationException("Missing contributions.");
            Write("Activation and UIElement transfer succeeded. Click plugin button and exercise async ABI.");
            exercise.IsEnabled = restart.IsEnabled = true;
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
        }

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
            try { Stop(); Start(); }
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
        try { Start(); }
        catch (Exception ex) { Write(ex.ToString()); }
    }
}
