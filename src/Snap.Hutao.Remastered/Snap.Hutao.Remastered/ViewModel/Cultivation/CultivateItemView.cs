// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;
using Snap.Hutao.Remastered.Model;
using Snap.Hutao.Remastered.Model.Metadata.Item;

namespace Snap.Hutao.Remastered.ViewModel.Cultivation;

public sealed partial class CultivateItemView : ObservableObject, IEntityAccessWithMetadata<Model.Entity.CultivateItem, Material>
{
    private readonly TimeSpan offset;
    private readonly bool isAllMaterialsOpenToday;

    private CultivateItemView(Model.Entity.CultivateItem entity, Material inner, in TimeSpan offset, bool isAllMaterialsOpenToday)
    {
        Entity = entity;
        Inner = inner;
        this.offset = offset;
        this.isAllMaterialsOpenToday = isAllMaterialsOpenToday;
    }

    public Material Inner { get; }

    public Model.Entity.CultivateItem Entity { get; }

    public bool IsFinished
    {
        get => Entity.IsFinished;
        set => SetProperty(Entity.IsFinished, value, Entity, (entity, isFinished) => entity.IsFinished = isFinished);
    }

    public bool IsToday { get => Inner.IsItemOfToday(offset, true, isAllMaterialsOpenToday); }

    public DaysOfWeek DaysOfWeek { get => Inner.GetDaysOfWeek(); }

    public static CultivateItemView Create(Model.Entity.CultivateItem entity, Material inner, in TimeSpan offset, bool isAllMaterialsOpenToday)
    {
        return new(entity, inner, offset, isAllMaterialsOpenToday);
    }
}