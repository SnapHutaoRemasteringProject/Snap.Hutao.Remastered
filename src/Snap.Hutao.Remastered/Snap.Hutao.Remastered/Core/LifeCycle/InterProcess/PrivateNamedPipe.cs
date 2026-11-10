// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.Core.LifeCycle.InterProcess;

public static class PrivateNamedPipe
{
    public const int PrivateVersion = 1;
    public const int FullTrustVersion = 1;
    public static readonly string PrivateName = "Snap.Hutao.Remastered.PrivateNamedPipe" + AppDataPaths.InstanceSuffix;
    public const string FullTrustName = "Snap.Hutao.Remastered.PrivateFullTrustNamedPipe";
    public const string ToastPipeName = "Snap.Hutao.Remastered.ToastPipe";
}
