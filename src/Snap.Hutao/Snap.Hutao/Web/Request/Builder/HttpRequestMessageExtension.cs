// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using System.Net.Http;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Snap.Hutao.Web.Request.Builder;

internal static class HttpRequestMessageExtension
{
    private const int MessageNotYetSent = 0;

    extension(HttpRequestMessage httpRequestMessage)
    {
        public void Resurrect()
        {
            // Mark the message as not yet sent
            Interlocked.Exchange(ref GetPrivateSendStatus(httpRequestMessage), MessageNotYetSent);

            if (httpRequestMessage.Content is { } content)
            {
                // Clear the buffered content, so that it can trigger a new read attempt
                // TODO: Remove reflection usage when UnsafeAccessorType supports fields
                // https://github.com/dotnet/runtime/issues/119664
                typeof(HttpContent).GetField("_bufferedContent", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(content, null);
                Volatile.Write(ref GetPrivateDisposed(content), false);
            }
        }
    }

    // private int _sendStatus
    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_sendStatus")]
    private static extern ref int GetPrivateSendStatus(HttpRequestMessage message);

    // private bool _disposed
    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_disposed")]
    private static extern ref bool GetPrivateDisposed(HttpContent content);
}