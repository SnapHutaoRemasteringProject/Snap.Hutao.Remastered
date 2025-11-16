// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Web.Hoyolab;

[SuppressMessage("", "SA1310")]
internal static class CookieExtension
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
            return source.TryGetValuesToCookie([Cookie.MID, Cookie.STOKEN, Cookie.STUID], out cookie);
        }

        public bool TryGetLToken([NotNullWhen(true)] out Cookie? cookie)
        {
            return source.TryGetValuesToCookie([Cookie.LTOKEN, Cookie.LTUID], out cookie);
        }

        public bool TryGetCookieToken([NotNullWhen(true)] out Cookie? cookie)
        {
            return source.TryGetValuesToCookie([Cookie.ACCOUNT_ID, Cookie.COOKIE_TOKEN], out cookie);
        }

        public bool TryGetDeviceFp([NotNullWhen(true)] out string? deviceFp)
        {
            return source.TryGetValue(DEVICEFP, out deviceFp);
        }

        private bool TryGetValuesToCookie(in ReadOnlySpan<string> keys, [NotNullWhen(true)] out Cookie? cookie)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(keys.Length);
            SortedDictionary<string, string> cookieMap = [];

            foreach (ref readonly string key in keys)
            {
                if (source.TryGetValue(key, out string? value))
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