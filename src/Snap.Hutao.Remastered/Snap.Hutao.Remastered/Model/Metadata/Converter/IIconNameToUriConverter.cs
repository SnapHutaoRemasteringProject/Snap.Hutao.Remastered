// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.Model.Metadata.Converter;

internal interface IIconNameToUriConverter
{
    static abstract Uri IconNameToUri(string name);
}