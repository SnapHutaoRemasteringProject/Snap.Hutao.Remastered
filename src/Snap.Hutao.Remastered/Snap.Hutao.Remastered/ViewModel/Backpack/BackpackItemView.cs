// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;
using Snap.Hutao.Remastered.Model.Entity;
using Snap.Hutao.Remastered.Model.Intrinsic;
using Snap.Hutao.Remastered.Model.Metadata.Avatar;
using Snap.Hutao.Remastered.Model.Metadata.Converter;
using Snap.Hutao.Remastered.Model.Metadata.Item;
using Snap.Hutao.Remastered.Model.Metadata.Reliquary;
using Snap.Hutao.Remastered.Model.Metadata.Weapon;
using Snap.Hutao.Remastered.Model.Primitive;
using Snap.Hutao.Remastered.Service.Backpack;
using System.Collections.Immutable;

namespace Snap.Hutao.Remastered.ViewModel.Backpack;

public partial class BackpackItemView : ObservableObject
{
    public BackpackItem Entity { get; protected init => SetProperty(ref field, value); } = default!;

    [ObservableProperty]
    public partial BackpackItemCategory Category { get; protected set; }

    [ObservableProperty]
    public partial Material? Material { get; protected set; }

    [ObservableProperty]
    public partial string Name { get; protected set; } = string.Empty;

    public string DisplayCount => Entity.Count > 1 ? $"x{Entity.Count}" : string.Empty;

    [ObservableProperty]
    public partial string Description { get; protected set; } = string.Empty;

    [ObservableProperty]
    public partial string TypeDescription { get; protected set; } = string.Empty;

    [ObservableProperty]
    public partial Uri IconUri { get; protected set; } = default!;

    [ObservableProperty]
    public partial QualityType Quality { get; protected set; }

    [ObservableProperty]
    public partial ImmutableArray<string> SetDescriptions { get; protected set; } = [];

    public bool HasSetDescriptions => !SetDescriptions.IsDefaultOrEmpty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasEquippedAvatar))]
    [NotifyPropertyChangedFor(nameof(EquippedAvatarDisplayName))]
    public partial string? EquippedAvatarName { get; protected set; }

    [ObservableProperty]
    public partial Uri? EquippedAvatarIconUri { get; protected set; }

    public bool HasEquippedAvatar => EquippedAvatarName is not null;

    public string EquippedAvatarDisplayName => EquippedAvatarName is null
        ? string.Empty
        : SH.FormatViewPageBackpackEquippedState(EquippedAvatarName);

    public static BackpackItemView Create(BackpackItem entity, BackpackServiceMetadataContext context)
    {
        if (context.IdWeaponMap.TryGetValue(entity.ItemId, out Weapon? weapon))
        {
            return BackpackWeaponItemView.Create(entity, context, weapon);
        }

        if (entity.MainPropId is not null && context.IdReliquaryMap.TryGetValue(entity.ItemId, out Reliquary? reliquary))
        {
            return BackpackReliquaryItemView.Create(entity, context, reliquary);
        }

        // First try to classify by ItemId alone
        // If that returns Material (default), try to get Material metadata and classify by MaterialType
        context.IdMaterialMap.TryGetValue(entity.ItemId, out Material? material);

        BackpackItemView view = new()
        {
            Entity = entity,
            Material = material,
            Category = material.GetCategory(entity.ItemId),
            Name = material?.Name ?? string.Empty,
            Description = material?.Description ?? string.Empty,
            TypeDescription = material?.TypeDescription ?? string.Empty,
            IconUri = material is not null ? ItemIconConverter.IconNameToUri(material.Icon) : default!,
            Quality = material?.RankLevel ?? QualityType.QUALITY_NONE,
        };

        view.FillEquippedAvatar(context);

        return view;
    }

    /// <summary>
    /// Resolves the equipping avatar from metadata. Missing avatar metadata is a degrade, not an error.
    /// </summary>
    protected void FillEquippedAvatar(BackpackServiceMetadataContext context)
    {
        if (Entity.EquippedAvatarId is 0)
        {
            return;
        }

        if (!context.IdAvatarMap.TryGetValue((AvatarId)Entity.EquippedAvatarId, out Avatar? avatar))
        {
            return;
        }

        EquippedAvatarName = avatar.Name;
        EquippedAvatarIconUri = AvatarSideIconConverter.IconNameToUri(avatar.SideIcon);
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        switch (e.PropertyName)
        {
            case nameof(SetDescriptions):
                OnPropertyChanged(nameof(HasSetDescriptions));
                break;
            case nameof(Entity):
                OnPropertyChanged(nameof(DisplayCount));
                break;
        }
    }
}
