// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using System.Runtime.CompilerServices;
using Windows.ApplicationModel;

namespace Snap.Hutao.Extension;

internal static class PackageVersionExtension
{
    extension(PackageVersion packageVersion)
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Version ToVersion()
        {
            return new(packageVersion.Major, packageVersion.Minor, packageVersion.Build, packageVersion.Revision);
        }
    }
}