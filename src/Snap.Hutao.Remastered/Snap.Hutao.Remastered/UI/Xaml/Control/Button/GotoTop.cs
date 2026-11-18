using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Snap.Hutao.Remastered.UI.Xaml.Control.Button
{
    public partial class GotoTop : DevWinUI.GotoTop
    {
        public override void CreateGotoAction(DependencyObject target)
        {
            if (target is GridView gridView)
            {
                gridView.ApplyTemplate();

                if (VisualTreeHelper.GetChildrenCount(gridView) == 0)
                {
                    return;
                }

                if (VisualTreeHelper.GetChild(gridView, 0) is FrameworkElement root
                    && root.FindName("ScrollViewer") is ScrollViewer scrollViewer)
                {
                    base.CreateGotoAction(scrollViewer);
                }

                return;
            }

            base.CreateGotoAction(target);
        }
    }
}
