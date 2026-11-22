// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Extensions.Caching.Memory;
using Snap.Hutao.Remastered.Service.Metadata;

namespace Snap.Hutao.Remastered.Test.Metadata;

[TestClass]
public sealed class MetadataCacheTest
{
    [TestMethod]
    public void KeyCarriesSourceCategoryAndShape()
    {
        using MemoryCache memoryCache = new(new MemoryCacheOptions());
        MetadataCache cache = new(memoryCache, "Metadata.Proto.CHS");

        string key = cache.CreateKey(MetadataCategories.Achievement, "Map", "MaterialId", "DisplayItem");

        Assert.AreEqual("Metadata.Proto.CHS.Achievement.Map.MaterialId.DisplayItem", key);
    }

    [TestMethod]
    public void SetIfCurrentStoresThenClearRemoves()
    {
        using MemoryCache memoryCache = new(new MemoryCacheOptions());
        MetadataCache cache = new(memoryCache, "Metadata.Proto.CHS");
        string key = cache.CreateKey(MetadataCategories.Weapon, "Array");

        int generation = cache.Generation;
        cache.SetIfCurrent(generation, key, 42);

        Assert.IsTrue(cache.TryGet(key, out object? value));
        Assert.AreEqual(42, value);

        int removed = cache.Clear();

        Assert.AreEqual(1, removed);
        Assert.IsFalse(cache.TryGet(key, out _));
        Assert.AreNotEqual(generation, cache.Generation);
    }

    [TestMethod]
    public void SetIfCurrentDropsResultFromPreviousGeneration()
    {
        using MemoryCache memoryCache = new(new MemoryCacheOptions());
        MetadataCache cache = new(memoryCache, "Metadata.Proto.CHS");
        string key = cache.CreateKey(MetadataCategories.Weapon, "Array");

        int generation = cache.Generation;
        cache.Clear();
        cache.SetIfCurrent(generation, key, 7);

        Assert.IsFalse(cache.TryGet(key, out _));
    }

    [TestMethod]
    public void ClearDoesNotRemoveOtherCaches()
    {
        using MemoryCache memoryCache = new(new MemoryCacheOptions());
        memoryCache.Set("AnnouncementService.Cache", 1);

        MetadataCache cache = new(memoryCache, "Metadata.Git.CHS");
        cache.SetIfCurrent(cache.Generation, cache.CreateKey(MetadataCategories.Material, "Array"), 2);

        cache.Clear();

        Assert.IsTrue(memoryCache.TryGetValue("AnnouncementService.Cache", out object? value));
        Assert.AreEqual(1, value);
    }

    [TestMethod]
    public async Task GetOrCreateReturnsCachedValueOnce()
    {
        using MemoryCache memoryCache = new(new MemoryCacheOptions());
        MetadataCache cache = new(memoryCache, "Metadata.Git.CHS");
        string key = cache.CreateKey(MetadataCategories.Chapter, "Array");
        int calls = 0;

        int first = await cache.GetOrCreateAsync(key, _ =>
        {
            calls++;
            return ValueTask.FromResult(1);
        }, CancellationToken.None);

        int second = await cache.GetOrCreateAsync(key, _ =>
        {
            calls++;
            return ValueTask.FromResult(2);
        }, CancellationToken.None);

        Assert.AreEqual(1, first);
        Assert.AreEqual(1, second);
        Assert.AreEqual(1, calls);
    }
}
