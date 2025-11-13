// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Web.Request.Builder.Abstraction;
using System.Text;

namespace Snap.Hutao.Web.Request.Builder;

internal static class JsonBuilderExtension
{
    extension<TBuilder>(TBuilder builder)
        where TBuilder : class, IHttpContentBuilder
    {
        public TBuilder SetJsonContent<TContent>(TContent content, Encoding? encoding = null, JsonHttpContentSerializer? serializer = null)
        {
            return builder.SetContent(serializer ?? builder.HttpContentSerializer, content, encoding);
        }

        public TBuilder SetJsonContent(object? content, Type contentType, Encoding? encoding = null, JsonHttpContentSerializer? serializer = null)
        {
            return builder.SetContent(serializer ?? builder.HttpContentSerializer, content, contentType, encoding);
        }
    }

    extension<TBuilder>(TBuilder builder)
        where TBuilder : class, IHttpMethodBuilder, IHttpContentBuilder
    {
        public TBuilder PostJson<TContent>(TContent content, Encoding? encoding = null, JsonHttpContentSerializer? serializer = null)
        {
            return builder.Post().SetJsonContent(content, encoding, serializer);
        }

        public TBuilder PostJson(object? content, Type contentType, Encoding? encoding = null, JsonHttpContentSerializer? serializer = null)
        {
            return builder.Post().SetJsonContent(content, contentType, encoding, serializer);
        }

        public TBuilder PutJson<TContent>(TContent content, Encoding? encoding = null, JsonHttpContentSerializer? serializer = null)
        {
            return builder.Put().SetJsonContent(content, encoding, serializer);
        }

        public TBuilder PutJson(object? content, Type contentType, Encoding? encoding = null, JsonHttpContentSerializer? serializer = null)
        {
            return builder.Put().SetJsonContent(content, contentType, encoding, serializer);
        }

        public TBuilder PatchJson<TContent>(TContent content, Encoding? encoding = null, JsonHttpContentSerializer? serializer = null)
        {
            return builder.Patch().SetJsonContent(content, encoding, serializer);
        }

        public TBuilder PatchJson(object? content, Type contentType, Encoding? encoding = null, JsonHttpContentSerializer? serializer = null)
        {
            return builder.Patch().SetJsonContent(content, contentType, encoding, serializer);
        }
    }
}