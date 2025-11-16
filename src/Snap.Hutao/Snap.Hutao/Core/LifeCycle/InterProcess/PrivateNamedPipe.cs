// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Core.LifeCycle.InterProcess;

internal static class PrivateNamedPipe
{
    public const int PrivateVersion = 1;
    public const int FullTrustVersion = 1;
    public const string PrivateName = "Snap.Hutao.PrivateNamedPipe";
    public const string FullTrustName = "Snap.Hutao.PrivateFullTrustNamedPipe";
}