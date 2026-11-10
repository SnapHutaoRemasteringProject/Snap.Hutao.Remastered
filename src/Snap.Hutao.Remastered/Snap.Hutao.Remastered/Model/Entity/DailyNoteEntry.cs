// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;
using Snap.Hutao.Remastered.Model.Entity.Abstraction;
using Snap.Hutao.Remastered.Web.Hoyolab.Takumi.GameRecord.DailyNote;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Snap.Hutao.Remastered.Model.Entity;

[Table("daily_notes")]
public sealed partial class DailyNoteEntry : ObservableObject, IAppDbEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid InnerId { get; set; }

    public Guid UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = default!;

    public string Uid { get; set; } = default!;

    public DailyNote? DailyNote { get; set; }

    public DateTimeOffset RefreshTime { get; set; }

    [ObservableProperty]
    public partial int ResinNotifyThreshold { get; set; }

    public bool ResinNotifySuppressed { get; set; }

    public bool ResinDotVisible { get; set; }

    [ObservableProperty]
    public partial int HomeCoinNotifyThreshold { get; set; }

    public bool HomeCoinNotifySuppressed { get; set; }

    public bool HomeCoinDotVisible { get; set; }

    [ObservableProperty]
    public partial bool TransformerNotify { get; set; }

    public bool TransformerNotifySuppressed { get; set; }

    public bool TransformerDotVisible { get; set; }

    [ObservableProperty]
    public partial bool DailyTaskNotify { get; set; }

    public bool DailyTaskNotifySuppressed { get; set; }

    public bool DailyTaskDotVisible { get; set; }

    [ObservableProperty]
    public partial bool ExpeditionNotify { get; set; }

    public bool ExpeditionNotifySuppressed { get; set; }

    public bool ExpeditionDotVisible { get; set; }
}
