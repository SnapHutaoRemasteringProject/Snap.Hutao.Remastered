// Copyright (c) Snap Hutao RP. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Snap.Hutao.Remastered.UI.Xaml.Control.Theme;

public sealed partial class ComboBoxPopupLifetime : DependencyObject
{
    private static readonly HashSet<ComboBox> Controls = [];

    public static readonly DependencyProperty IsManagedProperty = DependencyProperty.RegisterAttached(
        "IsManaged", typeof(bool), typeof(ComboBoxPopupLifetime), new PropertyMetadata(false, OnIsManagedChanged));

    public static bool GetIsManaged(DependencyObject target) => (bool)target.GetValue(IsManagedProperty);

    public static void SetIsManaged(DependencyObject target, bool value) => target.SetValue(IsManagedProperty, value);

    public static void CloseAll()
    {
        foreach (ComboBox control in Controls.ToArray()) control.IsDropDownOpen = false;
    }

    private static void OnIsManagedChanged(DependencyObject target, DependencyPropertyChangedEventArgs args)
    {
        if (target is not ComboBox control) return;
        control.Loaded -= OnLoaded;
        control.Unloaded -= OnUnloaded;
        Controls.Remove(control);
        if ((bool)args.NewValue)
        {
            control.Loaded += OnLoaded;
            control.Unloaded += OnUnloaded;
            if (control.IsLoaded) Controls.Add(control);
        }
    }

    private static void OnLoaded(object sender, RoutedEventArgs args)
    {
        if (!XamlApplicationLifetime.Exiting) Controls.Add((ComboBox)sender);
    }

    private static void OnUnloaded(object sender, RoutedEventArgs args)
    {
        // Popup state belongs to this visual instance, never its inactive peer.
        ComboBox control = (ComboBox)sender;
        Controls.Remove(control);
        if (!XamlApplicationLifetime.Exiting) control.IsDropDownOpen = false;
    }
}
