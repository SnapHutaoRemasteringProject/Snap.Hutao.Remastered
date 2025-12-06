// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Web.Request.Builder.Abstraction;
using System.Net.Http;

namespace Snap.Hutao.Remastered.Web.Request.Builder;

internal static class HttpContentDeserializerExtension
{
    extension(IHttpContentDeserializer deserializer)
    {
        public async ValueTask<T?> DeserializeAsync<T>(HttpContent? httpContent, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(deserializer);
            return (T?)await deserializer.DeserializeAsync(httpContent, typeof(T), cancellationToken).ConfigureAwait(false);
        }
    }
}