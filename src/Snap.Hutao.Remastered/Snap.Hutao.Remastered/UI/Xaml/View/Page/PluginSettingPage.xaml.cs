using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Snap.Hutao.Remastered.Service.Navigation;
using Snap.Hutao.Remastered.Service.Plugin;
using Snap.Hutao.Remastered.UI.Content;
using Snap.Hutao.Remastered.UI.Xaml.Control;
using Snap.Hutao.Remastered.ViewModel.Plugin;
using System.Globalization;

namespace Snap.Hutao.Remastered.UI.Xaml.View.Page;

public sealed partial class PluginSettingPage : ScopedPage
{
    private string? id;
    private PluginService service = null!;
    private IServiceProvider services = null!;
    private bool busy;
    private bool unloaded;

    public PluginSettingPage()
    {
        InitializeComponent();
        Loaded += (_, _) => Rebuild();
        Unloaded += (_, _) => unloaded = true;
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        id = (e.Parameter as INavigationExtraData)?.Data as string;
        base.OnNavigatedTo(e);
    }

    protected override void LoadingOverride()
    {
        InitializeDataContext<PluginViewModel>();
        services = XamlRoot.XamlContext()!.ServiceProvider;
        service = services.GetRequiredService<PluginService>();
        this.DataContext<PluginViewModel>()!.MarkReady();
    }

    private void Rebuild()
    {
        if (unloaded) return;
        SettingsPanel.Children.Clear();
        TitleText.Text = id + " — " + SH.ViewPagePluginSettingTitle;
        if (id is null || !service.IsActive(id))
        {
            Status.IsOpen = true;
            Status.Severity = InfoBarSeverity.Warning;
            Status.Message = SH.GetString("ViewPagePluginEnableFirst") ?? "Enable the plugin first.";
            return;
        }
        foreach (RegisteredPluginSetting setting in service.Settings(id))
        {
            StackPanel row = new() { Spacing = 6 };
            row.Children.Add(new TextBlock { Text = setting.Name + " — " + setting.Description, TextWrapping = TextWrapping.Wrap });
            row.Children.Add(new TextBlock { Text = "Default: " + setting.DefaultValue });
            Func<object> get;
            if (setting.Value is bool flag)
            {
                ToggleSwitch editor = new() { IsOn = flag };
                row.Children.Add(editor);
                get = () => editor.IsOn;
            }
            else
            {
                TextBox editor = new() { Text = Convert.ToString(setting.Value, CultureInfo.InvariantCulture) ?? string.Empty };
                row.Children.Add(editor);
                get = setting.Value is int ? () => int.Parse(editor.Text, NumberStyles.Integer, CultureInfo.InvariantCulture) : () => editor.Text;
            }
            StackPanel buttons = new() { Orientation = Orientation.Horizontal, Spacing = 8 };
            Button save = new() { Content = SH.ViewPagePluginSettingSaveButton };
            Button reset = new() { Content = SH.ViewPagePluginSettingResetButton };
            save.Click += async (_, _) => await RunAsync(() => service.SetSettingAsync(id, setting.Name, get()));
            reset.Click += async (_, _) => await RunAsync(() => service.ResetSettingAsync(id, setting.Name));
            buttons.Children.Add(save); buttons.Children.Add(reset); row.Children.Add(buttons);
            SettingsPanel.Children.Add(row);
        }
    }

    private async Task RunAsync(Func<Task> action)
    {
        if (busy || unloaded) return;
        busy = true;
        try
        {
            await action();
            Rebuild();
            if (!unloaded) { Status.IsOpen = true; Status.Severity = InfoBarSeverity.Success; Status.Message = SH.ViewPagePluginSettingSaveSuccess; }
        }
        catch (Exception error)
        {
            if (!unloaded) { Status.IsOpen = true; Status.Severity = InfoBarSeverity.Error; Status.Message = error.ToString(); }
        }
        finally { busy = false; }
    }

    private void BackClick(object sender, RoutedEventArgs e) => services.GetRequiredService<INavigationService>().Navigate<PluginPage>(NavigationExtraData.Default, true);
}
