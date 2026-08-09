// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core.Database.Abstraction;
using Snap.Hutao.Remastered.UI.Xaml.Data;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Snap.Hutao.Remastered.Model.Entity;

[Table("backpack_archives")]
public sealed partial class BackpackArchive : ISelectable, IPropertyValuesProvider
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid InnerId { get; set; }

    public string Name { get; set; } = default!;

    public bool IsSelected { get; set; }

    public static BackpackArchive Create(string name)
    {
        return new() { Name = name };
    }
}
