// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Web.WebView2.Core;
using Snap.Hutao.Remastered.UI.Xaml.View.Window.WebView2;
using Snap.Hutao.Remastered.ViewModel.User;
using Snap.Hutao.Remastered.Web.Bridge;
using Snap.Hutao.Remastered.Web.Hoyolab;

namespace Snap.Hutao.Remastered.ViewModel.DailyNote;

internal sealed class DailyJSBridgeUriSourceProvider : IJSBridgeUriSourceProvider
{
    public MiHoYoJSBridge CreateJSBridge(IServiceProvider serviceProvider, CoreWebView2 coreWebView2, UserAndUid userAndUid)
    {
        return ActivatorUtilities.CreateInstance<MiHoYoJSBridge>(serviceProvider, coreWebView2, userAndUid);
    }

    public string GetSource(UserAndUid userAndUid)
    {
        string query = userAndUid.Uid.ToRoleIdServerQueryString();
        return $"https://webstatic.mihoyo.com/app/community-game-records/index.html?bbs_presentation_style=fullscreen#/ys/daily/?{query}";
    }
}