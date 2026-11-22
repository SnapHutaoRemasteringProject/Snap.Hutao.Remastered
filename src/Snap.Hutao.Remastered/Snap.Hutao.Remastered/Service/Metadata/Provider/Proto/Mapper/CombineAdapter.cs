// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using LanguageMetadata = Snap.Hutao.Remastered.Metadata.LanguageMetadata;
using Snap.Hutao.Remastered.Model.Intrinsic;
using Snap.Hutao.Remastered.Model.Metadata;
using Snap.Hutao.Remastered.Model.Primitive;
using ProtoCombine = Snap.Hutao.Remastered.Metadata.Combine;
using ProtoCombineItem = Snap.Hutao.Remastered.Metadata.CombineItem;
using ProtoRecipeType = Snap.Hutao.Remastered.Metadata.RecipeType;

namespace Snap.Hutao.Remastered.Service.Metadata.Provider.Proto.Mapper;

internal sealed class CombineAdapter : ProtoCategoryAdapter<ProtoCombine, Combine>
{
    public CombineAdapter()
        : base(MetadataCategories.Combine)
    {
    }

    public override IEnumerable<ProtoCombine> Enumerate(LanguageMetadata language)
    {
        return language.Combines;
    }

    public override Combine Map(ProtoCombine message, MetadataConvert convert)
    {
        return new()
        {
            Id = (CombineId)message.Id,
            Type = message.Type,
            SubType = message.SubType,
            RecipeType = convert.ToEnum<ProtoRecipeType, RecipeType>(message.RecipeType, nameof(message.RecipeType)),
            Cost = message.Cost,
            Result = MapItem(message.Result, convert),
            Materials = [.. message.Materials.Select(item => MapItem(item, convert))],
            EffectDescription = convert.Text(message.EffectDescription),
        };
    }

    private static IdCount MapItem(ProtoCombineItem? item, MetadataConvert convert)
    {
        ArgumentNullException.ThrowIfNull(item);

        return new()
        {
            Id = (MaterialId)item.Id,
            Count = item.Count,
        };
    }
}
