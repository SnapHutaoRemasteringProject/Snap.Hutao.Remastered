using Snap.Hutao.Remastered.Model.Intrinsic;
using Snap.Hutao.Remastered.Model.Metadata.Abstraction;
using Snap.Hutao.Remastered.Model.Metadata.Converter;
using Snap.Hutao.Remastered.Model.Primitive;
using Snap.Hutao.Remastered.ViewModel.GachaLog;

namespace Snap.Hutao.Remastered.Model.Metadata.Item;

[ProtoContract]
public partial class BydMaterial : 
    IDefaultIdentity<BydMaterialId>,
    ISummaryItemConvertible,
    IStatisticsItemConvertible,
    IItemConvertible
{
    [ProtoMember(1)]
    public BydMaterialId Id { get; init; }

    [ProtoMember(6)]
    public QualityType RankLevel { get; init; }

    [ProtoMember(4)]
    public string Icon { get; set; } = string.Empty;

    [ProtoMember(2)]
    public string Name { get; set; } = string.Empty;

    [ProtoMember(3)]
    public string? Description { get; set; }

    public string? TypeDescription { get; set; }

    [ProtoMember(5)]
    public int Type { get; set; }

    [JsonIgnore]
    public QualityType Quality { get => RankLevel; }

    public SummaryItem ToSummaryItem(int lastPull, in DateTimeOffset time, bool isUp)
    {
        return new()
        {
            Id = Id,
            Name = Name,
            Icon = BydMaterialNameIconConverter.IconNameToUri(Icon),
            Time = time,
            Quality = RankLevel,
            LastPull = lastPull,
            IsUp = isUp,
        };
    }

    public StatisticsItem ToStatisticsItem(int count)
    {
        return new()
        {
            Id = Id,
            Name = Name,
            Icon = BydMaterialNameIconConverter.IconNameToUri(Icon),
            Quality = RankLevel,
            Count = count,
        };
    }

    public TItem ToItem<TItem>()
        where TItem : Model.Item, new()
    {
        return new()
        {
            Id = Id,
            Name = Name,
            Icon = BydMaterialNameIconConverter.IconNameToUri(Icon),
            Quality = Quality,
        };
    }
}
