using Microsoft.UI.Xaml.Controls;
namespace Snap.Hutao.Remastered.UI.Xaml.View.Specialized;

[DependencyProperty<bool>("ShowUpPull", DefaultValue = true, NotNull = true)]
public sealed partial class BeyondStatisticsCard : UserControl
{
    public BeyondStatisticsCard()
    {
        InitializeComponent();
    }
}
