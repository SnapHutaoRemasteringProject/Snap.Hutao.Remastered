// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Core.LifeCycle.InterProcess.FullTrust;

internal sealed class FullTrustLoadLibraryRequest
{
    public required string LibraryName { get; set; }

    public required string LibraryPath { get; set; }

    public string? FunctionName { get; set; }

    public static FullTrustLoadLibraryRequest Create(string libraryName, string libraryPath)
    {
        return new FullTrustLoadLibraryRequest()
        {
            LibraryName = libraryName,
            LibraryPath = libraryPath,
        };
    }
}