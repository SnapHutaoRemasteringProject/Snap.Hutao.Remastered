// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Snap.Hutao.Remastered.Model.Entity;

[Table("avatar_strategies")]
public sealed partial class AvatarStrategy
{
    [Key]
    public uint AvatarId { get; set; }

    public int ChineseStrategyId { get; set; }

    public int OverseaStrategyId { get; set; }
}
