// Copyright (c) Snap Hutao RP. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Service.Game.Scheme;
using Snap.Hutao.Remastered.Web.Hoyolab.Takumi.GameRecord.ActCalendar;
using System.Runtime.CompilerServices;

// Runtime Binding reaches this list through our property-access generator.
// Register its CCW independently of conversions visible in XAML-generated code.
[assembly: WinRT.GeneratedWinRTExposedExternalType(typeof(List<Act>))]

// All immutable UI snapshots use this concrete reference type at the ABI boundary.
[assembly: WinRT.GeneratedWinRTExposedExternalType(typeof(List<object>))]
[assembly: WinRT.GeneratedWinRTExposedExternalType(typeof(List<LaunchScheme>))]

[assembly: InternalsVisibleTo("Snap.Hutao.Remastered.Test")]
