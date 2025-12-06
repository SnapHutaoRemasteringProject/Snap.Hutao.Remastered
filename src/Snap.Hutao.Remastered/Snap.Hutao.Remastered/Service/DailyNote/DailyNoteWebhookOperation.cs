// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core.DependencyInjection.Annotation.HttpClient;
using Snap.Hutao.Remastered.Web.Hoyolab;
using Snap.Hutao.Remastered.Web.Request.Builder;
using Snap.Hutao.Remastered.Web.Request.Builder.Abstraction;
using System.Net.Http;
using WebDailyNote = Snap.Hutao.Remastered.Web.Hoyolab.Takumi.GameRecord.DailyNote.DailyNote;

namespace Snap.Hutao.Remastered.Service.DailyNote;

[HttpClient(HttpClientConfiguration.Default)]
internal sealed partial class DailyNoteWebhookOperation
{
    private readonly IHttpRequestMessageBuilderFactory httpRequestMessageBuilderFactory;
    private readonly DailyNoteOptions dailyNoteOptions;
    private readonly HttpClient httpClient;

    [GeneratedConstructor]
    public partial DailyNoteWebhookOperation(IServiceProvider serviceProvider, HttpClient httpClient);

    public void TryPostDailyNoteToWebhook(PlayerUid playerUid, WebDailyNote dailyNote)
    {
        string? targetUrl = dailyNoteOptions.WebhookUrl.Value;
        if (string.IsNullOrEmpty(targetUrl) || !Uri.TryCreate(targetUrl, UriKind.Absolute, out Uri? targetUri))
        {
            return;
        }

        HttpRequestMessageBuilder builder = httpRequestMessageBuilderFactory.Create()
            .SetRequestUri(targetUri)
            .SetHeader("x-uid", $"{playerUid}")
            .PostJson(dailyNote);

        builder.Send(httpClient);
    }
}