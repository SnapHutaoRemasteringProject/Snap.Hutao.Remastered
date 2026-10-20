// Copyright (c) Snap Hutao RP. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.UI.Xaml.View.Window;

/// <summary>
/// Opens a single reusable account insights window. The repository and metadata
/// services are scoped to the window and are released by XamlWindowController.
/// </summary>
public static class AccountInsightsWindows
{
    private static AccountInsightsWindow? current;

    public static void Open()
    {
        if (current is null)
        {
            current = Ioc.Default.GetRequiredService<AccountInsightsWindow>();
            current.Closed += static (_, args) =>
            {
                if (!args.Handled)
                {
                    current = null;
                }
            };
        }

        current.Activate();
    }
}
