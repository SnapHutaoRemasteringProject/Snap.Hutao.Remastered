// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Service.Git;

internal sealed class GitRepositoryException : AggregateException
{
    public GitRepositoryException(string message, IEnumerable<Exception> innerExceptions)
        : base(message, innerExceptions)
    {
    }
}