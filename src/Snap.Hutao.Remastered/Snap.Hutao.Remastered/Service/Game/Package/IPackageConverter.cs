// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.Service.Game.Package;

internal interface IPackageConverter
{
    ValueTask<bool> EnsureGameResourceAsync(PackageConverterContext context);

    ValueTask EnsureDeprecatedFilesAndSDKAsync(PackageConverterDeprecationContext context);
}