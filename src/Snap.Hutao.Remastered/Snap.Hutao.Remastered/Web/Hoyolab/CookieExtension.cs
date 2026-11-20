// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.Web.Hoyolab;

[SuppressMessage("", "SA1310")]
public static class CookieExtension
{
    private const string DEVICEFP = "DEVICEFP";

    extension(Cookie source)
    {
        public bool TryGetLoginTicket([NotNullWhen(true)] out Cookie? cookie)
        {
            return source.TryGetValuesToCookie([Cookie.LOGIN_TICKET, Cookie.LOGIN_UID], out cookie);
        }

        public bool TryGetSToken([NotNullWhen(true)] out Cookie? cookie)
        {
            string? accountId = source.GetAccountId();
            string? mid = source.GetAccountMid();
            string? token = source.GetValueOrDefault(Cookie.STOKEN);
            if (string.IsNullOrWhiteSpace(accountId) || string.IsNullOrWhiteSpace(mid) || string.IsNullOrWhiteSpace(token))
            {
                cookie = null;
                return false;
            }

            cookie = new() { [Cookie.STUID] = accountId, [Cookie.MID] = mid, [Cookie.STOKEN] = token };
            return true;
        }

        public bool TryGetLToken(bool isOversea, [NotNullWhen(true)] out Cookie? cookie)
        {
            return isOversea
                ? source.TryGetValuesToCookie([Cookie.LTOKEN, Cookie.LTUID], out cookie)
                : source.TryGetV2Token(Cookie.LTOKEN_V2, Cookie.LTUID_V2, Cookie.LTMID_V2, out cookie);
        }

        public bool TryGetCookieToken(bool isOversea, [NotNullWhen(true)] out Cookie? cookie)
        {
            return isOversea
                ? source.TryGetValuesToCookie([Cookie.ACCOUNT_ID, Cookie.COOKIE_TOKEN], out cookie)
                : source.TryGetV2Token(Cookie.COOKIE_TOKEN_V2, Cookie.ACCOUNT_ID_V2, Cookie.ACCOUNT_MID_V2, out cookie);
        }

        public string? GetAccountId() => source.GetValueOrDefault(Cookie.STUID)
            ?? source.GetValueOrDefault(Cookie.LTUID_V2)
            ?? source.GetValueOrDefault(Cookie.ACCOUNT_ID_V2);

        public string? GetAccountMid() => source.GetValueOrDefault(Cookie.MID)
            ?? source.GetValueOrDefault(Cookie.LTMID_V2)
            ?? source.GetValueOrDefault(Cookie.ACCOUNT_MID_V2);

        public bool TryGetDeviceFp([NotNullWhen(true)] out string? deviceFp)
        {
            return source.TryGetValue(DEVICEFP, out deviceFp);
        }

        private bool TryGetV2Token(string tokenKey, string accountIdKey, string midKey, [NotNullWhen(true)] out Cookie? cookie)
        {
            string? token = source.GetValueOrDefault(tokenKey);
            string? accountId = source.GetAccountId();
            string? mid = source.GetAccountMid();
            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(accountId) || string.IsNullOrWhiteSpace(mid)
                || (source.TryGetValue(accountIdKey, out string? tokenAccountId) && !string.Equals(tokenAccountId, accountId, StringComparison.Ordinal))
                || (source.TryGetValue(midKey, out string? tokenMid) && !string.Equals(tokenMid, mid, StringComparison.Ordinal)))
            {
                cookie = null;
                return false;
            }

            cookie = new() { [tokenKey] = token, [accountIdKey] = accountId, [midKey] = mid };
            return true;
        }

        private bool TryGetValuesToCookie(in ReadOnlySpan<string> keys, [NotNullWhen(true)] out Cookie? cookie)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(keys.Length);
            SortedDictionary<string, string> cookieMap = [];

            foreach (ref readonly string key in keys)
            {
                if (source.TryGetValue(key, out string? value) && !string.IsNullOrWhiteSpace(value))
                {
                    cookieMap.TryAdd(key, value);
                }
                else
                {
                    cookie = default;
                    return false;
                }
            }

            cookie = new(cookieMap);
            return true;
        }
    }
}
