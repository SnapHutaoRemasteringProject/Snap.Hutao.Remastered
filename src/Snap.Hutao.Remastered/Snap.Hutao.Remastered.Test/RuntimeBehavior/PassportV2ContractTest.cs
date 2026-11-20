using Snap.Hutao.Remastered.Web.Hoyolab;
using Snap.Hutao.Remastered.Web.Hoyolab.Passport;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Snap.Hutao.Remastered.Test.RuntimeBehavior;

[TestClass]
public sealed partial class PassportV2ContractTest
{
    private static readonly WireContext Wire = new(new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping });

    [TestMethod]
    [DataRow(2)]
    [DataRow(4)]
    public void ExchangeSerializesTheExactBodyUsedForSigning(int destination)
    {
        PassportTokenExchange.Request request = new((PassportTokenExchange.TokenKind)destination, new("test-token=", PassportTokenExchange.TokenKind.SToken), "test-mid");
        string body = PassportTokenExchange.SerializeBody(request, Wire.ExchangeRequest);

        Assert.AreEqual($$"""{"dst_token_type":{{destination}},"src_token":{"token":"test-token\u003d","token_type":1},"mid":"test-mid"}""", body);
    }

    [TestMethod]
    [DataRow("""{"token":{"token":"test-token","token_type":2},"user_info":{"aid":101}}""", true)]
    [DataRow("""{"token":{"token":"test-token","token_type":2},"user_info":{"aid":"101"}}""", true)]
    [DataRow("""{"token":{"token":"test-token","token_type":2}}""", true)]
    [DataRow("""{"token":{"token":"test-token","token_type":4},"user_info":{"aid":101}}""", false)]
    [DataRow("""{"token":{"token":"test-token","token_type":2},"user_info":{"aid":102}}""", false)]
    [DataRow("""{"token":{"token":"","token_type":2}}""", false)]
    public void ExchangeOnlyAcceptsAUsableCredentialForTheRequestedAccount(string json, bool matches)
    {
        PassportTokenExchange.Result? response = JsonSerializer.Deserialize(json, Wire.ExchangeResult);
        Assert.IsNotNull(response);
        Assert.AreEqual(matches, response.Matches(PassportTokenExchange.TokenKind.LToken, "101"));
    }

    [TestMethod]
    public void PastedV2CookieRecognizesAccountAliasesAndPreservesTokenPadding()
    {
        Cookie raw = Cookie.Parse("stoken = test-stoken=\r\nltuid_v2=101;ltmid_v2=test-mid;ltoken_v2=test-ltoken;cookie_token_v2=test-ctoken");
        Assert.IsTrue(raw.TryGetSToken(out Cookie? sToken));
        Assert.AreEqual("test-stoken=", sToken?[Cookie.STOKEN]);
        Assert.AreEqual("101", sToken?[Cookie.STUID]);
        Assert.AreEqual("test-mid", sToken?[Cookie.MID]);
        Assert.IsTrue(raw.TryGetLToken(false, out Cookie? lToken));
        Assert.AreEqual("test-ltoken", lToken?[Cookie.LTOKEN_V2]);
        Assert.IsTrue(raw.TryGetCookieToken(false, out Cookie? cookieToken));
        Assert.AreEqual("test-mid", cookieToken?[Cookie.ACCOUNT_MID_V2]);
    }

    [TestMethod]
    public void ConflictingAccountIdentityDoesNotReuseImportedV2Credentials()
    {
        Cookie raw = Cookie.Parse("stuid=101;mid=test-mid;stoken=test-stoken;ltuid_v2=102;ltoken_v2=other-ltoken;account_mid_v2=other-mid;cookie_token_v2=other-ctoken");
        Assert.IsFalse(raw.TryGetLToken(false, out _));
        Assert.IsFalse(raw.TryGetCookieToken(false, out _));
    }

    [JsonSerializable(typeof(PassportTokenExchange.Request), TypeInfoPropertyName = "ExchangeRequest")]
    [JsonSerializable(typeof(PassportTokenExchange.Result), TypeInfoPropertyName = "ExchangeResult")]
    private sealed partial class WireContext : JsonSerializerContext;
}
