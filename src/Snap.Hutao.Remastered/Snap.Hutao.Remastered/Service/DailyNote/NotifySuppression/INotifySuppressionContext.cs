// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Model.Entity;

namespace Snap.Hutao.Remastered.Service.DailyNote.NotifySuppression;

internal interface INotifySuppressionContext
{
    DailyNoteEntry Entry { get; }

    Web.Hoyolab.Takumi.GameRecord.DailyNote.DailyNote DailyNote
    {
        get
        {
            ArgumentNullException.ThrowIfNull(Entry.DailyNote);
            return Entry.DailyNote;
        }
    }

    void Add(DailyNoteNotifyInfo info);
}