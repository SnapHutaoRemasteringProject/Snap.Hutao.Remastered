// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.Service.Yae.Achievement;

public sealed class NativeConfiguration
{
    [JsonPropertyName("storeCmdId")]
    public required uint StoreCmdId { get; init; }

    [JsonPropertyName("achievementCmdId")]
    public required uint AchievementCmdId { get; init; }

    [JsonPropertyName("avatarCmdId")]
    public required uint AvatarCmdId { get; init; }

    [JsonPropertyName("methodRva")]
    public required MethodRvaWrapper MethodRva { get; init; }
}