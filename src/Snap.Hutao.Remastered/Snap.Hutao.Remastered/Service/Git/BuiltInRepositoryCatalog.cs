// Copyright (c) Snap Hutao RP. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core.Text.Json;
using Snap.Hutao.Remastered.Web.Hutao;
using Snap.Hutao.Remastered.Web.Hutao.Response;
using System.Collections.Immutable;

namespace Snap.Hutao.Remastered.Service.Git;

public static class BuiltInRepositoryCatalog
{
    // Snapshot supplied for emergency use only after the live catalogue fails.
    private const string Snapshot = """
        {"data":[
          {"Id":3,"name":"Snap.Metadata","friendly_name":"Gitcode","https_url":"https://gitcode.com/JWJUN233233/Snap.Metadata.git","web_url":"https://gitcode.com/JWJUN233233/Snap.Metadata/","type":"Public","CreatedAt":"2026-04-07T17:12:48","UpdatedAt":"2026-04-07T17:12:52"},
          {"Id":4,"name":"Snap.ContentDelivery","friendly_name":"Gitcode","https_url":"https://gitcode.com/JWJUN233233/Snap.ContentDelivery.git","web_url":"https://gitcode.com/JWJUN233233/Snap.ContentDelivery/","type":"Public","CreatedAt":"2007-01-06T17:04:46","UpdatedAt":"2006-05-10T15:47:46"},
          {"Id":8,"name":"Snap.Metadata","friendly_name":"CTCC","https_url":"http://cnccgit.snaphutaorp.org:19011/shadmin/Snap.Metadata.git","web_url":"http://cnccgit.snaphutaorp.org:19011/shadmin/Snap.Metadata.git","type":"Public","CreatedAt":"2015-09-20T08:51:00","UpdatedAt":"2020-02-15T13:31:22"},
          {"Id":9,"name":"Snap.ContentDelivery","friendly_name":"CTCC","https_url":"http://cnccgit.snaphutaorp.org:19011/shadmin/Snap.ContentDelivery.git","web_url":"http://cnccgit.snaphutaorp.org:19011/shadmin/Snap.ContentDelivery.git","type":"Public","CreatedAt":"2007-01-06T17:04:46","UpdatedAt":"2006-05-10T15:47:46"},
          {"Id":10,"name":"Snap.Metadata","friendly_name":"","https_url":"https://github.com/SnapHutaoRemasteringProject/Snap.Metadata.git","web_url":"https://github.com/SnapHutaoRemasteringProject/Snap.Metadata.git","type":"Public","CreatedAt":"2026-04-07T17:12:48","UpdatedAt":"2026-04-07T17:12:52"},
          {"Id":11,"name":"Snap.ContentDelivery","friendly_name":"","https_url":"https://github.com/SnapHutaoRemasteringProject/Snap.ContentDelivery.git","web_url":"https://github.com/SnapHutaoRemasteringProject/Snap.ContentDelivery.git","type":"Public","CreatedAt":"2007-01-06T17:04:46","UpdatedAt":"2006-05-10T15:47:46"}
        ],"retcode":0,"code":0,"message":"OK"}
        """;

    public static ImmutableArray<GitRepository> Create()
    {
        return JsonSerializer.Deserialize<HutaoResponse<ImmutableArray<GitRepository>>>(Snapshot, JsonOptions.Default)!.Data;
    }
}
