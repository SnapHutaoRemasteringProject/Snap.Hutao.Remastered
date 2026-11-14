// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Model.Metadata.Converter;
using Snap.Hutao.Remastered.Model.Primitive;
using Snap.Hutao.Remastered.Web.Endpoint.Hutao;
using Snap.Hutao.Remastered.Web.Hutao.SpiralAbyss;
using System.Collections.Immutable;
using System.Text;

namespace Snap.Hutao.Remastered.ViewModel.Complex;

/// <summary>
/// 圣遗物套装
/// </summary>
public sealed partial class ReliquarySetView : RateAndDelta
{
    public ReliquarySetView(ImmutableDictionary<ExtendedEquipAffixId, Model.Metadata.Reliquary.ReliquarySet> idReliquarySetMap, ItemRate<ReliquarySets, double> setRate, ItemRate<ReliquarySets, double>? lastSetRate)
        : base(setRate.Rate, lastSetRate?.Rate)
    {
        ReliquarySets sets = setRate.Item;

        if (sets is [_, ..])
        {
            StringBuilder nameBuilder = new();
            List<Uri> icons = new(2);
            foreach (ReliquarySet set in sets)
            {
                Model.Metadata.Reliquary.ReliquarySet metaSet = idReliquarySetMap[set.EquipAffixId];
                nameBuilder.Append(set.Count).Append('×').Append(metaSet.Name).Append('+');
                icons.Add(RelicIconConverter.IconNameToUri(metaSet.Icon));
            }

            Name = nameBuilder.ToString(0, nameBuilder.Length - 1);
            Icons = icons;
        }
        else
        {
            Name = SH.ViewModelComplexReliquarySetViewEmptyName;
        }
    }

    /// <summary>
    /// 名称
    /// </summary>
    public string Name { get; private set => SetProperty(ref field, value); }

    /// <summary>
    /// 图标
    /// </summary>
    public List<Uri>? Icons { get; private set => SetProperty(ref field, value); }

    /// <summary>
    /// 第一个图标，供卡片左侧直接绑定；空集合时使用占位图。
    /// </summary>
    public Uri Icon => Icons is [var icon, ..] ? icon : StaticResourcesEndpoints.UIItemIconNone;

    /// <summary>
    /// 第二个图标；单套装时为 null。
    /// </summary>
    public Uri? SecondaryIcon => Icons is [_, var icon, ..] ? icon : null;
}
