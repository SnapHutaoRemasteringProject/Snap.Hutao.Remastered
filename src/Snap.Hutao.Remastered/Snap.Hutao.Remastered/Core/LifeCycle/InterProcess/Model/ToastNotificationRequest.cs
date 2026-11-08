// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.Core.LifeCycle.InterProcess.Model;

public sealed class ToastNotificationRequest
{
    [JsonConstructor]
    public ToastNotificationRequest(string rawXml, bool suppressDisplay = false)
    {
        RawXml = rawXml;
        SuppressDisplay = suppressDisplay;
    }

    public string RawXml { get; }

    public bool SuppressDisplay { get; }
}
