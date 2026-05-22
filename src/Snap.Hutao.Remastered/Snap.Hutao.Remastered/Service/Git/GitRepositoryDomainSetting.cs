// Copyright (c) Snap Hutao RP. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.Service.Git;

public static class GitRepositoryDomainSetting
{
    public const string Auto = "auto";

    public static bool IsAuto(string? value)
    {
        return string.IsNullOrWhiteSpace(value) || string.Equals(value, Auto, StringComparison.OrdinalIgnoreCase);
    }
}
