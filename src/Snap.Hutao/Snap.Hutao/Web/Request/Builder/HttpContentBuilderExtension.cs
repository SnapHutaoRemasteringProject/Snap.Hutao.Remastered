// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Core.Abstraction;
using Snap.Hutao.Web.Request.Builder.Abstraction;
using System.Diagnostics;
using System.Net.Http;
using System.Text;

namespace Snap.Hutao.Web.Request.Builder;

internal static class HttpContentBuilderExtension
{
    extension<T>(T builder)
        where T : class, IHttpContentBuilder
    {
        [DebuggerStepThrough]
        public T SetFormUrlEncodedContent(params (string Key, string Value)[] content)
        {
            return builder.SetFormUrlEncodedContent((IEnumerable<(string, string)>)content);
        }

        [DebuggerStepThrough]
        public T SetFormUrlEncodedContent(IEnumerable<(string Key, string Value)> content)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(content);
            return builder.SetFormUrlEncodedContent(content.Select(x => new KeyValuePair<string, string>(x.Key, x.Value)));
        }

        [DebuggerStepThrough]
        public T SetFormUrlEncodedContent(params KeyValuePair<string, string>[] content)
        {
            return builder.SetFormUrlEncodedContent((IEnumerable<KeyValuePair<string, string>>)content);
        }

        [DebuggerStepThrough]
        public T SetFormUrlEncodedContent(IEnumerable<KeyValuePair<string, string>> content)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(content);
            return builder.SetContent(new FormUrlEncodedContent(content));
        }

        [DebuggerStepThrough]
        public T SetStringContent(string content, Encoding? encoding = null, string? mediaType = default)
        {
            ArgumentNullException.ThrowIfNull(content);
            return builder.SetContent(new StringContent(content, encoding, mediaType));
        }

        [DebuggerStepThrough]
        public T SetByteArrayContent(byte[] content)
        {
            return builder.SetByteArrayContent(content, 0, content?.Length ?? 0);
        }

        [DebuggerStepThrough]
        public T SetByteArrayContent(byte[] content, int offset, int count)
        {
            ArgumentNullException.ThrowIfNull(content);
            return builder.SetContent(new ByteArrayContent(content, offset, count));
        }

        public T SetContent<TContent>(IHttpContentSerializer serializer, TContent content, Encoding? encoding = null)
        {
            // Validate builder here already so that no unnecessary serialization is done.
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(serializer);

            // Don't call the SetContent(object, Type) overload on purpose.
            // We have the extension method for a generic serialize, so we should use it.
            // If the behavior ever changes, only the extension method will have to be updated.
            HttpContent? httpContent = serializer.Serialize(content, encoding);
            return builder.SetContent(httpContent);
        }

        public T SetContent(IHttpContentSerializer serializer, object? content, Type contentType, Encoding? encoding = null)
        {
            // Validate builder here already so that no unnecessary serialization is done.
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(serializer);

            HttpContent? httpContent = serializer.Serialize(content, contentType, encoding);
            return builder.SetContent(httpContent);
        }

        [DebuggerStepThrough]
        public T SetContent(HttpContent? content)
        {
            return builder.Configure(builder => builder.Content = content);
        }
    }
}