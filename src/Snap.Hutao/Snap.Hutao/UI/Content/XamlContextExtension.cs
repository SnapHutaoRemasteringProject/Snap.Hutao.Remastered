// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Content;
using Microsoft.UI.Xaml;

namespace Snap.Hutao.UI.Content;

internal static class XamlContextExtension
{
    extension(ContentIsland? contentIsland)
    {
        public XamlContext? XamlContext()
        {
            return contentIsland?.AppData as XamlContext;
        }
    }

    extension(XamlRoot xamlRoot)
    {
        public XamlContext? XamlContext()
        {
            return xamlRoot.ContentIsland?.AppData as XamlContext;
        }
    }
}