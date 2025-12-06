// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Model.Metadata.Avatar;

namespace Snap.Hutao.Remastered.ViewModel.Calendar;

internal readonly struct MonthAndDay : IEquatable<MonthAndDay>
{
    public readonly uint Month;
    public readonly uint Day;

    public MonthAndDay(uint month, uint day)
    {
        Month = month;
        Day = day;
    }

    public static MonthAndDay Create(Avatar avatar)
    {
        return new MonthAndDay(avatar.FetterInfo.BirthMonth, avatar.FetterInfo.BirthDay);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Month, Day);
    }

    public bool Equals(MonthAndDay other)
    {
        return Month == other.Month && Day == other.Day;
    }

    public override bool Equals(object? obj)
    {
        return obj is MonthAndDay other && Equals(other);
    }
}