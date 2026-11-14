// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml.Controls;
using Snap.Hutao.Remastered.Model.Entity;

namespace Snap.Hutao.Remastered.UI.Xaml.View.Dialog;

public sealed partial class DailyNoteNotificationDialog : ContentDialog
{
    private DailyNoteEntry Entry { get; }

    public DailyNoteNotificationDialog(IServiceProvider serviceProvider, DailyNoteEntry entry)
    {
        Entry = entry;
        InitializeComponent();

        DataContext = entry;
    }
}
