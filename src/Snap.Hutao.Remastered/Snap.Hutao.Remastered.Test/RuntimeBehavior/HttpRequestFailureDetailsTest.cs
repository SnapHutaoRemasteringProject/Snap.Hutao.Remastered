// Copyright (c) Snap Hutao RP. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Web.Request.Builder;
using System.Net;
using System.Net.Http;

namespace Snap.Hutao.Remastered.Test.RuntimeBehavior;

[TestClass]
public sealed class HttpRequestFailureDetailsTest
{
    [TestMethod]
    public void JsonFailureKeepsLocationAndStageWithoutCredentialsOrPayloads()
    {
        JsonException json = new("response-secret", "$.data.cookie_token", 2, 17);
        Exception error = new InvalidOperationException("outer-secret", json);
        using HttpResponseMessage response = new(HttpStatusCode.OK) { Content = new StringContent("body-secret") };
        response.Headers.TryAddWithoutValidation("Set-Cookie", "cookie-secret");

        string text = HttpRequestFailureDetails.Format(error, typeof(Dictionary<string, string>),
            new Uri("https://user-secret:password-secret@example.invalid/account?stoken=query-secret#fragment-secret"), response, "deserialize");

        Assert.Contains("Stage: deserialize", text);
        Assert.Contains("System.Text.Json.JsonException", text);
        Assert.Contains("JSON path: $.data.cookie_token", text);
        Assert.Contains("HTTP: 200", text);
        Assert.Contains("https://example.invalid/account", text);
        foreach (string secret in new[] { "response-secret", "outer-secret", "body-secret", "cookie-secret", "user-secret", "password-secret", "query-secret", "fragment-secret" })
        {
            Assert.DoesNotContain(secret, text);
        }
    }

    [TestMethod]
    public void TransportFailureIsDistinguishedFromDeserialization()
    {
        string text = HttpRequestFailureDetails.Format(new HttpRequestException("private request data"), typeof(string),
            new Uri("https://example.invalid/account"), null, "send");

        Assert.Contains("Stage: send", text);
        Assert.Contains("System.Net.Http.HttpRequestException", text);
        Assert.DoesNotContain("private request data", text);
        Assert.DoesNotContain("Stage: deserialize", text);
    }

    [TestMethod]
    public void MissingMetadataFailureKeepsItsOriginalExceptionCategory()
    {
        string text = HttpRequestFailureDetails.Format(new NotSupportedException("private payload"), typeof(Dictionary<string, int>), null, null, "deserialize");

        Assert.Contains("System.NotSupportedException", text);
        Assert.Contains(typeof(Dictionary<string, int>).FullName!, text);
        Assert.DoesNotContain("private payload", text);
    }
}
