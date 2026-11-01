// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml.Controls;
using Snap.Hutao.Remastered.ViewModel.User;
using User = Snap.Hutao.Remastered.ViewModel.User.User;

namespace Snap.Hutao.Remastered.UI.Xaml.View;

public sealed partial class UserView : UserControl
{
    public UserView()
    {
        InitializeComponent();
    }

    public string GetUserNickname(User? user) => user?.UserInfo?.Nickname ?? SH.ViewUserNoUserHint;

    public new UserViewModel DataContext { get => (UserViewModel)base.DataContext; set => base.DataContext = value; }
}
