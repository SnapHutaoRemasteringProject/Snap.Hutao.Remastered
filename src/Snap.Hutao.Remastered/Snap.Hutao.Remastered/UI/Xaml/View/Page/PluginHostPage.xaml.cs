using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Snap.Hutao.Remastered.Service.Navigation;
using Snap.Hutao.Remastered.Service.Plugin;
using Snap.Hutao.Remastered.UI.Content;
using Snap.Hutao.Remastered.UI.Xaml.Control;

namespace Snap.Hutao.Remastered.UI.Xaml.View.Page;

public sealed partial class PluginHostPage : ScopedPage
{
    private PluginService? service;
    internal PluginNavigationTarget? Target { get; private set; }

    public PluginHostPage()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += (_, _) => Detach();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        Target = (e.Parameter as INavigationExtraData)?.Data as PluginNavigationTarget;
        base.OnNavigatedTo(e);
    }

    protected override void LoadingOverride()
    {
        if (XamlRoot is { } root && root.XamlContext() is { } context)
        {
            service = context.ServiceProvider.GetRequiredService<PluginService>();
            service.AttachRoot(root);
        }
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        Detach();
        base.OnNavigatedFrom(e);
    }

    private void ClearContent() => PluginContent.Content = null;

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        try
        {
            if (service is null || Target is null) throw new InvalidOperationException("Plugin navigation target unavailable.");
            PluginContent.Content = service.OpenView(Target, ClearContent);
        }
        catch (Exception error)
        {
            Detach();
            Status.IsOpen = true; Status.Severity = InfoBarSeverity.Error; Status.Message = error.ToString();
        }
    }

    private void Detach()
    {
        try { if (Target is { } target) service?.CloseView(target.PluginId, ClearContent); }
        catch (Exception error) { SentrySdk.CaptureException(error); }
        finally { ClearContent(); }
    }
}
