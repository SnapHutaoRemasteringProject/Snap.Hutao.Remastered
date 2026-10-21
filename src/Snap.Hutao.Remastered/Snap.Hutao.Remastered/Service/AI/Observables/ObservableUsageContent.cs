// Copyright (c) 2026 DGP Studio. All rights reserved.
// Licensed under the MIT license.
// Adapted from Snap.Nicole.

using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.AI;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace Snap.Hutao.Remastered.Service.AI.Observables;

internal sealed partial class ObservableUsageContent : ObservableAIContent
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasCounts))]
    public partial long? TotalTokenCount { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasCounts), nameof(CacheHitRate))]
    public partial long? InputTokenCount { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasCounts))]
    public partial long? InputAudioTokenCount { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasCounts))]
    public partial long? InputTextTokenCount { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasCounts))]
    public partial long? ReasoningTokenCount { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasCounts))]
    public partial long? OutputTokenCount { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasCounts))]
    public partial long? OutputAudioTokenCount { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasCounts))]
    public partial long? OutputTextTokenCount { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasCounts), nameof(CacheHitRate))]
    public partial long? CachedInputTokenCount { get; set; }

    [JsonIgnore]
    public double? CacheHitRate
    {
        get
        {
            if (InputTokenCount is not long inputTokenCount || inputTokenCount <= 0 ||
                CachedInputTokenCount is not long cachedInputTokenCount || cachedInputTokenCount <= 0)
            {
                return null;
            }

            return (double)cachedInputTokenCount / inputTokenCount;
        }
    }

    [JsonIgnore]
    public bool HasCounts
    {
        get =>
            TotalTokenCount.HasValue ||
            InputTokenCount.HasValue ||
            InputAudioTokenCount.HasValue ||
            InputTextTokenCount.HasValue ||
            ReasoningTokenCount.HasValue ||
            OutputTokenCount.HasValue ||
            OutputAudioTokenCount.HasValue ||
            OutputTextTokenCount.HasValue ||
            CachedInputTokenCount.HasValue;
    }

    public static ObservableUsageContent? Create(UsageContent usageContent)
    {
        UsageDetails details = usageContent.Details;
        if (details is null ||
            ((details.TotalTokenCount is not long totalTokenCount || totalTokenCount <= 0) &&
            (details.InputTokenCount is not long inputTokenCount || inputTokenCount <= 0) &&
            (details.InputAudioTokenCount is not long inputAudioTokenCount || inputAudioTokenCount < 0) &&
            (details.InputTextTokenCount is not long inputTextTokenCount || inputTextTokenCount < 0) &&
            (details.ReasoningTokenCount is not long reasoningTokenCount || reasoningTokenCount < 0) &&
            (details.OutputTokenCount is not long outputTokenCount || outputTokenCount < 0) &&
            (details.OutputAudioTokenCount is not long outputAudioTokenCount || outputAudioTokenCount < 0) &&
            (details.OutputTextTokenCount is not long outputTextTokenCount || outputTextTokenCount < 0) &&
            (details.CachedInputTokenCount is not long cachedInputTokenCount || cachedInputTokenCount < 0)))
        {
            return null;
        }

        return new()
        {
            TotalTokenCount = NormalizeCount(details?.TotalTokenCount),
            InputTokenCount = NormalizeCount(details?.InputTokenCount),
            InputAudioTokenCount = NormalizeCount(details?.InputAudioTokenCount),
            InputTextTokenCount = NormalizeCount(details?.InputTextTokenCount),
            ReasoningTokenCount = NormalizeCount(details?.ReasoningTokenCount),
            OutputTokenCount = NormalizeCount(details?.OutputTokenCount),
            OutputAudioTokenCount = NormalizeCount(details?.OutputAudioTokenCount),
            OutputTextTokenCount = NormalizeCount(details?.OutputTextTokenCount),
            CachedInputTokenCount = NormalizeCount(details?.CachedInputTokenCount)
        };
    }

    public void Update(ObservableUsageContent usageContent)
    {
        TotalTokenCount = usageContent.TotalTokenCount;
        InputTokenCount = usageContent.InputTokenCount;
        InputAudioTokenCount = usageContent.InputAudioTokenCount;
        InputTextTokenCount = usageContent.InputTextTokenCount;
        ReasoningTokenCount = usageContent.ReasoningTokenCount;
        OutputTokenCount = usageContent.OutputTokenCount;
        OutputAudioTokenCount = usageContent.OutputAudioTokenCount;
        OutputTextTokenCount = usageContent.OutputTextTokenCount;
        CachedInputTokenCount = usageContent.CachedInputTokenCount;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static long? NormalizeCount(long? value)
    {
        // Treat 0 as null since some providers may return 0 instead of null when the count is not available
        return value is long count && count != 0 ? count : null;
    }
}
