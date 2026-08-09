// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Snap.Hutao.Remastered.Model.Entity;

[Table("backpack_items")]
public sealed class BackpackItem
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid InnerId { get; set; }

    [ForeignKey(nameof(ArchiveId))]
    public BackpackArchive Archive { get; set; } = default!;

    public Guid ArchiveId { get; set; }

    public uint ItemId { get; set; }

    public ulong Guid { get; set; }

    public uint Count { get; set; }

    public uint Level { get; set; }

    public uint PromoteLevel { get; set; }

    public uint RefinementRank { get; set; }

    public uint? MainPropId { get; set; }

    public string? AppendPropIdListJson { get; set; }

    public bool IsLocked { get; set; }

    public bool IsMarked { get; set; }
}
