// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using System.Runtime.CompilerServices;

namespace Snap.Hutao.Model.Metadata.Abstraction;

internal static class ItemConvertibleExtension
{
    private static readonly ConditionalWeakTable<IItemConvertible, Model.Item> Items = [];

    extension(IItemConvertible source)
    {
        public Model.Item GetOrCreateItem()
        {
            return Items.GetOrAdd(source, static value => value.ToItem<Model.Item>());
        }
    }
}