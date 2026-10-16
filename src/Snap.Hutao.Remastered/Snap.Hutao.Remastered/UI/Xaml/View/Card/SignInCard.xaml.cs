// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.UI.Xaml.Control.Theme;
using Snap.Hutao.Remastered.ViewModel.Abstraction;
using Snap.Hutao.Remastered.ViewModel.Sign;

namespace Snap.Hutao.Remastered.UI.Xaml.View.Card;

public sealed partial class SignInCard : MaterialCardButton
{
    public SignInCard(IServiceProvider serviceProvider)
    {
        InitializeComponent();
        this.InitializeViewModelSlim<SignInViewModel>(serviceProvider);
        this.DataContext<SignInViewModel>()?.AttachXamlElement(AwardScrollViewer);
    }
}
