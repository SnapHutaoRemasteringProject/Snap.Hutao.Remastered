// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml.Controls;
using Snap.Hutao.Remastered.ViewModel.User;
using System.Runtime.CompilerServices;
using User = Snap.Hutao.Remastered.ViewModel.User.User;

namespace Snap.Hutao.Remastered.UI.Xaml.View;

public sealed partial class UserView : UserControl, INotifyPropertyChanged
{
    public UserView()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => PropertyChanged?.Invoke(this, new(nameof(ViewModel)));
    }

    // Close both nested and parent flyouts when an account action launches a dialog. Preserve ICommand bindings so CanExecute still controls the AppBarButtons.
    // This is a temporary workaround for the lack of a FlyoutManager in WinUI 3. We'll need to manage the closing event like this manually:

    /*
    private bool preserveAccountFlyouts;

    private void OnFlyoutClosing(FlyoutBase sender, FlyoutBaseClosingEventArgs args)
    {
        if (preserveAccountFlyouts)
        {
            args.Cancel = true;
        }
    }
    */
    private void DismissAccountFlyouts(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        MiHoYoLoginFlyout.Hide();
        HoYoLabLoginFlyout.Hide();
        AccountFlyout.Hide();
    }

    public string GetUserNickname(User? user) => user?.UserInfo?.Nickname ?? SH.ViewUserNoUserHint;

    public event PropertyChangedEventHandler? PropertyChanged;

    public UserViewModel ViewModel => DataContext;

    public new UserViewModel DataContext { get => Unsafe.As<UserViewModel>(base.DataContext); set => base.DataContext = value; }
}
