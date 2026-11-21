// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml;
using Microsoft.Web.WebView2.Core;
using Windows.Graphics;

namespace Snap.Hutao.Remastered.UI.Xaml.View.Window.WebView2;

public sealed class GeetestWebView2ContentProvider : DependencyObject, IWebView2ContentProvider
{
    private readonly string gt;
    private readonly string? challenge;
    private readonly string? sessionId;
    private readonly string? riskType;
    private readonly bool isOversea;
    private readonly TaskCompletionSource resultTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

    private string? result;

    public GeetestWebView2ContentProvider(string gt, string challenge, bool isOversea)
    {
        this.gt = gt;
        this.challenge = challenge;
        this.isOversea = isOversea;
    }

    public GeetestWebView2ContentProvider(string captchaId, string sessionId, string? riskType)
    {
        gt = captchaId;
        this.sessionId = sessionId;
        this.riskType = riskType;
    }

    public ElementTheme ActualTheme { get; set; }

    public CoreWebView2? CoreWebView2 { get; set; }

    public Action? CloseWindowAction { get; set; }

    public ValueTask InitializeAsync(IServiceProvider serviceProvider, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(CoreWebView2);

        CoreWebView2.WebMessageReceived += OnWebMessageReceived;
        bool isV4 = sessionId is not null;

        CoreWebView2.NavigateToString($$"""
            <!doctype html>
            <html>
                <head>
                    <meta charset="utf-8">
                    <meta name="viewport" content="width=device-width, initial-scale=1">
                    <title>{{System.Net.WebUtility.HtmlEncode(SH.UIXamlViewWindowWebView2GeetestHeader)}}</title>
                    <style>
                        body { color-scheme: light dark; font-family: system-ui, sans-serif; margin: 0; }
                        main { box-sizing: border-box; min-height: 100vh; display: grid; place-content: center; padding: 1rem; text-align: center; }
                        #geetest-div { display: flex; justify-content: center; }
                    </style>
                </head>
                <body>
                    <main>
                        <p id="status" role="status">{{System.Net.WebUtility.HtmlEncode(SH.UIXamlViewWindowWebView2GeetestHeader)}}</p>
                        <div id="geetest-div"></div>
                    </main>
                </body>
                <script>
                    const status = document.getElementById("status");
                    const timeout = setTimeout(() => showError(), 20000);
                    const showError = () => {
                        clearTimeout(timeout);
                        status.hidden = false;
                        status.textContent = {{ToScriptString(SH.ViewWebView2WindowLoadFailedHint)}};
                    };
                    const isV4 = {{(isV4 ? "true" : "false")}};
                    const loader = document.createElement("script");
                    loader.src = isV4
                        ? "https://static.geetest.com/v4/gt4.js"
                        : "https://static.geetest.com/static/js/gt.0.5.0.js";
                    loader.onerror = showError;
                    loader.onload = () => {
                        const initializeCaptcha = isV4 ? window.initGeetest4 : window.initGeetest;
                        if (typeof initializeCaptcha !== "function") {
                            showError();
                            return;
                        }
                        try {
                            const options = isV4 ? {
                                captchaId: {{ToScriptString(gt)}},
                                riskType: {{ToScriptString(riskType)}} || undefined,
                                userInfo: JSON.stringify({ session_id: {{ToScriptString(sessionId)}} }),
                                protocol: "https://",
                                https: true,
                                product: "popup",
                                onError: showError
                            } : {
                                protocol: "https://",
                                gt: {{ToScriptString(gt)}},
                                challenge: {{ToScriptString(challenge)}},
                                new_captcha: true,
                                offline: false,
                                product: "popup",
                                api_server: {{ToScriptString(isOversea ? "api-na.geetest.com" : "api.geetest.com")}}
                            };
                            initializeCaptcha(options, (captcha) => {
                                captcha.onError(showError);
                                captcha.onReady(() => {
                                    clearTimeout(timeout);
                                    status.hidden = true;
                                });
                                captcha.onSuccess(() => {
                                    const result = captcha.getValidate();
                                    if (result) chrome.webview.postMessage(result);
                                });
                                captcha.appendTo("#geetest-div");
                            });
                        } catch {
                            showError();
                        }
                    };
                    document.head.appendChild(loader);
              </script>
            </html>
            """);

        return ValueTask.CompletedTask;
    }

    public void Unload()
    {
        if (CoreWebView2 is { } webView)
        {
            webView.WebMessageReceived -= OnWebMessageReceived;
        }

        resultTcs.TrySetResult();
    }

    public RectInt32 InitializePosition(RectInt32 parentRect, double parentDpi)
    {
        return WebView2WindowPosition.Vertical(parentRect, parentDpi);
    }

    public async ValueTask<string?> GetResultAsync()
    {
        await resultTcs.Task.ConfigureAwait(false);
        return result;
    }

    private static string ToScriptString(string? value)
    {
        return value is null ? "null" : $"\"{JsonEncodedText.Encode(value)}\"";
    }

    private void OnWebMessageReceived(CoreWebView2 sender, CoreWebView2WebMessageReceivedEventArgs args)
    {
        result = args.WebMessageAsJson;
        CloseWindowAction?.Invoke();
    }
}
