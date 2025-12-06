// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.Model;

internal interface IEntityAccessWithMetadata<out TEntity, out TMetadata> : IEntityAccess<TEntity>
{
    TMetadata Inner { get; }
}