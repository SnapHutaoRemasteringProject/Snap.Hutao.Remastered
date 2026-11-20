// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using System.Text.Json.Serialization.Metadata;

namespace Snap.Hutao.Remastered.Web.Hoyolab.Passport;

internal static class PassportTokenExchange
{
    internal static string SerializeBody<T>(T data, JsonTypeInfo<T> typeInfo)
    {
        // Sign the exact JSON sent to Passport, including its lowercase escapes.
        return JsonSerializer.Serialize(data, typeInfo)
            .Replace("<", "\\u003c", StringComparison.Ordinal)
            .Replace(">", "\\u003e", StringComparison.Ordinal)
            .Replace("&", "\\u0026", StringComparison.Ordinal)
            .Replace("=", "\\u003d", StringComparison.Ordinal)
            .Replace("'", "\\u0027", StringComparison.Ordinal);
    }

    internal enum TokenKind
    {
        SToken = 1,
        LToken = 2,
        CookieToken = 4,
    }

    internal sealed record Request(
        [property: JsonPropertyName("dst_token_type")] TokenKind Destination,
        [property: JsonPropertyName("src_token")] Token Source,
        [property: JsonPropertyName("mid")] string Mid);

    internal sealed record Token(
        [property: JsonPropertyName("token")] string? Value,
        [property: JsonPropertyName("token_type")] TokenKind? Kind);

    internal sealed record Result(
        [property: JsonPropertyName("token")] ReturnedToken? Token,
        [property: JsonPropertyName("user_info")] Account? Account)
    {
        internal bool Matches(TokenKind destination, string accountId)
        {
            string? returnedAid = Account?.Aid.ToString();
            if (string.IsNullOrEmpty(returnedAid))
            {
                returnedAid = Account?.Uid.ToString();
            }

            return Token is { } token
                && !string.IsNullOrWhiteSpace(token.Value)
                && (token.Kind is null || token.Kind == (int)destination)
                && (string.IsNullOrEmpty(returnedAid) || string.Equals(accountId, returnedAid, StringComparison.Ordinal));
        }
    }

    internal sealed record ReturnedToken(
        [property: JsonPropertyName("token")] string? Value,
        [property: JsonPropertyName("token_type"), JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)] int? Kind);

    internal sealed record Account(
        [property: JsonPropertyName("aid")] JsonElement Aid,
        [property: JsonPropertyName("uid")] JsonElement Uid);
}
