// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.
using System.IO.Pipes;

namespace Snap.Hutao.Core.LifeCycle.InterProcess.FullTrust;

internal sealed partial class FullTrustNamedPipeClient : IDisposable
{
    private readonly NamedPipeClientStream clientStream = new(".", PrivateNamedPipe.FullTrustName, PipeDirection.InOut, PipeOptions.Asynchronous | PipeOptions.WriteThrough);

    public void Dispose()
    {
        clientStream.Dispose();
    }
}