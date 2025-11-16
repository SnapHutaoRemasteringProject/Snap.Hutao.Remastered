// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Web.Hoyolab.HoyoPlay.Connect.Branch;

namespace Snap.Hutao.Web.Endpoint.Hoyolab;

internal static class ApiEndpointsExtension
{
    extension(IApiEndpoints apiEndpoints)
    {
        public string SophonChunkGetBuildByBranch(BranchWrapper wrapper)
        {
            return string.Equals(wrapper.Branch, "PREDOWNLOAD", StringComparison.OrdinalIgnoreCase)
                ? apiEndpoints.SophonChunkGetBuildNoTag(wrapper)
                : apiEndpoints.SophonChunkGetBuild(wrapper);
        }
    }
}