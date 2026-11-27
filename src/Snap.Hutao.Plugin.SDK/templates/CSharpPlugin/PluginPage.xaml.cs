using Microsoft.UI.Xaml.Controls;

namespace Example.MyPlugin;

public sealed partial class PluginPage : Page
{
    public PluginPage() => InitializeComponent();

    public string Title => "My Plugin";

    internal Button ActionButton => ClickButton;
}
