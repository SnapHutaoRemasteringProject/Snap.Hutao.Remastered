// Copyright (c) 2026 DGP Studio. All rights reserved.
// Licensed under the MIT license.
// Adapted from Snap.Nicole.

using Snap.Hutao.Remastered.Core.AI.ComponentModel;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace Snap.Hutao.Remastered.Core.AI.Collections.ObjectModel;

internal sealed class AdvancedObservableCollection<T> : ObservableCollection<T>
    where T : class
{
    private T? currentItem;

    public T? CurrentItem
    {
        get => currentItem;
        set => SetCurrentItem(value);
    }

    public void MoveCurrentToFirst()
    {
        CurrentItem = Count is 0 ? null : this[0];
    }

    protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        if (currentItem is not null && !Contains(currentItem))
        {
            SetCurrentItem(null);
        }

        base.OnCollectionChanged(e);
    }

    private void SetCurrentItem(T? value)
    {
        T? coercedValue = value is not null && Contains(value) ? value : null;
        if (ReferenceEquals(currentItem, coercedValue))
        {
            return;
        }

        currentItem = coercedValue;
        OnPropertyChanged(PropertyChangedEventArgs.CurrentItem);
    }
}
