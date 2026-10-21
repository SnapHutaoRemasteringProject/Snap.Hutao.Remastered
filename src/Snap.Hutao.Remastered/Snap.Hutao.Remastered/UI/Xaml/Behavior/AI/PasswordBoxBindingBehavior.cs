// Copyright (c) 2026 DGP Studio. All rights reserved.
// Licensed under the MIT license.
// Adapted from Snap.Nicole.

using CommunityToolkit.WinUI.Behaviors;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Snap.Hutao.Remastered.Core.AI;

namespace Snap.Hutao.Remastered.UI.Xaml.Behavior.AI;

[DependencyProperty<string>("Password", PropertyChangedCallbackName = nameof(OnPasswordChanged))]
internal sealed partial class PasswordBoxBindingBehavior : BehaviorBase<PasswordBox>
{
    private bool isUpdatingPasswordBox;
    private bool isUpdatingPassword;

    protected override bool Initialize()
    {
        if (!base.Initialize())
        {
            return false;
        }

        AssociatedObject.PasswordChanged += OnPasswordBoxPasswordChanged;
        UpdatePasswordBox();
        return true;
    }

    protected override void OnAssociatedObjectLoaded()
    {
        UpdatePasswordBox();
    }

    protected override bool Uninitialize()
    {
        AssociatedObject.PasswordChanged -= OnPasswordBoxPasswordChanged;
        return base.Uninitialize();
    }

    private static void OnPasswordChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not PasswordBoxBindingBehavior behavior)
        {
            return;
        }

        behavior.UpdatePasswordBox();
    }

    private void OnPasswordBoxPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (isUpdatingPasswordBox)
        {
            return;
        }

        using (BooleanTrueScope.Create(ref isUpdatingPassword))
        {
            Password = AssociatedObject.Password;
        }
    }

    private void UpdatePasswordBox()
    {
        if (AssociatedObject is null)
        {
            return;
        }

        if (isUpdatingPassword)
        {
            return;
        }

        using (BooleanTrueScope.Create(ref isUpdatingPasswordBox))
        {
            string newPassword = Password ?? string.Empty;
            if (!string.Equals(AssociatedObject.Password, newPassword))
            {
                AssociatedObject.Password = newPassword;
            }
        }
    }
}
