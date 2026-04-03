// Copyright (c) Millennium-Science-Technology-R-D-Inst. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;

namespace Snap.Hutao.Remastered.Service.Game.AdvancedStart.Model;

internal sealed partial class AdvancedStartDelayedProgramEntry : ObservableObject
{
    public AdvancedStartDelayedProgramEntry(string name, string path, int delaySeconds)
    {
        Name = name;
        Path = path;
        DelaySeconds = delaySeconds;
    }

    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Path { get; set; } = string.Empty;

    [ObservableProperty]
    public partial int DelaySeconds { get; set; }
}
