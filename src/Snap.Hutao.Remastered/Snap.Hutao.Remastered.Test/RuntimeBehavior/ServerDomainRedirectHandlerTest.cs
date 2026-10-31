using Snap.Hutao.Remastered.Core.IO.Http;
using Snap.Hutao.Remastered.Web;
using System.Net;
using System.Net.Http;

namespace Snap.Hutao.Remastered.Test.RuntimeBehavior;

[TestClass]
public sealed class ServerDomainRedirectHandlerTest
{
    [TestMethod]
    [DataRow("/target", "https://example.test/target")]
    [DataRow("../target", "https://example.test/target")]
    [DataRow("?page=2", "https://example.test/base/start?page=2")]
    [DataRow("//other.test/target", "https://other.test/target")]
    [DataRow("https://other.test/absolute", "https://other.test/absolute")]
    public async Task ResolvesLocationAgainstCurrentRequest(string location, string expected)
    {
        using RecordingHandler terminal = new(HttpStatusCode.Redirect, location);
        using HttpClient client = CreateClient(terminal);

        using HttpResponseMessage response = await client.GetAsync("https://example.test/base/start");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        CollectionAssert.AreEqual(new[] { "https://example.test/base/start", expected }, terminal.Targets);
    }

    [TestMethod]
    public async Task ResolvesRelativeLocationBeforeBackupDomainRewrite()
    {
        using RecordingHandler terminal = new(HttpStatusCode.Redirect, "/target?source=primary");
        RecordingDomainService domain = new(true);
        using HttpClient client = CreateClient(terminal, domain);

        using HttpResponseMessage response = await client.GetAsync("https://api.snaphutaorp.org/base/start");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual(new Uri("https://api.snaphutaorp.org/target?source=primary"), domain.RewriteInput);
        CollectionAssert.AreEqual(new[] { "https://api.snaphutaorp.org/base/start", "https://api.hutaorp.org/target?source=primary" }, terminal.Targets);
    }

    [TestMethod]
    [DataRow(302, null)]
    [DataRow(200, "/target")]
    public async Task LeavesResponsesWithoutAnActionableRedirectUnchanged(int status, string? location)
    {
        using RecordingHandler terminal = new((HttpStatusCode)status, location);
        using HttpClient client = CreateClient(terminal);

        using HttpResponseMessage response = await client.GetAsync("https://example.test/base/start");

        Assert.AreEqual((HttpStatusCode)status, response.StatusCode);
        Assert.AreEqual(1, terminal.Targets.Count);
    }

    [TestMethod]
    public async Task StopsFollowingRelativeRedirectLoop()
    {
        using RecordingHandler terminal = new(HttpStatusCode.Redirect, "/loop", true);
        using HttpClient client = CreateClient(terminal);

        using HttpResponseMessage response = await client.GetAsync("https://example.test/loop");

        Assert.AreEqual(HttpStatusCode.Redirect, response.StatusCode);
        Assert.AreEqual(6, terminal.Targets.Count);
    }

    [TestMethod]
    public async Task ResolvesEachRelativeHopAgainstItsCurrentRequest()
    {
        using RecordingHandler terminal = new(HttpStatusCode.Redirect, "nested/step", nextLocation: "../final");
        using HttpClient client = CreateClient(terminal);

        using HttpResponseMessage response = await client.GetAsync("https://example.test/base/start");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        CollectionAssert.AreEqual(new[]
        {
            "https://example.test/base/start",
            "https://example.test/base/nested/step",
            "https://example.test/base/final",
        }, terminal.Targets);
    }
    private static HttpClient CreateClient(RecordingHandler terminal, RecordingDomainService? domain = null)
    {
        return new(new ServerDomainRedirectHandler(domain ?? new(false)) { InnerHandler = terminal });
    }

    private sealed class RecordingHandler(HttpStatusCode status, string? location, bool repeat = false, string? nextLocation = null) : HttpMessageHandler
    {
        public List<string> Targets { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request.RequestUri);
            Targets.Add(request.RequestUri.AbsoluteUri);
            HttpResponseMessage response = new(Targets.Count is 1 || repeat || (Targets.Count is 2 && nextLocation is not null) ? status : HttpStatusCode.OK);
            string? currentLocation = Targets.Count is 2 && nextLocation is not null ? nextLocation : location;
            if (currentLocation is not null)
            {
                response.Headers.Location = new(currentLocation, UriKind.RelativeOrAbsolute);
            }

            return Task.FromResult(response);
        }
    }

    private sealed class RecordingDomainService(bool backup) : IServerDomainService
    {
        public Uri? RewriteInput { get; private set; }

        public bool IsBackupMode() => backup;

        public Uri? RewriteHostToCurrentMode(Uri? uri)
        {
            RewriteInput = uri;
            return new Uri("https://api.hutaorp.org/target?source=primary");
        }
    }
}
