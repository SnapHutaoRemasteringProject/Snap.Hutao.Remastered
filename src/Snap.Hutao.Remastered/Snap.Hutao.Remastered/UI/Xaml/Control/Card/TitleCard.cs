// Copyright (c) Millennium-Science-Technology-R-D-Inst. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Markup;

namespace Snap.Hutao.Remastered.UI.Xaml.Control.Card;

[ContentProperty(Name = nameof(Content))]
internal sealed partial class TitleCard : Microsoft.UI.Xaml.Controls.Control
{
    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(string), typeof(TitleCard), new PropertyMetadata(default(string), OnVisualPropertyChanged));

    public static readonly DependencyProperty TitleContentProperty =
        DependencyProperty.Register(nameof(TitleContent), typeof(object), typeof(TitleCard), new PropertyMetadata(default(object), OnVisualPropertyChanged));

    public static readonly DependencyProperty ContentProperty =
        DependencyProperty.Register(nameof(Content), typeof(object), typeof(TitleCard), new PropertyMetadata(default(object)));

    public static readonly DependencyProperty DividerVisibilityProperty =
        DependencyProperty.Register(nameof(DividerVisibility), typeof(Visibility), typeof(TitleCard), new PropertyMetadata(Visibility.Visible));

    public TitleCard()
    {
        DefaultStyleKey = typeof(TitleCard);
    }

    public string? Title
    {
        get => (string?)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public object? TitleContent
    {
        get => (object?)GetValue(TitleContentProperty);
        set => SetValue(TitleContentProperty, value);
    }

    public object? Content
    {
        get => (object?)GetValue(ContentProperty);
        set => SetValue(ContentProperty, value);
    }

    public Visibility DividerVisibility
    {
        get => (Visibility)GetValue(DividerVisibilityProperty);
        set => SetValue(DividerVisibilityProperty, value);
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        SetVisualStates();
    }

    private static void OnVisualPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TitleCard card)
        {
            card.SetVisualStates();
        }
    }

    private void SetVisualStates()
    {
        if (string.IsNullOrEmpty(Title) && TitleContent is null)
        {
            VisualStateManager.GoToState(this, "TitleGridCollapsed", true);
            DividerVisibility = Visibility.Collapsed;
        }
        else
        {
            VisualStateManager.GoToState(this, "TitleGridVisible", true);
            DividerVisibility = Visibility.Visible;
        }
    }
}
