// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls.Primitives;
using Snap.Hutao.Remastered.UI.Xaml.Control;
using Snap.Hutao.Remastered.ViewModel.Home;
using Snap.Hutao.Remastered.Web.Hoyolab.Hk4e.Common.Announcement;
using System.Runtime.CompilerServices;

namespace Snap.Hutao.Remastered.UI.Xaml.View.Page;

public sealed partial class AnnouncementPage : ScopedPage
{
    public AnnouncementPage()
    {
        InitializeComponent();
    }

    protected override void LoadingOverride()
    {
        InitializeDataContext<AnnouncementViewModel>();
    }

    public new AnnouncementViewModel DataContext { get => Unsafe.As<AnnouncementViewModel>(base.DataContext); set => base.DataContext = value; }

    private AnnouncementListWrapper? GetAnnouncementCategory(AnnouncementWrapper? wrapper, int index)
    {
        return wrapper is not null && !wrapper.List.IsDefaultOrEmpty && (uint)index < (uint)wrapper.List.Length
            ? wrapper.List[index]
            : null;
    }

    private string? GetAnnouncementHeader(AnnouncementWrapper? wrapper, int index)
    {
        return wrapper?.TypeList is { } types && (uint)index < (uint)types.Count
            ? types[index].MI18NName
            : null;
    }

    // A DataTemplate has its own binding scope. Give its button the existing
    // command directly so ButtonBase retains async CanExecute handling.
    [WinRT.DynamicWindowsRuntimeCast(typeof(global::Microsoft.UI.Xaml.Controls.Primitives.ButtonBase))]
    private void OnCopyCodeLoaded(object sender, RoutedEventArgs args)
    {
        if (sender is ButtonBase button && base.DataContext is AnnouncementViewModel viewModel)
        {
            button.Command = viewModel.CopyCodeCommand;
        }
    }

    [WinRT.DynamicWindowsRuntimeCast(typeof(global::Microsoft.UI.Xaml.Controls.Primitives.ButtonBase))]
    private void OnCopyCodeUnloaded(object sender, RoutedEventArgs args)
    {
        if (sender is ButtonBase button)
        {
            button.Command = null;
        }
    }
}
