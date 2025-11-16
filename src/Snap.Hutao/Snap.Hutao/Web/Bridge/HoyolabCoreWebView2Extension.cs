// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Web.WebView2.Core;
using Snap.Hutao.Web.Hoyolab;
using Snap.Hutao.Web.WebView2;

namespace Snap.Hutao.Web.Bridge;

internal static class HoyolabCoreWebView2Extension
{
    extension(CoreWebView2 webView)
    {
        public ValueTask DeleteCookiesAsync(bool isOversea)
        {
            return webView.DeleteCookiesAsync(isOversea ? ".hoyolab.com" : ".mihoyo.com");
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
                cookieManager
                    .AddMihoyoCookie(Cookie.ACCOUNT_ID, cookieToken, isOversea)
                    .AddMihoyoCookie(Cookie.COOKIE_TOKEN, cookieToken, isOversea);

                if (lToken is not null)
                {
                    cookieManager
                        .AddMihoyoCookie(Cookie.LTUID, lToken, isOversea)
                        .AddMihoyoCookie(Cookie.LTOKEN, lToken, isOversea);
                }
            }

            return webView;
        }
    }

    extension(CoreWebView2CookieManager manager)
    {
        private CoreWebView2CookieManager AddMihoyoCookie(string name, Cookie cookie, bool isOversea = false)
        {
            string domain = isOversea ? ".hoyolab.com" : ".mihoyo.com";
            manager.AddOrUpdateCookie(manager.CreateCookie(name, cookie[name], domain, "/"));
            return manager;
        }
    }
}