// Copyright (c) 2026 DGP Studio. All rights reserved.
// Licensed under the MIT license.
// Adapted from Snap.Nicole.

using Snap.Hutao.Remastered.Core.AI;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

namespace Snap.Hutao.Remastered.Service.AI.Settings;

internal sealed class ObservableSettingsCollection<TItem, TId> : ObservableCollection<TItem>, ICopyFrom<ObservableSettingsCollection<TItem, TId>>, IObservableChildrenProvider
    where TItem : class, INotifyPropertyChanged, IIdentifiable<TId>, ICopyFrom<TItem>, new()
    where TId : struct, IEquatable<TId>
{
    private readonly HashSet<TItem> subscribedItems = [];
    private TItem? currentItem;
    private TId? currentItemId;

    public TItem? CurrentItem
    {
        get => CoerceCurrentItem();
        set => SetCurrentItem(value);
    }

    public TId? CurrentItemId
    {
        get => currentItemId;
        set => SetCurrentItemId(value);
    }

    public void MoveCurrentTo(TId id)
    {
        CurrentItemId = id;
    }

    public void MoveCurrentToFirst()
    {
        CurrentItem = this.FirstOrDefault();
    }

    public void CopyFrom(ObservableSettingsCollection<TItem, TId> source)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (ReferenceEquals(this, source))
        {
            return;
        }

        Dictionary<TId, TItem> existingMap = [];
        foreach (TItem item in this)
        {
            existingMap.TryAdd(item.Id, item);
        }

        HashSet<TId> visitedIds = [];
        HashSet<TItem> retainedItems = [];

        int targetIndex = 0;
        foreach (TItem sourceItem in source)
        {
            TId id = sourceItem.Id;
            if (!visitedIds.Add(id))
            {
                continue;
            }

            if (existingMap.TryGetValue(id, out TItem? existingItem))
            {
                int currentIndex = IndexOf(existingItem);
                if (currentIndex != targetIndex)
                {
                    Move(currentIndex, targetIndex);
                }

                existingItem.CopyFrom(sourceItem);
                retainedItems.Add(existingItem);
            }
            else
            {
                TItem newItem = new();
                newItem.CopyFrom(sourceItem);
                Insert(targetIndex, newItem);
                retainedItems.Add(newItem);
            }

            targetIndex++;
        }

        for (int i = Count - 1; i >= 0; i--)
        {
            if (!retainedItems.Contains(this[i]))
            {
                RemoveAt(i);
            }
        }

        EnsureCurrentItem();
    }

    public IEnumerable<INotifyPropertyChanged> EnumerateObservableChildren()
    {
        yield return this;

        foreach (TItem item in this)
        {
            if (item is not IObservableChildrenProvider provider)
            {
                continue;
            }

            foreach (INotifyPropertyChanged source in provider.EnumerateObservableChildren())
            {
                yield return source;
            }
        }
    }

    protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        SyncItemSubscriptions();
        EnsureCurrentItem();
        base.OnCollectionChanged(e);
    }

    private void OnItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));

        if (ReferenceEquals(sender, CurrentItem))
        {
            OnPropertyChanged(new PropertyChangedEventArgs(nameof(CurrentItem)));
        }
    }

    private void SyncItemSubscriptions()
    {
        HashSet<TItem> currentItems = [.. this];

        foreach (TItem item in subscribedItems.ToArray())
        {
            if (!currentItems.Contains(item))
            {
                UnsubscribeItem(item);
            }
        }

        foreach (TItem item in this)
        {
            SubscribeItem(item);
        }
    }

    private void SubscribeItem(TItem item)
    {
        if (subscribedItems.Add(item))
        {
            item.PropertyChanged += OnItemPropertyChanged;
        }
    }

    private void UnsubscribeItem(TItem item)
    {
        if (subscribedItems.Remove(item))
        {
            item.PropertyChanged -= OnItemPropertyChanged;
        }
    }

    private void EnsureCurrentItem()
    {
        SetCurrentItemCore(CoerceCurrentItem());
    }

    private void SetCurrentItem(TItem? value)
    {
        SetCurrentItemCore(CoerceCurrentItem(value));
    }

    private void SetCurrentItemId(TId? value)
    {
        if (!value.HasValue)
        {
            SetCurrentItemCore(null, null);
            return;
        }

        TItem? item = CoerceCurrentItem(value.Value);
        SetCurrentItemCore(item, item?.Id ?? value);
    }

    private void SetCurrentItemCore(TItem? value)
    {
        SetCurrentItemCore(value, value?.Id);
    }

    private void SetCurrentItemCore(TItem? value, TId? id)
    {
        TItem? oldItem = currentItem;
        TId? oldId = currentItemId;

        currentItem = value;
        currentItemId = id;

        if (!ReferenceEquals(oldItem, currentItem))
        {
            OnPropertyChanged(new PropertyChangedEventArgs(nameof(CurrentItem)));
        }

        if (!IdEquals(oldId, currentItemId))
        {
            OnPropertyChanged(new PropertyChangedEventArgs(nameof(CurrentItemId)));
        }
    }

    private TItem? CoerceCurrentItem(TItem? value)
    {
        if (value is null)
        {
            return null;
        }

        if (Contains(value))
        {
            return value;
        }

        return this.FirstOrDefault();
    }

    private TItem? CoerceCurrentItem()
    {
        return currentItemId.HasValue ? CoerceCurrentItem(currentItemId.Value) : CoerceCurrentItem(currentItem);
    }

    private TItem? CoerceCurrentItem(TId id)
    {
        TItem? item = this.FirstOrDefault(item => EqualityComparer<TId>.Default.Equals(item.Id, id));
        if (item is not null)
        {
            return item;
        }

        return this.FirstOrDefault();
    }

    private static bool IdEquals(TId? x, TId? y)
    {
        return EqualityComparer<TId?>.Default.Equals(x, y);
    }
}
