// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Core.Abstraction;
using Snap.Hutao.Web.Request.Builder.Abstraction;
using System.Diagnostics;

namespace Snap.Hutao.Web.Request.Builder;

internal static class RequestUriBuilderExtension
{
    extension<T>(T builder)
        where T : class, IRequestUriBuilder
    {
        [DebuggerStepThrough]
        public T SetRequestUri(string? requestUri, UriKind uriKind = UriKind.RelativeOrAbsolute)
        {
            return builder.SetRequestUri(requestUri is null ? null : new Uri(requestUri, uriKind));
        }

        [DebuggerStepThrough]
        public T SetRequestUri(Uri? requestUri)
        {
            return builder.Configure(builder => builder.RequestUri = requestUri);
        }
    }
}