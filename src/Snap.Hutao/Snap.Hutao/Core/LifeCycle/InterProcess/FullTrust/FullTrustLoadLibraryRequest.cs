// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Core.LifeCycle.InterProcess.FullTrust;

internal sealed class FullTrustLoadLibraryRequest
{
    public required string LibraryPath { get; set; }

    public static FullTrustLoadLibraryRequest Create(string libraryPath)
    {
        return new FullTrustLoadLibraryRequest()
        {
            LibraryPath = libraryPath,
        };
    }
}