// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;
using WinUIControl = Microsoft.UI.Xaml.Controls.Control;
using WinUIListView = Microsoft.UI.Xaml.Controls.ListView;

namespace Snap.Hutao.Remastered.UI.Xaml.Control.AutoSortBox;

[DependencyProperty<IReadOnlyList<AutoSortToken>>("AvailableTokens", PropertyChangedCallbackName = nameof(OnAvailableTokensChanged))]
[DependencyProperty<ICommand>("SortCommand")]
public sealed partial class AutoSortList : WinUIControl
{
    private ObservableCollection<AutoSortToken> innerItems = [];
    private WinUIListView? tokenListView;

    public AutoSortList()
    {
        DefaultStyleKey = typeof(AutoSortList);
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        if (tokenListView is not null)
        {
            tokenListView.ItemClick -= OnTokenItemClick;
            tokenListView.ContainerContentChanging -= OnContainerContentChanging;
        }

        if (GetTemplateChild("TokenListView") is WinUIListView listView)
        {
            tokenListView = listView;
            listView.ItemsSource = innerItems;
            listView.ItemClick += OnTokenItemClick;
            listView.ContainerContentChanging += OnContainerContentChanging;
        }
    }

    private void OnContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
    {
        if (args.Item is not AutoSortToken)
        {
            return;
        }

        if (args.InRecycleQueue)
        {
            if (args.ItemContainer.FindDescendant<Button>() is { } removeButton)
            {
                removeButton.Click -= OnRemoveButtonClick;
            }
        }
        else
        {
            if (args.ItemContainer.FindDescendant<Button>() is { } removeButton)
            {
                removeButton.Click += OnRemoveButtonClick;
            }
        }
    }

    private void OnRemoveButtonClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: AutoSortToken token })
        {
            RemoveSort(token);
        }
    }

    private static void OnAvailableTokensChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is AutoSortList list && e.NewValue is IReadOnlyList<AutoSortToken> tokens)
        {
            // Detach ItemsSource first so existing containers recycle and unhook button events
            if (list.tokenListView is not null)
            {
                list.tokenListView.ItemsSource = null;
            }

            list.innerItems = new ObservableCollection<AutoSortToken>(tokens);
            if (list.tokenListView is not null)
            {
                list.tokenListView.ItemsSource = list.innerItems;
            }
        }
    }

    private void OnTokenItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is not AutoSortToken clickedToken)
        {
            return;
        }

        if (clickedToken.IsSelected)
        {
            // Toggle sort direction
            clickedToken.IsDescending = !clickedToken.IsDescending;
        }
        else
        {
            // Assign next order number (last selected order + 1, or 1 if none selected)
            int nextOrder = innerItems.Where(t => t.IsSelected).Select(t => t.SortOrder).DefaultIfEmpty(0).Max() + 1;
            clickedToken.IsSelected = true;
            clickedToken.SortOrder = nextOrder;
        }

        SortCommand?.Execute(null);
    }

    [Command("RemoveSortCommand")]
    private void RemoveSort(AutoSortToken? token)
    {
        ArgumentNullException.ThrowIfNull(token);
        token.IsSelected = false;
        token.SortOrder = 0;
        token.IsDescending = true;

        // Re-number remaining selected tokens
        int order = 1;
        foreach (AutoSortToken t in innerItems)
        {
            if (t.IsSelected)
            {
                t.SortOrder = order;
                order++;
            }
        }

        SortCommand?.Execute(null);
    }
}
