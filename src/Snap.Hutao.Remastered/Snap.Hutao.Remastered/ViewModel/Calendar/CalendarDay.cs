// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;
using Snap.Hutao.Remastered.Model;
using Snap.Hutao.Remastered.UI.Xaml.Data;
using System.Collections.Immutable;

namespace Snap.Hutao.Remastered.ViewModel.Calendar;

public sealed partial class CalendarDay : ObservableObject, IPropertyValuesProvider
{
    public required DateTimeOffset Date { get; init => SetProperty(ref field, value); }

    public required int DayInMonth { get; init => SetProperty(ref field, value); }

    public required string DayName { get; init => SetProperty(ref field, value); }

    public required ImmutableArray<Item> BirthDayAvatars { get; init => SetProperty(ref field, value); }

    public required ImmutableArray<CalendarMaterial> Materials { get; init => SetProperty(ref field, value); }
}
