// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Model.Intrinsic;

namespace Snap.Hutao.Remastered.UI.Xaml.View.Dialog;

public sealed class PresetComboItem
{
    public string DisplayName { get; set; } = string.Empty;
    public ReliquaryScoreConfigPreset? PresetKey { get; set; }
    public Guid? ConfigId { get; set; }
}
