using Microsoft.UI.Xaml.Controls;

namespace TestPlugin.Aot;

public sealed partial class PluginPage : Page
{
    public PluginPage() => InitializeComponent();

    public string InitialCaption => "Click C# AOT plugin-owned Button";

    internal Button CounterButton => ClickButton;
    internal TextBlock CounterLabel => CounterText;
    internal TextBox MessageEditor => MessageBox;
    internal Button SaveMessageButton => SaveButton;
}
