// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Snap.Hutao.Remastered.Model.Entity;

[Table("cultivate_entry_level_informations")]
public sealed partial class CultivateEntryLevelInformation
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid InnerId { get; set; }

    public Guid EntryId { get; set; }

    [ForeignKey(nameof(EntryId))]
    public CultivateEntry? Entry { get; set; }

    public uint AvatarLevelFrom { get; set; }

    public uint AvatarLevelTo { get; set; }

    public bool AvatarIsPromoting { get; set; }

    public uint SkillALevelFrom { get; set; }

    public uint SkillALevelTo { get; set; }

    public uint SkillELevelFrom { get; set; }

    public uint SkillELevelTo { get; set; }

    public uint SkillQLevelFrom { get; set; }

    public uint SkillQLevelTo { get; set; }

    public uint WeaponLevelFrom { get; set; }

    public uint WeaponLevelTo { get; set; }

    public bool WeaponIsPromoting { get; set; }
}
