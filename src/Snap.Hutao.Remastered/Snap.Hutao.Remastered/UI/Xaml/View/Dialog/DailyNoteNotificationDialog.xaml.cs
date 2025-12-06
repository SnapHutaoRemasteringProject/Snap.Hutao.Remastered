// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml.Controls;
using Snap.Hutao.Remastered.Model.Entity;

namespace Snap.Hutao.Remastered.UI.Xaml.View.Dialog;

internal sealed partial class DailyNoteNotificationDialog : ContentDialog
{
    public DailyNoteNotificationDialog(IServiceProvider serviceProvider, DailyNoteEntry entry)
    {
        InitializeComponent();

        DataContext = entry;
    }
}
