// Copyright (c) Snap Hutao RP. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.ViewModel.Guide;

public static class GuideNavigation
{
    public static uint Normalize(uint state) => Math.Min(state, (uint)GuideState.Completed);

    public static uint VisibleState(uint state) => Math.Min(state, (uint)GuideState.Material);

    public static int StepIndex(uint state) => (int)VisibleState(state);

    public static bool IsFinalStep(uint state) => state >= (uint)GuideState.Material;

    public static GuideState StateAfterDownload(bool skipRequested, int remaining) => skipRequested || remaining == 0 ? GuideState.Material : GuideState.StaticResourceSetting;
}
