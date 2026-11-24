// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Web.Hutao;

namespace Snap.Hutao.Remastered.Service.Update;

public sealed class CheckUpdateResult
{
    public CheckUpdateResultKind Kind { get; set; }

    public HutaoPackageInformation? PackageInformation { get; set; }

    public bool IsMirrorChyanOnly { get; set; }
}
