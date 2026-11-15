// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.WinUI.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Markup;
using Microsoft.Xaml.Interactivity;
using WinUI.LiquidGlass;

namespace Snap.Hutao.Remastered.UI.Xaml.Behavior;

/// <summary>Subscribes to the application's known UI events without event reflection.</summary>
[ContentProperty(Name = nameof(Actions))]
public sealed partial class StaticEventTriggerBehavior : Microsoft.Xaml.Interactivity.Behavior<FrameworkElement>
{
    public static readonly DependencyProperty EventNameProperty = DependencyProperty.Register(
        nameof(EventName), typeof(string), typeof(StaticEventTriggerBehavior), new PropertyMetadata(string.Empty, OnEventNameChanged));
    public static readonly DependencyProperty ActionsProperty = DependencyProperty.Register(
        nameof(Actions), typeof(ActionCollection), typeof(StaticEventTriggerBehavior), new PropertyMetadata(null));

    private FrameworkElement? source;
    private string subscribedEvent = string.Empty;

    public string EventName
    {
        get => (string)GetValue(EventNameProperty);
        set => SetValue(EventNameProperty, value);
    }

    public ActionCollection Actions
    {
        get
        {
            if (GetValue(ActionsProperty) is ActionCollection actions) return actions;
            actions = new ActionCollection();
            SetValue(ActionsProperty, actions);
            return actions;
        }
    }

    protected override void OnAttached()
    {
        base.OnAttached();
        RefreshSubscription();
    }

    protected override void OnDetaching()
    {
        Unsubscribe();
        base.OnDetaching();
    }

    private static void OnEventNameChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        ((StaticEventTriggerBehavior)sender).RefreshSubscription();
    }

    private void RefreshSubscription()
    {
        Unsubscribe();
        if (AssociatedObject is not { } target || string.IsNullOrEmpty(EventName)) return;
        ChangeSubscription(target, EventName, true);
        source = target;
        subscribedEvent = EventName;
    }

    private void Unsubscribe()
    {
        if (source is not { } target) return;
        ChangeSubscription(target, subscribedEvent, false);
        source = null;
        subscribedEvent = string.Empty;
    }

    private void OnEvent(object? sender, object args)
    {
        if (source is { } target) Interaction.ExecuteActions(target, Actions, args);
    }

    [WinRT.DynamicWindowsRuntimeCast(typeof(global::Microsoft.UI.Xaml.Controls.Primitives.ButtonBase))]
    [WinRT.DynamicWindowsRuntimeCast(typeof(global::Microsoft.UI.Xaml.Controls.AutoSuggestBox))]
    [WinRT.DynamicWindowsRuntimeCast(typeof(global::WinUI.LiquidGlass.LiquidGlassSearchBox))]
    private void ChangeSubscription(FrameworkElement target, string eventName, bool subscribe)
    {
        switch (eventName)
        {
            case "Click" when target is SettingsCard card:
                if (subscribe) card.Click += OnEvent; else card.Click -= OnEvent;
                break;
            case "Click" when target is ButtonBase button:
                if (subscribe) button.Click += OnEvent; else button.Click -= OnEvent;
                break;
            case "QuerySubmitted" when target is LiquidGlassSearchBox glassSearch:
                if (subscribe) glassSearch.QuerySubmitted += OnEvent; else glassSearch.QuerySubmitted -= OnEvent;
                break;
            case "QuerySubmitted" when target is AutoSuggestBox autoSuggest:
                if (subscribe) autoSuggest.QuerySubmitted += OnEvent; else autoSuggest.QuerySubmitted -= OnEvent;
                break;
            case "Tapped":
                if (subscribe) target.Tapped += OnEvent; else target.Tapped -= OnEvent;
                break;
            case "PointerEntered":
                if (subscribe) target.PointerEntered += OnEvent; else target.PointerEntered -= OnEvent;
                break;
            case "PointerExited":
                if (subscribe) target.PointerExited += OnEvent; else target.PointerExited -= OnEvent;
                break;
            case "PointerPressed":
                if (subscribe) target.PointerPressed += OnEvent; else target.PointerPressed -= OnEvent;
                break;
            default:
                throw new NotSupportedException($"Unsupported UI event '{eventName}' on {target.GetType().FullName}.");
        }
    }
}
