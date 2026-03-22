using CommunityToolkit.Mvvm.Messaging;
using HarmonyLib;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Snap.Hutao.Remastered.API.Annotation;
using Snap.Hutao.Remastered.Core.Threading;
using Snap.Hutao.Remastered.Service.Notification;
using Snap.Hutao.Remastered.UI.Xaml.View;
using Snap.Hutao.Remastered.UI.Xaml.View.Window;
using System.Reflection;
using Snap.Hutao.Remastered.API.Model;
using Snap.Hutao.Remastered.API;
using Snap.Hutao.Remastered.Service.Plugin;
using Snap.Hutao.Remastered.API.Setting;
using Microsoft.Extensions.DependencyInjection;

namespace TestPluginSDK;

[PluginMain]
public class TestPlugin : HutaoPlugin
{
    public static TestPlugin Instance { get; private set; } = null!;

    public static PluginSetting<int> Counter;
    public static PluginSetting<string> Message;

    private static MainView? mainView;
    private static NavigationViewItem? testPluginItem;
    private static NavigationView? navigationView;

    public PluginContext PluginContext { get; private set; } = null!;
    private IMessenger messenger = null!;
    private ITaskContext taskContext = null!;
    private IPluginService pluginService = null!;
    private Harmony harmony = null!;

    public override void OnDisable()
    {
        harmony?.UnpatchAll();

        taskContext?.InvokeOnMainThread(() =>
        {
            CleanUpPage();
        });

        Console.WriteLine($"{Manifest.Name} disabled!");
    }

    public override void OnEnable()
    {
        ApplyHarmonyPatches();

        taskContext?.InvokeOnMainThread(() =>
        {
            SetupPage(MainWindow.Instance);
        });

        Console.WriteLine($"{Manifest.Name} enabled!");
    }

    private void OnTestPluginItemTapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
    {
        Frame frame = FindChild<Frame>(mainView!)!;
        PluginScopedPage page = new TestPluginPage(this);
        frame.Content = page;
        navigationView?.IsEnabled = true;
    }

    public override void OnInstall()
    {
        Console.WriteLine($"{Manifest.Name} installed!");
    }

    public override void OnLoad(PluginContext context)
    {
        Instance = this;

        PluginContext = context;
        messenger = context.ServiceProvider.GetRequiredService<IMessenger>();
        pluginService = context.ServiceProvider.GetRequiredService<IPluginService>();
        taskContext = context.ServiceProvider.GetRequiredService<ITaskContext>();

        Counter = new PluginSetting<int>(pluginService, nameof(Counter), 0);
        Message = new PluginSetting<string>(pluginService, nameof(Message), "Default Message");

        Counter.Register(Manifest.Id, "Counter setting for Test Plugin");
        Message.Register(Manifest.Id, "Message setting for Test Plugin");

        messenger.Send(InfoBarMessage.Success($"Hello Test Plugin"));
    }

    public override void OnUninstall()
    {
        Console.WriteLine($"{Manifest.Name} uninstalled!");
    }
    
    private void ApplyHarmonyPatches()
    {
        try
        {
            harmony = new Harmony($"{Manifest.Id}");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to apply Harmony patches: {ex.Message}");
        }
    }

    private static T? FindChild<T>(DependencyObject parent) where T : DependencyObject
    {
        if (parent == null) return null;

        int count = VisualTreeHelper.GetChildrenCount(parent);
        for (int i = 0; i < count; i++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(parent, i);
            if (child is T result)
            {
                return result;
            }

            T? descendant = FindChild<T>(child);
            if (descendant != null)
            {
                return descendant;
            }
        }

        return null;
    }

    public static void SetupPage(MainWindow window)
    {
        FieldInfo mainViewField = typeof(MainWindow).DeclaredField("MainView");
        mainView = (MainView)mainViewField.GetValue(window)!;
        navigationView = FindChild<NavigationView>(mainView);

        if (testPluginItem == null)
        {
            testPluginItem = new NavigationViewItem
            {
                Content = "Test Plugin",
                Icon = new FontIcon
                {
                    Glyph = "\uE74C" // Plugin icon
                },
                Tag = "TestPluginSDK"
            };

            testPluginItem.Tapped += Instance.OnTestPluginItemTapped;
        }

        navigationView!.MenuItems.Add(testPluginItem);
    }

    public static void CleanUpPage()
    {
        if (navigationView != null && testPluginItem != null)
        {
            navigationView.MenuItems.Remove(testPluginItem);
            testPluginItem = null;
        }
    }
}

[HarmonyPatch(typeof(MainWindow))]
[HarmonyPatch(MethodType.Constructor, new Type[] { typeof(IServiceProvider) })]
public static class MainWindowConstructorPatch
{
    [HarmonyPostfix]
    public static void Postfix(MainWindow __instance)
    {
        TestPlugin.CleanUpPage();
        TestPlugin.SetupPage(__instance);
    }
}
