using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Snap.Hutao.Plugin.Contracts;
using Snap.Hutao.Plugin.SDK;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace TestPlugin.Aot;

public static unsafe class Exports
{
    [UnmanagedCallersOnly(EntryPoint = "HutaoPluginCreate", CallConvs = [typeof(CallConvCdecl)])]
    public static int Create(uint abiMajor, nint* result) => PluginExport.Create<TestPlugin>(abiMajor, result);
}

public sealed class TestPlugin : HutaoPlugin
{
    private PluginSetting<int> counter = null!;
    private PluginSetting<bool> enabled = null!;
    private PluginSetting<string> message = null!;

    public override void OnLoad(PluginContext context)
    {
        counter = context.Settings.Register("Counter", 0, "Counter");
        enabled = context.Settings.Register("Enabled", true, "Boolean");
        message = context.Settings.Register("Message", "Default Message", "Message");
        context.Notify(NotificationKind.Success, "C# AOT OnLoad: " + context.PluginId);
    }

    public override void OnInstall() => Context.Log(NotificationKind.Information, "C# AOT OnInstall");

    public override void OnEnable()
    {
        Context.RegisterView("main", CreateView);
        Context.RegisterNavigationItem("main", "C# AOT plugin", "\uE74C", "main");
        Context.RegisterView("binding", CreateBindingView);
        Context.RegisterNavigationItem("binding", "C# AOT Binding", "\uE74C", "binding");
        Context.RegisterView("programmatic", CreateProgrammaticView);
        Context.RegisterNavigationItem("programmatic", "C# AOT programmatic UI", "\uE74C", "programmatic");
        Context.RegisterCommand("exercise", "Exercise ABI", ExerciseAsync);
        Context.RegisterCommand("game-path", "Exercise C# First service", async () =>
        {
            var service = Context.GamePath ?? throw new NotSupportedException("Authored game path service unavailable.");
            string path = await service.LocateAsync();
            if (!Context.HasThreadAccess) throw new InvalidOperationException("Lost UI thread after authored service.");
            Context.Log(NotificationKind.Success, "C# First service completed; game path found: " + (path.Length != 0));
        });
    }

    public override void OnUninstall() => Context.Log(NotificationKind.Information, "C# AOT OnUninstall");

    private async Task ExerciseAsync()
    {
        PluginDispatcher dispatcher = Context.CaptureDispatcher();
        void RequireUi(string stage)
        {
            if (!dispatcher.HasThreadAccess) throw new InvalidOperationException("Lost plugin UI synchronization context: " + stage);
        }

        // Two forced asynchronous hops expose a context that only survives the first Post.
        await Task.Yield();
        RequireUi("first yield");
        await Task.Yield();
        RequireUi("second yield");
        await counter.SetValueAsync(counter.Value + 1);
        RequireUi("Counter write");
        await enabled.SetValueAsync(false);
        RequireUi("Enabled write");
        await message.SetValueAsync("C# AOT round trip");
        RequireUi("Message write");
        if (enabled.Value || message.Value != "C# AOT round trip") throw new InvalidOperationException("Settings round trip failed.");
        await Task.Run(async () =>
        {
            if (dispatcher.HasThreadAccess) throw new InvalidOperationException("Expected worker.");
            await dispatcher.InvokeAsync(() =>
            {
                if (!dispatcher.HasThreadAccess) throw new InvalidOperationException("Expected UI thread.");
                Context.Log(NotificationKind.Information, "C# AOT worker callback on UI thread");
            });
        });
        RequireUi("worker round trip");
        DialogResult result = await Context.ShowMessageAsync("C# AOT ABI", "Choose OK to complete the test.");
        RequireUi("dialog completion");
        if (result != DialogResult.Primary) throw new OperationCanceledException();
        Context.Notify(NotificationKind.Success, "C# AOT async round trip completed.");
    }

    private PluginView CreateView()
    {
        try { return CreateViewCore(); }
        catch (Exception error)
        {
            Context.Log(NotificationKind.Error, "C# AOT CreateView: " + error);
            throw;
        }
    }

    private PluginView CreateViewCore()
    {
        PluginPage root = new();
        Button click = root.CounterButton;
        TextBlock count = root.CounterLabel;
        TextBox edit = root.MessageEditor;
        Button save = root.SaveMessageButton;
        count.Text = counter.Value.ToString();
        edit.Text = message.Value;
        IDisposable subscription = counter.Subscribe(() => count.Text = counter.Value.ToString());
        bool closed = false;
        async void ClickOnUi(object sender, RoutedEventArgs args)
        {
            try
            {
                await Context.RunOnUiAsync(async () =>
                {
                    if (closed) return;
                    await counter.SetValueAsync(counter.Value + 1);
                    if (!closed) click.Content = "C# AOT plugin click executed";
                });
            }
            catch (Exception error) { System.Diagnostics.Debug.WriteLine(error); }
        }

        async void SaveOnUi(object sender, RoutedEventArgs args)
        {
            try
            {
                await Context.RunOnUiAsync(async () =>
                {
                    if (!closed) await message.SetValueAsync(edit.Text);
                });
            }
            catch (Exception error) { System.Diagnostics.Debug.WriteLine(error); }
        }
        click.Click += ClickOnUi;
        save.Click += SaveOnUi;
        return new(root, () =>
        {
            closed = true;
            click.Click -= ClickOnUi;
            save.Click -= SaveOnUi;
            subscription.Dispose();
        });
    }

    private PluginView CreateBindingView()
    {
        BindingPage root = new();
        SampleViewModel model = new(Context, counter, message);
        root.DataContext = model;
        return new(root, () => { root.DataContext = null; model.Dispose(); });
    }

    private PluginView CreateProgrammaticView()
    {
        Button button = new() { Content = "Programmatic C# AOT Button" };
        void Click(object sender, RoutedEventArgs e)
        {
            button.Content = "C# AOT callback completed";
            Context.Notify(NotificationKind.Success, "C# core click");
        }
        button.Click += Click;
        return new(button, () => button.Click -= Click);
    }
}
