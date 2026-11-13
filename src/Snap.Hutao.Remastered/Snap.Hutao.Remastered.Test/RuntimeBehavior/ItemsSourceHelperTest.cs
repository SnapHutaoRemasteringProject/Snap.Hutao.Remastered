// Copyright (c) Snap Hutao RP. All rights reserved.
// Licensed under the MIT license.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Snap.Hutao.Remastered.UI.Xaml;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Snap.Hutao.Remastered.Test.RuntimeBehavior;

[TestClass]
public sealed class ItemsSourceHelperTest
{
    [TestMethod]
    public void DefaultAndEmptyImmutableArraysAreSafeDuringInitialBinding()
    {
        ImmutableArray<string> uninitialized = default;
        ImmutableArray<string> empty = [];

        Assert.AreEqual(0, ItemsSourceHelper.Create(uninitialized, uninitialized.IsDefaultOrEmpty)!.Count);
        Assert.AreEqual(0, ItemsSourceHelper.Create(empty, empty.IsDefaultOrEmpty)!.Count);
    }

    [TestMethod]
    public void SnapshotPreservesItemIdentityNullsAndOrderWithoutChangingSource()
    {
        object first = new();
        object last = new();
        ImmutableArray<object?> source = [first, null, last];

        List<object?> items = ItemsSourceHelper.Create(source, source.IsDefaultOrEmpty)!;
        Assert.AreSame(first, items[0]);
        Assert.IsNull(items[1]);
        Assert.AreSame(last, items[2]);

        items.RemoveAt(0);
        Assert.AreEqual(3, source.Length);
        Assert.AreSame(first, source[0]);
    }

    [TestMethod]
    public void ValueItemsRemainBoxedValuesInAReferenceTypeCollection()
    {
        ImmutableArray<bool> source = [true, false];
        List<object?> items = ItemsSourceHelper.Create(source, source.IsDefaultOrEmpty)!;

        CollectionAssert.AreEqual(new object[] { true, false }, items);
    }

    [TestMethod]
    public void MissingNullableSourceRemainsNull()
    {
        ImmutableArray<string>? source = null;

        Assert.IsNull(ItemsSourceHelper.Create(source, false));
    }
}
