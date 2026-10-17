// Copyright (c) Millennium-Science-Technology-R-D-Inst. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.UI.Xaml.Control.Theme;

/// <summary>Creates a fresh resource dictionary for each owning resource root.</summary>
public sealed class PageResourceScope<TDictionary>(Func<TDictionary> create, Action<TDictionary, TDictionary> attach)
    where TDictionary : class
{
    private TDictionary? owner;

    public void EnsureAttached(TDictionary currentOwner)
    {
        ArgumentNullException.ThrowIfNull(currentOwner);
        if (ReferenceEquals(owner, currentOwner)) return;

        TDictionary? previousOwner = owner;
        // Resource callbacks may re-enter page initialization. Mark this owner
        // before attaching, but allow a later retry if initialization fails.
        owner = currentOwner;
        try
        {
            attach(currentOwner, create());
        }
        catch
        {
            owner = previousOwner;
            throw;
        }
    }
}
