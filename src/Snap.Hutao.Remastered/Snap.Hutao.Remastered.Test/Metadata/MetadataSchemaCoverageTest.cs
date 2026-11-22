// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Google.Protobuf.Reflection;
using Snap.Hutao.Remastered.Metadata;
using Snap.Hutao.Remastered.Service.Metadata;

namespace Snap.Hutao.Remastered.Test.Metadata;

[TestClass]
public sealed class MetadataSchemaCoverageTest
{
    [TestMethod]
    public void EveryLanguageMetadataFieldIsConsumedOrExplicitlyIgnored()
    {
        HashSet<string> schema = [.. GetSchemaCategoryNames()];
        HashSet<string> consumed = [.. MetadataCategories.All.Select(category => category.ProtoName)];
        HashSet<string> ignored = [.. MetadataCategories.IgnoredProtoCategories];

        HashSet<string> covered = [.. consumed, .. ignored];

        Assert.AreNotEqual(0, schema.Count, "LanguageMetadata has no category fields.");
        Assert.IsTrue(schema.SetEquals(covered), $"Schema=[{string.Join(',', schema.Except(covered))}], registered=[{string.Join(',', covered.Except(schema))}]");
        Assert.AreEqual(0, consumed.Count(name => ignored.Contains(name)), "A category cannot be both consumed and ignored.");
    }

    [TestMethod]
    public void ApplicationCategoriesAndIgnoredCategoriesDoNotOverlap()
    {
        Assert.AreEqual(30, MetadataCategories.All.Length);
        Assert.AreEqual(30, MetadataCategories.All.Select(category => category.ProtoName).Distinct(StringComparer.Ordinal).Count());
        Assert.AreEqual(30, MetadataCategories.All.Select(category => category.Name).Distinct(StringComparer.Ordinal).Count());
    }

    [TestMethod]
    public void BydMaterialKeepsTheLegacyJsonFileName()
    {
        Assert.AreEqual("BeyondItem", MetadataCategories.BydMaterial.Name);
        Assert.AreEqual("BydMaterial", MetadataCategories.BydMaterial.ProtoName);
    }

    private static IEnumerable<string> GetSchemaCategoryNames()
    {
        return LanguageMetadata.Descriptor.Fields
            .InDeclarationOrder()
            .Where(field => field.FieldType is FieldType.Message)
            .Select(field => field.MessageType.Name);
    }
}
