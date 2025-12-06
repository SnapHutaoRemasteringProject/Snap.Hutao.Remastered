// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.Core.DependencyInjection.Abstraction;

internal interface IOverseaSupportFactory<out TClient>
{
    TClient Create(bool isOversea);
}