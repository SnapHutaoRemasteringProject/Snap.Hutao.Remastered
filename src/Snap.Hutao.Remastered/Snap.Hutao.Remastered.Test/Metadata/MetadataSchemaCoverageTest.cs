// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Service.Metadata;

namespace Snap.Hutao.Remastered.Test.Metadata;

[TestClass]
public sealed class MetadataSchemaCoverageTest
{
    [TestMethod]
    public void ApplicationCategoriesAndIgnoredCategoriesDoNotOverlap()
    {
        Assert.AreEqual(30, MetadataCategories.All.Length);
        Assert.AreEqual(30, MetadataCategories.All.Select(category => category.ProtoName).Distinct(StringComparer.Ordinal).Count());
        Assert.AreEqual(30, MetadataCategories.All.Select(category => category.Name).Distinct(StringComparer.Ordinal).Count());
        Assert.AreEqual(0, MetadataCategories.All.Count(category => MetadataCategories.IgnoredProtoCategories.Contains(category.ProtoName)));
    }

    [TestMethod]
    public void BydMaterialKeepsTheLegacyJsonFileName()
    {
        Assert.AreEqual("BeyondItem", MetadataCategories.BydMaterial.Name);
        Assert.AreEqual("BydMaterial", MetadataCategories.BydMaterial.ProtoName);
    }
}
