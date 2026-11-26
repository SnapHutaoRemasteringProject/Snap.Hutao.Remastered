// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.WinUI.Behaviors;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Snap.Hutao.Remastered.Core;
using Snap.Hutao.Remastered.Core.Logging;
using Snap.Hutao.Remastered.Core.Setting;
using Snap.Hutao.Remastered.Service.Navigation;
using Snap.Hutao.Remastered.Service.Navigation.Message;
using Snap.Hutao.Remastered.Service.Plugin;
using Snap.Hutao.Remastered.UI.Content;
using Snap.Hutao.Remastered.UI.Xaml.Control;
using Snap.Hutao.Remastered.UI.Xaml.View.Page;
using WinRT;

namespace Snap.Hutao.Remastered.UI.Xaml.Behavior;

public sealed class ServiceRecipientNavigationViewBehavior : BehaviorBase<NavigationView>,
    IRecipient<NavigationNavigateMessage>,
    IRecipient<NavigationGoBackMessage>,
    IRecipient<NavigationPaneToggleMessage>
{
    private IMessenger? messenger;
    private Frame? contentFrame;
    private PluginService? plugins;
    private readonly List<object> pluginItems = [];

    public void Receive(NavigationNavigateMessage message)
    {
        if (AssociatedObject is not { } navigationView)
        {
            message.Result = NavigationResult.Failed;
            return;
        }

        message.Result = Navigate(navigationView, message.PageType, message.Data, message.SyncNavigationViewItem);
    }

    public void Receive(NavigationGoBackMessage message)
    {
        if (AssociatedObject is not { } navigationView || navigationView.Content.As<Frame>() is not { } frame)
        {
            message.Result = false;
            return;
        }

        if (frame.CanGoBack)
        {
            frame.GoBack();
            message.Result = SyncSelectedNavigationViewItem(navigationView, frame.Content?.GetType(), (frame.Content as PluginHostPage)?.Target);
        }
    }

    public void Receive(NavigationPaneToggleMessage message)
    {
        if (AssociatedObject is not { } navigationView)
        {
            return;
        }

        navigationView.IsPaneOpen = !navigationView.IsPaneOpen;
    }

    protected override void OnAssociatedObjectLoaded()
    {
        AssociatedObject.IsPaneOpen = LocalSetting.Get(SettingKeys.IsNavPaneOpen, true);

        if (AssociatedObject.XamlRoot.XamlContext()?.ServiceProvider is { } serviceProvider)
        {
            messenger = serviceProvider.GetRequiredService<IMessenger>();
            messenger.Register<NavigationNavigateMessage>(this);
            messenger.Register<NavigationGoBackMessage>(this);
            messenger.Register<NavigationPaneToggleMessage>(this);
            plugins = serviceProvider.GetRequiredService<PluginService>();
            plugins.AttachRoot(AssociatedObject.XamlRoot);
            plugins.Changed += RefreshPluginNavigation;
        }

        AssociatedObject.ItemInvoked += OnItemInvoked;
        AssociatedObject.PaneOpened += OnPaneStateChanged;
        AssociatedObject.PaneClosed += OnPaneStateChanged;

        contentFrame = AssociatedObject.Content.As<Frame>();
            contentFrame.Navigated += OnFrameNavigated;

        Navigate(AssociatedObject, typeof(AnnouncementPage), NavigationExtraData.Default, true);
        RefreshPluginNavigation();
        plugins?.StartAsync().SafeForget();
    }

    protected override bool Uninitialize()
    {
        if (contentFrame is not null)
        {
            contentFrame.Navigated -= OnFrameNavigated;
            contentFrame = null;
        }

        if (plugins is not null) plugins.Changed -= RefreshPluginNavigation;
        foreach (object item in pluginItems) AssociatedObject.MenuItems.Remove(item);
        pluginItems.Clear();
        plugins = null;
        AssociatedObject.ItemInvoked -= OnItemInvoked;
        messenger?.UnregisterAll(this);
        AssociatedObject.PaneOpened -= OnPaneStateChanged;
        AssociatedObject.PaneClosed -= OnPaneStateChanged;
        return base.Uninitialize();
    }

    private void OnFrameNavigated(object sender, NavigationEventArgs e)
    {
        SyncSelectedNavigationViewItem(AssociatedObject, e.Content?.GetType(), (e.Parameter as INavigationExtraData)?.Data as PluginNavigationTarget);
    }

    private void RefreshPluginNavigation()
    {
        if (plugins is null || AssociatedObject is not { } navigationView) return;
        foreach (object item in pluginItems) navigationView.MenuItems.Remove(item);
        pluginItems.Clear();
        IReadOnlyList<PluginNavigationItem> contributions = plugins.Navigation;
        if (contributions.Count != 0)
        {
            NavigationViewItemHeader header = new() { Content = SH.ViewPluginHeader };
            navigationView.MenuItems.Add(header);
            pluginItems.Add(header);
        }
        foreach (PluginNavigationItem contribution in contributions)
        {
            NavigationViewItem item = new()
            {
                Content = contribution.Title,
                Icon = new FontIcon { Glyph = string.IsNullOrEmpty(contribution.IconGlyph) ? "\uE74C" : contribution.IconGlyph },
            };
            NavigationViewItemHelper.SetNavigateTo(item, typeof(PluginHostPage));
            NavigationViewItemHelper.SetExtraData(item, contribution.Target);
            navigationView.MenuItems.Add(item);
            pluginItems.Add(item);
        }
        if (contentFrame?.Content is PluginHostPage host)
        {
            if (!SyncSelectedNavigationViewItem(navigationView, typeof(PluginHostPage), host.Target))
                Navigate(navigationView, typeof(PluginPage), NavigationExtraData.Default, true);
        }
    }

    private static IEnumerable<NavigationViewItem> EnumerateMenuItems(IList<object> items)
    {
        foreach (NavigationViewItem item in items.OfType<NavigationViewItem>())
        {
            yield return item;

            if (item.MenuItems.Count > 0)
            {
                foreach (NavigationViewItem subItem in EnumerateMenuItems(item.MenuItems))
                {
                    yield return subItem;
                }
            }
        }
    }

    private static void OnPaneStateChanged(NavigationView sender, object args)
    {
        ArgumentNullException.ThrowIfNull(sender);
        LocalSetting.Set(SettingKeys.IsNavPaneOpen, sender.IsPaneOpen);
    }

    [WinRT.DynamicWindowsRuntimeCast(typeof(global::Microsoft.UI.Xaml.Controls.NavigationViewItem))]
    private static void OnItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        if (args.InvokedItemContainer is not NavigationViewItem item)
        {
            return;
        }

        Type? targetType = args.IsSettingsInvoked
            ? typeof(SettingPage)
            : NavigationViewItemHelper.GetNavigateTo(item);

        // Ignore item that doesn't have nav type specified
        if (targetType is not null)
        {
            INavigationCompletionSource data = new NavigationExtraData(NavigationViewItemHelper.GetExtraData(item));
            Navigate(sender, targetType, data, syncNavigationViewItem: false); // Because we are already invoking the item
        }
    }

    private static NavigationResult Navigate(NavigationView navigationView, Type pageType, INavigationCompletionSource data, bool syncNavigationViewItem = false)
    {
        if (navigationView.Content.As<Frame>() is not { } frame)
        {
            return NavigationResult.Failed;
        }

        Page? currentPage = frame.Content?.As<Page>();
        Type? currentPageType = currentPage?.GetType();

        SentrySdk.AddBreadcrumb(BreadcrumbFactory.CreateNavigation(
            currentPageType is null ? "Empty" : TypeNameHelper.GetTypeDisplayName(currentPageType, fullName: false),
            TypeNameHelper.GetTypeDisplayName(pageType, fullName: false),
            "Navigation"));

        if (currentPageType == pageType && pageType != typeof(PluginHostPage) && pageType != typeof(PluginSettingPage))
        {
            CancellationToken token = currentPage is ScopedPage scopedPage ? scopedPage.CancellationToken : CancellationToken.None;
            NavigationExtraDataSupport.NotifyRecipientAsync(frame.Content, data, token).SafeForget();
            return NavigationResult.AlreadyNavigatedTo;
        }

        _ = syncNavigationViewItem && SyncSelectedNavigationViewItem(navigationView, pageType, (data as INavigationExtraData)?.Data as PluginNavigationTarget);

        bool navigated = false;
        try
        {
            navigated = data is ISupportNavigationTransitionInfo { TransitionInfo: { } transitionInfo }
                ? frame.Navigate(pageType, data, transitionInfo)
                : frame.Navigate(pageType, data);
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
        }

        return navigated ? NavigationResult.Succeed : NavigationResult.Failed;
    }

    private static bool SyncSelectedNavigationViewItem(NavigationView navigationView, Type? pageType, PluginNavigationTarget? pluginTarget = null)
    {
        if (pageType is null)
        {
            return false;
        }

        if (pageType == typeof(SettingPage))
        {
            navigationView.SelectedItem = navigationView.SettingsItem;
            return true;
        }

        NavigationViewItem? target = EnumerateMenuItems(navigationView.MenuItems)
            .FirstOrDefault(menuItem => NavigationViewItemHelper.GetNavigateTo(menuItem) == pageType &&
                (pageType != typeof(PluginHostPage) || Equals(NavigationViewItemHelper.GetExtraData(menuItem), pluginTarget)));

        if (target is null)
        {
            return false;
        }

        navigationView.SelectedItem = target;
        return true;
    }
}
