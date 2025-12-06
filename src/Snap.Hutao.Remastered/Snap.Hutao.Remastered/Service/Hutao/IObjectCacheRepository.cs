// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Model.Entity;
using Snap.Hutao.Remastered.Service.Abstraction;

namespace Snap.Hutao.Remastered.Service.Hutao;

internal interface IObjectCacheRepository : IRepository<ObjectCacheEntry>
{
    ValueTask AddObjectCacheAsync<T>(string key, TimeSpan expire, T data)
        where T : class;

    ValueTask<T?> GetObjectOrDefaultAsync<T>(string key)
        where T : class;
}