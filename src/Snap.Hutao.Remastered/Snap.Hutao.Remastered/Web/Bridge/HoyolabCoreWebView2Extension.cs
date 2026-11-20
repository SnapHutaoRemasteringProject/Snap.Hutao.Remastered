// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Web.WebView2.Core;
using Snap.Hutao.Remastered.Web.Hoyolab;
using Snap.Hutao.Remastered.Web.WebView2;

namespace Snap.Hutao.Remastered.Web.Bridge;

public static class HoyolabCoreWebView2Extension
{
    extension(CoreWebView2 webView)
    {
        public async ValueTask DeleteCookiesAsync(bool isOversea)
        {
            await webView.DeleteCookiesAsync(isOversea ? ".hoyolab.com" : ".mihoyo.com");
            if (!isOversea)
            {
                await webView.DeleteCookiesAsync(".miyoushe.com");
            }
        }

        public CoreWebView2 SetMobileUserAgentChinese()
        {
            webView.Settings.UserAgent = HoyolabOptions.MobileUserAgent;
            return webView;
        }

        public CoreWebView2 SetMobileUserAgentOversea()
        {
            webView.Settings.UserAgent = HoyolabOptions.MobileUserAgentOversea;
            return webView;
        }

        public CoreWebView2? SetMobileUserAgent(bool isOversea)
        {
            return isOversea
                ? webView.SetMobileUserAgentOversea()
                : webView.SetMobileUserAgentChinese();
        }

        public CoreWebView2 SetCookie(Cookie? cookieToken = null, Cookie? lToken = null, bool isOversea = false)
        {
            CoreWebView2CookieManager cookieManager = webView.CookieManager;

            if (cookieToken is not null)
            {
                ReadOnlySpan<string> names = isOversea
                    ? [Cookie.ACCOUNT_ID, Cookie.COOKIE_TOKEN]
                    : [Cookie.ACCOUNT_ID_V2, Cookie.ACCOUNT_MID_V2, Cookie.COOKIE_TOKEN_V2];
                cookieManager.AddMihoyoCookies(names, cookieToken, isOversea);
            }

            if (lToken is not null)
            {
                ReadOnlySpan<string> names = isOversea
                    ? [Cookie.LTUID, Cookie.LTOKEN]
                    : [Cookie.LTUID_V2, Cookie.LTMID_V2, Cookie.LTOKEN_V2];
                cookieManager.AddMihoyoCookies(names, lToken, isOversea);
            }

            return webView;
        }
    }

    extension(CoreWebView2CookieManager manager)
    {
        private void AddMihoyoCookies(ReadOnlySpan<string> names, Cookie cookie, bool isOversea)
        {
            string domain = isOversea ? ".hoyolab.com" : ".mihoyo.com";
            foreach (string name in names)
            {
                if (cookie.TryGetValue(name, out string? value))
                {
                    AddCookie(manager, name, value, domain);
                    if (!isOversea)
                    {
                        AddCookie(manager, name, value, ".miyoushe.com");
                    }
                }
            }
        }
    }

    private static void AddCookie(CoreWebView2CookieManager manager, string name, string value, string domain)
    {
        CoreWebView2Cookie cookie = manager.CreateCookie(name, value, domain, "/");
        cookie.IsSecure = true;
        manager.AddOrUpdateCookie(cookie);
    }
}
