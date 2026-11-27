using Microsoft.UI.Xaml;

namespace PrototypeHost;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Closed += async (_, _) => await Harness.StopOnCloseAsync();
    }
}
