// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core.Abstraction;

namespace Snap.Hutao.Remastered.Web.Request.Builder.Abstraction;

internal interface IRequestUriBuilder : IBuilder
{
    Uri? RequestUri { get; set; }
}