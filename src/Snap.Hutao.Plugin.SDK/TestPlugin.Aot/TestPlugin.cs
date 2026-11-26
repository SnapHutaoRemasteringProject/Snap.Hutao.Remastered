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
        Context.RegisterCommand("exercise", "Exercise ABI", ExerciseAsync);
    }

    public override void OnUninstall() => Context.Log(NotificationKind.Information, "C# AOT OnUninstall");

    private async Task ExerciseAsync()
    {
        await counter.SetValueAsync(counter.Value + 1);
        await enabled.SetValueAsync(false);
        await message.SetValueAsync("C# AOT round trip");
        if (enabled.Value || message.Value != "C# AOT round trip") throw new InvalidOperationException("Settings round trip failed.");
        PluginDispatcher dispatcher = Context.CaptureDispatcher();
        await Task.Run(async () =>
        {
            if (dispatcher.HasThreadAccess) throw new InvalidOperationException("Expected worker.");
            await dispatcher.InvokeAsync(() =>
            {
                if (!dispatcher.HasThreadAccess) throw new InvalidOperationException("Expected UI thread.");
                Context.Log(NotificationKind.Information, "C# AOT worker callback on UI thread");
            });
        });
        if (await Context.ShowMessageAsync("C# AOT ABI", "Choose OK to complete the test.") != DialogResult.Primary)
            throw new OperationCanceledException();
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

    [WinRT.DynamicWindowsRuntimeCast(typeof(StackPanel))]
    [WinRT.DynamicWindowsRuntimeCast(typeof(Button))]
    [WinRT.DynamicWindowsRuntimeCast(typeof(TextBlock))]
    [WinRT.DynamicWindowsRuntimeCast(typeof(TextBox))]
    private PluginView CreateViewCore()
    {
        // Real WinUI controls created by the host's XAML parser, no compiled-XAML resource hooks.
        StackPanel root = Context.ParseXaml("""
            <StackPanel xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" Spacing="8">
                <Button x:Name="ClickButton" Content="Click C# AOT plugin-owned Button"/>
                <TextBlock x:Name="CounterText"/>
                <TextBox x:Name="MessageBox" Header="Message"/>
                <Button x:Name="SaveButton" Content="Save message"/>
            </StackPanel>
            """) as StackPanel ?? throw new InvalidCastException("ParseXaml did not return a StackPanel projection.");
        Button click = root.FindName("ClickButton") as Button ?? throw new InvalidCastException("ClickButton is not a Button projection.");
        TextBlock count = root.FindName("CounterText") as TextBlock ?? throw new InvalidCastException("CounterText is not a TextBlock projection.");
        TextBox edit = root.FindName("MessageBox") as TextBox ?? throw new InvalidCastException("MessageBox is not a TextBox projection.");
        Button save = root.FindName("SaveButton") as Button ?? throw new InvalidCastException("SaveButton is not a Button projection.");
        count.Text = counter.Value.ToString();
        edit.Text = message.Value;
        IDisposable subscription = counter.Subscribe(() => count.Text = counter.Value.ToString());
        async void Click(object sender, RoutedEventArgs args)
        {
            try
            {
                await counter.SetValueAsync(counter.Value + 1);
                click.Content = "C# AOT plugin click executed";
            }
            catch (Exception error) { Context.Log(NotificationKind.Error, error.ToString()); }
        }
        async void Save(object sender, RoutedEventArgs args)
        {
            try { await message.SetValueAsync(edit.Text); }
            catch (Exception error) { Context.Log(NotificationKind.Error, error.ToString()); }
        }
        // AOT runtime's event continuations need their own dispatcher synchronization context.
        void ClickOnUi(object sender, RoutedEventArgs args) => RunOnUi(() => Click(sender, args));
        void SaveOnUi(object sender, RoutedEventArgs args) => RunOnUi(() => Save(sender, args));
        click.Click += ClickOnUi;
        save.Click += SaveOnUi;
        return new(root, () =>
        {
            click.Click -= ClickOnUi;
            save.Click -= SaveOnUi;
            subscription.Dispose();
        });
    }

    private static void RunOnUi(Action action)
    {
        SynchronizationContext? previous = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(new Microsoft.UI.Dispatching.DispatcherQueueSynchronizationContext(Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread()));
        try { action(); }
        finally { SynchronizationContext.SetSynchronizationContext(previous); }
    }
}
