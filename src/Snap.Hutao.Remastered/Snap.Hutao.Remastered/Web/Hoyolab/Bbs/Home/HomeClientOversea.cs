// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core.DependencyInjection.Annotation.HttpClient;
using Snap.Hutao.Remastered.Core.ExceptionService;
using Snap.Hutao.Remastered.Web.Response;
using System.Net.Http;

namespace Snap.Hutao.Remastered.Web.Hoyolab.Bbs.Home;

[HttpClient(HttpClientConfiguration.XRpc3)]
internal sealed partial class HomeClientOversea : IHomeClient
{
    [GeneratedConstructor]
    public partial HomeClientOversea(IServiceProvider serviceProvider, HttpClient httpClient);

    public ValueTask<Response<NewHomeNewInfo>> GetNewHomeInfoAsync(int gid, CancellationToken token = default)
    {
        return ValueTask.FromException<Response<NewHomeNewInfo>>(HutaoException.NotSupported());
    }
}