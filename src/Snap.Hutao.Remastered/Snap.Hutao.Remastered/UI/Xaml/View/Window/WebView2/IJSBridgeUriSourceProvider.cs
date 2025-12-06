// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Web.WebView2.Core;
using Snap.Hutao.Remastered.ViewModel.User;
using Snap.Hutao.Remastered.Web.Bridge;

namespace Snap.Hutao.Remastered.UI.Xaml.View.Window.WebView2;

internal interface IJSBridgeUriSourceProvider
{
    MiHoYoJSBridge CreateJSBridge(IServiceProvider serviceProvider, CoreWebView2 coreWebView2, UserAndUid userAndUid);

    string? GetSource(UserAndUid userAndUid);
}