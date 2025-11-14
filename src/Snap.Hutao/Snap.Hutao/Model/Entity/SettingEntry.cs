// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Core.Setting;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Snap.Hutao.Model.Entity;

[Table("settings")]
internal sealed class SettingEntry : SettingKeys
{
    public SettingEntry(string key, string? value)
    {
        Key = key;
        Value = value;
    }

    [Key]
    public string Key { get; set; }

    public string? Value { get; set; }
}