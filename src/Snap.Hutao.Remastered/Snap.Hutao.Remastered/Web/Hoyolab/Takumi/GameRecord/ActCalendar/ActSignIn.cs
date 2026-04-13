// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.Web.Hoyolab.Takumi.GameRecord.ActCalendar;

internal sealed class ActSignIn : Act
{
    [JsonPropertyName("sign_in_detail")]
    public required ActSignInDetail SignInDetail { get; init; }
}