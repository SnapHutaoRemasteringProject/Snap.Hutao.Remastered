// Copyright (c) 2026 DGP Studio. All rights reserved.
// Licensed under the MIT license.
// Adapted from Snap.Nicole.

using System.Collections.Generic;
using System.ComponentModel;

namespace Snap.Hutao.Remastered.Service.AI.Settings;

internal sealed class ObservableObjectHierarchyObserver<T, TState> : IDisposable
    where T : class, INotifyPropertyChanged
{
    private readonly T root;
    private readonly List<INotifyPropertyChanged> observableChildren = [];

    private readonly TState state;
    private readonly Action<TState, T> handleChange;

    private bool suppressed;

    public ObservableObjectHierarchyObserver(T root, TState state, Action<TState, T> handleChange)
    {
        this.state = state;
        this.handleChange = handleChange;

        this.root = root;
        root.PropertyChanged += OnPropertyChanged;
        UpdateObservableChildren();
    }

    public ref bool Suppressed { get => ref suppressed; }

    public T Root { get => root; }

    public void Dispose()
    {
        root.PropertyChanged -= OnPropertyChanged;
        ClearObservableChildren();
    }

    public void Refresh()
    {
        UpdateObservableChildren();
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (Suppressed)
        {
            return;
        }

        UpdateObservableChildren();
        handleChange(state, root);
    }

    private void UpdateObservableChildren()
    {
        ClearObservableChildren();

        if (root is not IObservableChildrenProvider provider)
        {
            return;
        }

        foreach (INotifyPropertyChanged source in provider.EnumerateObservableChildren())
        {
            source.PropertyChanged += OnPropertyChanged;
            observableChildren.Add(source);
        }
    }

    private void ClearObservableChildren()
    {
        foreach (INotifyPropertyChanged source in observableChildren)
        {
            source.PropertyChanged -= OnPropertyChanged;
        }

        observableChildren.Clear();
    }
}
