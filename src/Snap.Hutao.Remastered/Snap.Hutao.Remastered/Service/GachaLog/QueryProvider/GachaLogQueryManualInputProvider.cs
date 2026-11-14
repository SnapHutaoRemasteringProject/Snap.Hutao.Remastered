// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Factory.ContentDialog;
using Snap.Hutao.Remastered.UI.Xaml.View.Dialog;
using System.Collections.Specialized;
using System.Web;

namespace Snap.Hutao.Remastered.Service.GachaLog.QueryProvider;

[Service(ServiceLifetime.Transient, typeof(IGachaLogQueryProvider), Key = RefreshOptionKind.ManualInput)]
public sealed partial class GachaLogQueryManualInputProvider : IGachaLogQueryProvider
{
    private readonly IContentDialogFactory contentDialogFactory;
    private readonly IServiceProvider serviceProvider;
    private readonly CultureOptions cultureOptions;

    [GeneratedConstructor]
    public partial GachaLogQueryManualInputProvider(IServiceProvider serviceProvider);

    /// <inheritdoc/>
    public async ValueTask<ValueResult<bool, GachaLogQuery>> GetQueryAsync()
    {
        GachaLogUrlDialog dialog = await contentDialogFactory.CreateInstanceAsync<GachaLogUrlDialog>(serviceProvider).ConfigureAwait(false);
        if (await dialog.GetInputUrlAsync().ConfigureAwait(false) is not (true, { } url))
        {
            return new(false, default);
        }

        string queryString = ExtractQuery(url);
        NameValueCollection query = HttpUtility.ParseQueryString(queryString);
        if (query["auth_appid"] is not "webview_gacha")
        {
            return new(false, GachaLogQuery.Invalid(SH.ServiceGachaLogUrlProviderManualInputInvalid));
        }

        query.Set("lang", cultureOptions.LanguageCode);
        return new(true, new(query.ToString()!));
    }

    private static string ExtractQuery(string url)
    {
        ReadOnlySpan<char> span = url.AsSpan().Trim();

        int fragmentIndex = span.IndexOf('#');
        if (fragmentIndex >= 0)
        {
            span = span[..fragmentIndex];
        }

        int queryIndex = span.IndexOf('?');
        if (queryIndex >= 0)
        {
            span = span[queryIndex..];
        }

        int end = span.IndexOfAny(" \t\r\n");
        if (end >= 0)
        {
            span = span[..end];
        }

        return span.ToString();
    }
}
