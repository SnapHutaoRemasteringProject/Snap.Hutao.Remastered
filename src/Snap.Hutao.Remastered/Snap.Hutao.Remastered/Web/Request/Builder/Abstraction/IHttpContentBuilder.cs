// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core.Abstraction;
using System.Net.Http;

namespace Snap.Hutao.Remastered.Web.Request.Builder.Abstraction;

internal interface IHttpContentBuilder : IBuilder
{
    HttpContent? Content { get; set; }

    HttpContentSerializer HttpContentSerializer { get; }
}