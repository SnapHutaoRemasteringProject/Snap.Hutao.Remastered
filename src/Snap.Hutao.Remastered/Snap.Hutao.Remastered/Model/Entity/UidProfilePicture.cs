// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Snap.Hutao.Remastered.Model.Entity;

[Table("uid_profile_pictures")]
public sealed partial class UidProfilePicture
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid InnerId { get; set; }

    public string Uid { get; set; } = default!;

    public uint ProfilePictureId { get; set; }

    public uint AvatarId { get; set; }

    public uint CostumeId { get; set; }

    public DateTimeOffset RefreshTime { get; set; }
}
