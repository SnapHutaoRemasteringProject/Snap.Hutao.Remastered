// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using LanguageMetadata = Snap.Hutao.Remastered.Metadata.LanguageMetadata;
using Snap.Hutao.Remastered.Model.Intrinsic;
using Snap.Hutao.Remastered.Model.Metadata;
using Snap.Hutao.Remastered.Model.Primitive;
using ProtoCookFoodType = Snap.Hutao.Remastered.Metadata.CookFoodType;
using ProtoCookMethod = Snap.Hutao.Remastered.Metadata.CookMethod;
using ProtoCookRecipe = Snap.Hutao.Remastered.Metadata.CookRecipe;
using ProtoCookItem = Snap.Hutao.Remastered.Metadata.CookItem;

namespace Snap.Hutao.Remastered.Service.Metadata.Provider.Proto.Mapper;

internal sealed class CookRecipeAdapter : ProtoCategoryAdapter<ProtoCookRecipe, CookRecipe>
{
    public CookRecipeAdapter()
        : base(MetadataCategories.CookRecipe)
    {
    }

    public override IEnumerable<ProtoCookRecipe> Enumerate(LanguageMetadata language)
    {
        return language.CookRecipes;
    }

    public override CookRecipe Map(ProtoCookRecipe message, MetadataConvert convert)
    {
        return new()
        {
            Id = (CookRecipeId)message.Id,
            Name = convert.Text(message.Name),
            Description = convert.Text(message.Description),
            EffectDescription = [.. message.EffectDescription.Select(convert.Text)],
            CookMethod = convert.ToEnum<ProtoCookMethod, CookMethod>(message.CookMethod, nameof(message.CookMethod)),
            FoodType = convert.ToEnum<ProtoCookFoodType, CookFoodType>(message.FoodType, nameof(message.FoodType)),
            Icon = convert.Text(message.Icon),
            IsDefaultUnlocked = message.IsDefaultUnlocked,
            MaxProficiency = message.MaxProficiency,
            RankLevel = convert.RawEnum<QualityType>(message.RankLevel, nameof(message.RankLevel)),
            QualityOutput = [.. message.QualityOutput.Select(item => MapItem(item, convert))],
            Input = [.. message.Input.Select(item => MapItem(item, convert))],
        };
    }

    private static IdCount MapItem(ProtoCookItem? item, MetadataConvert convert)
    {
        ArgumentNullException.ThrowIfNull(item);

        return new()
        {
            Id = (MaterialId)item.Id,
            Count = item.Count,
        };
    }
}
