// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.UI.Xaml.Data;

internal interface IPropertyValuesProvider
{
    object? GetPropertyValue(string name);
}