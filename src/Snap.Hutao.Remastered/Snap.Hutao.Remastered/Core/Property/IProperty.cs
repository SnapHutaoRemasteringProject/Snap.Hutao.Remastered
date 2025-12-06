// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.Core.Property;

internal interface IProperty<T>
{
    T Value { get; set; }
}