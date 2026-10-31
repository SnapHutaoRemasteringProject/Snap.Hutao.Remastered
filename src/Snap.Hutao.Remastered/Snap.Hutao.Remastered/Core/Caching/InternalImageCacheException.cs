// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core.ExceptionService;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Snap.Hutao.Remastered.Core.Caching;

internal sealed class InternalImageCacheException : Exception, IInternalException
{
    private InternalImageCacheException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    private InternalImageCacheException(string message)
        : base(message)
    {
    }

    [StackTraceHidden]
    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static InternalImageCacheException Throw(string message, Exception innerException)
    {
        throw new InternalImageCacheException(message, innerException);
    }

    [StackTraceHidden]
    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static InternalImageCacheException Throw(string message)
    {
        throw new InternalImageCacheException(message);
    }
}
