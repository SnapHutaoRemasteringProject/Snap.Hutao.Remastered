// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml;
using Microsoft.Web.WebView2.Core;
using Windows.Graphics;

namespace Snap.Hutao.Remastered.UI.Xaml.View.Window.WebView2;

public sealed class GeetestWebView2ContentProvider : DependencyObject, IWebView2ContentProvider
{
    private readonly string gt;
    private readonly string challenge;
    private readonly bool isOversea;
    private readonly TaskCompletionSource resultTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

    private string? result;

    public GeetestWebView2ContentProvider(string gt, string challenge, bool isOversea)
    {
        this.gt = gt;
        this.challenge = challenge;
        this.isOversea = isOversea;
    }

    public ElementTheme ActualTheme { get; set; }

    public CoreWebView2? CoreWebView2 { get; set; }

    public Action? CloseWindowAction { get; set; }

    public ValueTask InitializeAsync(IServiceProvider serviceProvider, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(CoreWebView2);

        CoreWebView2.WebMessageReceived += OnWebMessageReceived;

        CoreWebView2.NavigateToString($$"""
            <!doctype html>
            <html>
                <head>
                    <meta charset="utf-8">
                    <title>{{System.Net.WebUtility.HtmlEncode(SH.UIXamlViewWindowWebView2GeetestHeader)}}</title>
                    <style>
                        body { color-scheme: light dark; font-family: system-ui, sans-serif; text-align: center; margin-top: 3rem; }
                    </style>
                </head>
                <body>
                    <p id="status" role="status">{{System.Net.WebUtility.HtmlEncode(SH.UIXamlViewWindowWebView2GeetestHeader)}}</p>
                    <div id="geetest-div"></div>
                </body>
                <script>
                    const status = document.getElementById("status");
                    const timeout = setTimeout(() => showError(), 20000);
                    const showError = () => {
                        clearTimeout(timeout);
                        status.hidden = false;
                        status.textContent = {{JsonSerializer.Serialize(SH.ViewWebView2WindowLoadFailedHint)}};
                    };
                    const loader = document.createElement("script");
                    loader.src = "https://static.geetest.com/static/js/gt.0.5.0.js";
                    loader.onerror = showError;
                    loader.onload = () => {
                        if (typeof window.initGeetest !== "function") {
                            showError();
                            return;
                        }
                        try {
                            window.initGeetest({
                                protocol: "https://",
                                gt: {{JsonSerializer.Serialize(gt)}},
                                challenge: {{JsonSerializer.Serialize(challenge)}},
                                new_captcha: true,
                                offline: false,
                                product: "float",
                                api_server: {{JsonSerializer.Serialize(isOversea ? "api-na.geetest.com" : "api.geetest.com")}}
                            }, (captcha) => {
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

    private void OnWebMessageReceived(CoreWebView2 sender, CoreWebView2WebMessageReceivedEventArgs args)
    {
        result = args.WebMessageAsJson;
        CloseWindowAction?.Invoke();
    }
}
