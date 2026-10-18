using Snap.Hutao.Remastered.Web.Request.Builder;
using System.Net;
using System.Net.Http;
using System.Text;

namespace Snap.Hutao.Remastered.Test.RuntimeBehavior;

[TestClass]
public sealed class HttpRequestMessageSnapshotTest
{
    [TestMethod]
    public async Task ReplayAfterOriginalDisposalPreservesTheBodyAndRequestMetadata()
    {
        HttpRequestOptionsKey<bool> option = new("DisableRetry");
        HttpRequestMessage original = new(HttpMethod.Post, "https://example.test/record?role=1")
        {
            Version = HttpVersion.Version20,
            VersionPolicy = HttpVersionPolicy.RequestVersionOrLower,
            Content = new StringContent("{\"角色\":1}", Encoding.UTF8, "application/json"),
        };
        original.Headers.TryAddWithoutValidation("x-rpc-challenge", "verified");
        original.Options.Set(option, true);
        HttpRequestMessageSnapshot snapshot = await HttpRequestMessageSnapshot.CreateAsync(original);
        original.Dispose();

        using HttpRequestMessage replay = snapshot.CreateRequest();
        Assert.AreEqual(HttpMethod.Post, replay.Method);
        Assert.AreEqual(new Uri("https://example.test/record?role=1"), replay.RequestUri);
        Assert.AreEqual(HttpVersion.Version20, replay.Version);
        Assert.AreEqual(HttpVersionPolicy.RequestVersionOrLower, replay.VersionPolicy);
        Assert.AreEqual("verified", replay.Headers.GetValues("x-rpc-challenge").Single());
        Assert.IsTrue(replay.Options.TryGetValue(option, out bool disabled) && disabled);
        Assert.AreEqual("{\"角色\":1}", await replay.Content!.ReadAsStringAsync());
        Assert.AreEqual("application/json; charset=utf-8", replay.Content.Headers.ContentType!.ToString());
    }

    [TestMethod]
    public async Task EachReplayCanBeSentAndDisposedIndependently()
    {
        using HttpRequestMessage original = new(HttpMethod.Post, "https://example.test/record") { Content = new StringContent("payload") };
        HttpRequestMessageSnapshot snapshot = await HttpRequestMessageSnapshot.CreateAsync(original);
        using RecordingHandler handler = new();
        using HttpClient client = new(handler);
        for (int attempt = 0; attempt < 3; attempt++)
        {
            using HttpRequestMessage replay = snapshot.CreateRequest();
            replay.Headers.TryAddWithoutValidation("x-rpc-challenge", attempt.ToString());
            using HttpResponseMessage response = await client.SendAsync(replay);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        }

        CollectionAssert.AreEqual(new[] { "payload", "payload", "payload" }, handler.Bodies);
        using HttpRequestMessage fresh = snapshot.CreateRequest();
        Assert.IsFalse(fresh.Headers.Contains("x-rpc-challenge"));
    }

    [TestMethod]
    public async Task GetReplayDoesNotInventARequestBody()
    {
        using HttpRequestMessage original = new(HttpMethod.Get, "https://example.test/download");
        HttpRequestMessageSnapshot snapshot = await HttpRequestMessageSnapshot.CreateAsync(original);
        using HttpRequestMessage replay = snapshot.CreateRequest();
        Assert.IsNull(replay.Content);
        Assert.AreEqual(HttpMethod.Get, replay.Method);
    }

    [TestMethod]
    public async Task CancellationIsHonoredBeforeCapturingTheRequest()
    {
        using HttpRequestMessage original = new(HttpMethod.Get, "https://example.test/record");
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();
        await Assert.ThrowsExactlyAsync<OperationCanceledException>(async () => await HttpRequestMessageSnapshot.CreateAsync(original, cancellation.Token));
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public List<string> Bodies { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // No network: the real HttpClient still enforces send-once semantics.
            Bodies.Add(await request.Content!.ReadAsStringAsync(cancellationToken));
            return new(HttpStatusCode.OK);
        }
    }
}
