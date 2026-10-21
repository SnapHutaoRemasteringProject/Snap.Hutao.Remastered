// Copyright (c) 2026 DGP Studio. All rights reserved.
// Licensed under the MIT license.
// Adapted from Snap.Nicole.

namespace Snap.Hutao.Remastered.Service.AI.Models;

internal static class AgentOptionsNormalizer
{
    public const string DefaultUserName = "You";

    public const string DefaultSystemPrompt = "You are a helpful Genshin Impact account analysis assistant.";

    public const int DefaultMaximumIterationsPerRequest = 500;

    public static string NormalizeUserName(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? DefaultUserName : value.Trim();
    }

    public static int? NormalizeTokenLimit(int? value)
    {
        return value is > 0 ? value : null;
    }

    public static string? NormalizeSystemPrompt(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    public static int NormalizeMaximumIterationsPerRequest(int? value)
    {
        return value is > 0 ? value.Value : DefaultMaximumIterationsPerRequest;
    }
}
