// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using System.Text;

namespace Snap.Hutao.Remastered.Web.Hoyolab;

public sealed partial class Cookie
{
    private readonly SortedDictionary<string, string> inner;

    public Cookie()
        : this([])
    {
    }

    public Cookie(SortedDictionary<string, string> dict)
    {
        inner = dict;
    }

    public string this[string key]
    {
        get => inner[key];
        set => inner[key] = value;
    }

    public static Cookie Parse(string cookieString)
    {
        SortedDictionary<string, string> cookieMap = [];
        string[] values = cookieString.Split([';', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (string[] parts in values.Select(c => c.Split('=', 2)))
        {
            string name = parts[0].Trim();
            string value = parts.Length == 1 ? string.Empty : parts[1].Trim();
            if (name.Length > 0)
            {
                cookieMap.TryAdd(name, value);
            }
        }

        return new(cookieMap);
    }

    public static Cookie FromSToken(string stuid, string stoken)
    {
        SortedDictionary<string, string> cookieMap = new()
        {
            [STUID] = stuid,
            [STOKEN] = stoken,
        };

        return new(cookieMap);
    }

    public bool IsEmpty()
    {
        return inner.Count <= 0;
    }

    public static Cookie FromLToken(string accountId, string mid, string token, bool isOversea)
    {
        return isOversea
            ? new() { [LTUID] = accountId, [LTOKEN] = token }
            : new() { [LTUID_V2] = accountId, [LTMID_V2] = mid, [LTOKEN_V2] = token };
    }

    public static Cookie FromCookieToken(string accountId, string mid, string token, bool isOversea)
    {
        return isOversea
            ? new() { [ACCOUNT_ID] = accountId, [COOKIE_TOKEN] = token }
            : new() { [ACCOUNT_ID_V2] = accountId, [ACCOUNT_MID_V2] = mid, [COOKIE_TOKEN_V2] = token };
    }

    public bool TryGetValue(string key, [NotNullWhen(true)] out string? value)
    {
        return inner.TryGetValue(key, out value);
    }

    public string? GetValueOrDefault(string key)
    {
        return inner.GetValueOrDefault(key);
    }

    public override string ToString()
    {
        StringBuilder resultBuilder = new();

        IEnumerator<KeyValuePair<string, string>> enumerator = inner.GetEnumerator();
        if (!enumerator.MoveNext())
        {
            return string.Empty;
        }

        KeyValuePair<string, string> first = enumerator.Current;
        resultBuilder.Append(first.Key).Append('=').Append(first.Value);

        if (!enumerator.MoveNext())
        {
            return resultBuilder.ToString();
        }

        do
        {
            resultBuilder.Append(';');
            KeyValuePair<string, string> current = enumerator.Current;
            resultBuilder.Append(current.Key).Append('=').Append(current.Value);
        }
        while (enumerator.MoveNext());

        return resultBuilder.ToString();
    }
}
