// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.Factory.QuickResponse;

internal interface IQRCodeFactory
{
    byte[] Create(string source);
}