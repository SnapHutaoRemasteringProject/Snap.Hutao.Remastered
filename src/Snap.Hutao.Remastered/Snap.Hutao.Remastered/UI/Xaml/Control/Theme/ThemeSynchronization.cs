// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml;

namespace Snap.Hutao.Remastered.UI.Xaml.Control.Theme;

/// <summary>
/// Keeps an inactive SwitchPresenter branch in the live presenter's theme.
/// ActualTheme's dependency property does not notify x:Bind in WinUI (#10390).
/// </summary>
public sealed partial class ThemeSynchronization : DependencyObject
{
    public static readonly DependencyProperty SourceProperty = DependencyProperty.RegisterAttached(
        "Source", typeof(FrameworkElement), typeof(ThemeSynchronization), new PropertyMetadata(null, OnSourceChanged));

    private static readonly DependencyProperty SubscriptionProperty = DependencyProperty.RegisterAttached(
        "Subscription", typeof(object), typeof(ThemeSynchronization), new PropertyMetadata(null));

    public static FrameworkElement? GetSource(DependencyObject target) => (FrameworkElement?)target.GetValue(SourceProperty);

    public static void SetSource(DependencyObject target, FrameworkElement? value) => target.SetValue(SourceProperty, value);

    private static void OnSourceChanged(DependencyObject target, DependencyPropertyChangedEventArgs args)
    {
        if (target is not FrameworkElement element) return;
        (element.GetValue(SubscriptionProperty) as Subscription)?.Dispose();
        element.ClearValue(SubscriptionProperty);
        if (args.NewValue is FrameworkElement source)
        {
            element.SetValue(SubscriptionProperty, new Subscription(source, element));
        }
    }

    private sealed class Subscription : IDisposable
    {
        private readonly FrameworkElement source;
        private readonly WeakReference<FrameworkElement> target;

        public Subscription(FrameworkElement source, FrameworkElement target)
        {
            this.source = source;
            this.target = new(target);
            source.ActualThemeChanged += OnActualThemeChanged;
            source.Loaded += OnLoaded;
            Synchronize();
        }

        public void Dispose()
        {
            source.ActualThemeChanged -= OnActualThemeChanged;
            source.Loaded -= OnLoaded;
        }

        private void OnActualThemeChanged(FrameworkElement sender, object args) => Synchronize();

        private void OnLoaded(object sender, RoutedEventArgs args) => Synchronize();

        private void Synchronize()
        {
            if (XamlApplicationLifetime.Exiting) return;
            if (target.TryGetTarget(out FrameworkElement? element))
            {
                element.RequestedTheme = source.ActualTheme;
            }
            else
            {
                Dispose();
            }
        }
    }
}
