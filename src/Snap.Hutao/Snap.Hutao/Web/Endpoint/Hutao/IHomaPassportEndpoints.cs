// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Web.Endpoint.Hutao;

internal interface IHomaPassportEndpoints : IHomaRootAccess
{
    string PassportVerify()
    {
        return $"{Root}/Passport/v2/Verify";
    }

    string PassportRegister()
    {
        return $"{Root}/Passport/v2/Register";
    }

    string PassportCancel()
    {
        return $"{Root}/Passport/v2/Cancel";
    }

    string PassportResetUserName()
    {
        return $"{Root}/Passport/v2/ResetUsername";
    }

    string PassportResetPassword()
    {
        return $"{Root}/Passport/v2/ResetPassword";
    }

    string PassportLogin()
    {
        return $"{Root}/Passport/v2/Login";
    }

    string PassportUserInfo()
    {
        return $"{Root}/Passport/v2/UserInfo";
    }

    string PassportRefreshToken()
    {
        return $"{Root}/Passport/v2/RefreshToken";
    }

    string PassportRevokeToken()
    {
        return $"{Root}/Passport/v2/RevokeToken";
    }

    string PassportRevokeAllTokens()
    {
        return $"{Root}/Passport/v2/RevokeAllTokens";
    }
}