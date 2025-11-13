// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Core.Abstraction;
using Snap.Hutao.Web.Request.Builder.Abstraction;
using System.Diagnostics;
using System.Net.Http;

namespace Snap.Hutao.Web.Request.Builder;

internal static class HttpMethodBuilderExtension
{
    extension<T>(T builder)
        where T : class, IHttpMethodBuilder
    {
        [DebuggerStepThrough]
        public T Get()
        {
            return builder.SetMethod(HttpMethod.Get);
        }

        [DebuggerStepThrough]
        public T Post()
        {
            return builder.SetMethod(HttpMethod.Post);
        }

        [DebuggerStepThrough]
        public T Put()
        {
            return builder.SetMethod(HttpMethod.Put);
        }

        [DebuggerStepThrough]
        public T Delete()
        {
            return builder.SetMethod(HttpMethod.Delete);
        }

        [DebuggerStepThrough]
        public T Options()
        {
            return builder.SetMethod(HttpMethod.Options);
        }

        [DebuggerStepThrough]
        public T Trace()
        {
            return builder.SetMethod(HttpMethod.Trace);
        }

        [DebuggerStepThrough]
        public T Head()
        {
            return builder.SetMethod(HttpMethod.Head);
        }

        [DebuggerStepThrough]
        public T Patch()
        {
            return builder.SetMethod(HttpMethod.Patch);
        }

        [DebuggerStepThrough]
        public T SetMethod(string method)
        {
            ArgumentNullException.ThrowIfNull(method);
            return builder.SetMethod(new HttpMethod(method));
        }

        [DebuggerStepThrough]
        public T SetMethod(HttpMethod method)
        {
            ArgumentNullException.ThrowIfNull(method);
            return builder.Configure(builder => builder.Method = method);
        }
    }
}