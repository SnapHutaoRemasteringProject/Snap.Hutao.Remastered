// Copyright (c) Snap Hutao RP. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core.Setting;
using Windows.Storage;

namespace Snap.Hutao.Remastered.Service.Update;

internal sealed record AlphaUpdateRange(string CurrentCommit, string? BaselineCommit, string? BaselineLabel = null)
{
    public static AlphaUpdateRange? ForBuild(string? current, string? previous)
    {
        if (current is null)
        {
            return null;
        }

        ApplicationDataCompositeValue saved = LocalSetting.Get(SettingKeys.LastAlphaComparison, new ApplicationDataCompositeValue());
        if (saved.TryGetValue(nameof(CurrentCommit), out object? savedCurrent)
            && string.Equals(current, savedCurrent as string, StringComparison.OrdinalIgnoreCase))
        {
            saved.TryGetValue(nameof(BaselineCommit), out object? savedBaseline);
            saved.TryGetValue(nameof(BaselineLabel), out object? savedLabel);
            return new(current, savedBaseline as string, savedLabel as string);
        }

        return new(current, string.Equals(current, previous, StringComparison.OrdinalIgnoreCase) ? null : previous);
    }

    public void Save()
    {
        ApplicationDataCompositeValue value = new()
        {
            [nameof(CurrentCommit)] = CurrentCommit,
            [nameof(BaselineCommit)] = BaselineCommit ?? string.Empty,
            [nameof(BaselineLabel)] = BaselineLabel ?? string.Empty,
        };
        LocalSetting.Set(SettingKeys.LastAlphaComparison, value);
    }
}
