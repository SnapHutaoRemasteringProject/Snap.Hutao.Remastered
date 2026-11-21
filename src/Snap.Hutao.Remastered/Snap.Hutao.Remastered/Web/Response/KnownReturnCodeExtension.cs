// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.Web.Response;

public static class KnownReturnCodeExtension
{
    public static bool IsAuthenticationFailure(this KnownReturnCode returnCode)
    {
        return returnCode is
            KnownReturnCode.PleaseLogin or
            KnownReturnCode.RET_TOKEN_INVALID or
            KnownReturnCode.LoginStateInvalid or
            KnownReturnCode.LoginDataOutdated;
    }
}
