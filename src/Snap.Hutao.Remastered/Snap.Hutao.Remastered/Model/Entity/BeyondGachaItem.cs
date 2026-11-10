// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Web.Hoyolab.Hk4e.Event.GachaInfo;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Snap.Hutao.Remastered.Model.Entity;

[Table("beyond_gacha_items")]
public sealed partial class BeyondGachaItem
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid InnerId { get; set; }

    [ForeignKey(nameof(ArchiveId))]
    public GachaArchive Archive { get; set; } = default!;

    public Guid ArchiveId { get; set; }

    public GachaType GachaType { get; set; }

    public GachaType QueryType { get; set; }

    public uint ItemId { get; set; }

    public DateTimeOffset Time { get; set; }

    public long Id { get; set; }

    public long ScheduleId { get; set; }

    public int IsUp { get; set; }
}
