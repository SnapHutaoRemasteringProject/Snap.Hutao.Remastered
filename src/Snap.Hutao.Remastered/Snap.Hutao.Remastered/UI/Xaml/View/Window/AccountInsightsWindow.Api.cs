// Copyright (c) Snap Hutao RP. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml;
using Windows.ApplicationModel.DataTransfer;

namespace Snap.Hutao.Remastered.UI.Xaml.View.Window;

[SuppressMessage("Design", "CA1001", Justification = "The XAML window shutdown path disposes lifetime after pending operations finish in DisposeClosedResources.")]
public sealed partial class AccountInsightsWindow
{
    private void OnCopyClicked(object sender, RoutedEventArgs args)
    {
        if (snapshotJson is null)
        {
            return;
        }

        DataPackage package = new();
        package.SetText(snapshotJson);
        Clipboard.SetContent(package);
        StatusText.Text = L("Copied");
    }

}
