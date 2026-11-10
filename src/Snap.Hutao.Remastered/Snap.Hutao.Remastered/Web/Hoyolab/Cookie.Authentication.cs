// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Web.WebView2.Core;
using Snap.Hutao.Remastered.Web.Hoyolab.Passport;
using System.Text;

namespace Snap.Hutao.Remastered.Web.Hoyolab;

public sealed partial class Cookie
{
    public static Cookie FromCoreWebView2Cookies(IReadOnlyList<CoreWebView2Cookie> webView2Cookies)
    {
        SortedDictionary<string, string> cookieMap = [];

        foreach (CoreWebView2Cookie cookie in webView2Cookies)
        {
            cookieMap.TryAdd(cookie.Name, cookie.Value);
        }

        return new(cookieMap);
    }

    public static Cookie FromLoginResult(LoginResult? loginResult)
    {
        if (loginResult is null)
        {
            return new();
        }

        SortedDictionary<string, string> cookieMap = new();
        if (loginResult.UserInfo is not null)
        {
            cookieMap.TryAdd(STUID, loginResult.UserInfo.Aid);
            cookieMap.TryAdd(MID, loginResult.UserInfo.Mid);
        }

        if (loginResult.Token is not null)
        {
            cookieMap.TryAdd(STOKEN, loginResult.Token.Token);
        }

        return new(cookieMap);
    }

    public static Cookie FromQrLoginResult(QrLoginResult? qrLoginResult)
    {
        if (qrLoginResult is null)
        {
            return new();
        }

        SortedDictionary<string, string> cookieMap = new()
        {
            [STUID] = qrLoginResult.UserInfo.Aid,
            [STOKEN] = qrLoginResult.Tokens.Single(token => token.TokenType is 1).Token,
            [MID] = qrLoginResult.UserInfo.Mid,
        };

        return new(cookieMap);
    }

}
