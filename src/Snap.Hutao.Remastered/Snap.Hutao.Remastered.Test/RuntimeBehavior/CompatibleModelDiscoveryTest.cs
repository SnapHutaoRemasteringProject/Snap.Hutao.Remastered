using Snap.Hutao.Remastered.Service.AI;
using Snap.Hutao.Remastered.Service.AI.Models;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Snap.Hutao.Remastered.Test.RuntimeBehavior;

[TestClass]
public sealed class CompatibleModelDiscoveryTest
{
    [TestMethod]
    public async Task DiscoversQwenStyleModelsWithoutAccessingPrivateSdkMetadata()
    {
        using TcpListener listener = new(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        string? requestLine = null;
        Task server = Task.Run(async () =>
        {
            using TcpClient connection = await listener.AcceptTcpClientAsync();
            using NetworkStream stream = connection.GetStream();
            using StreamReader reader = new(stream, Encoding.ASCII, false, 1024, true);
            requestLine = await reader.ReadLineAsync();
            while (await reader.ReadLineAsync() is { Length: > 0 }) { }
            const string payload = """
                {"object":"list","data":[{"id":"qwen3.8-flash","object":"model","created":0,"owned_by":"system","status":{"provider_specific":"available"}},{"id":"qwen-plus","object":"model","created":0,"owned_by":"system"}]}
                """;
            byte[] body = Encoding.UTF8.GetBytes(payload);
            byte[] headers = Encoding.ASCII.GetBytes($"HTTP/1.1 200 OK\r\nContent-Type: application/json\r\nContent-Length: {body.Length}\r\nConnection: close\r\n\r\n");
            await stream.WriteAsync(headers);
            await stream.WriteAsync(body);
            await stream.FlushAsync();
        });
        try
        {
            ModelProviderProfile provider = new() { Endpoint = $"http://127.0.0.1:{port}/compatible-mode/v1", ApiKey = "test-only" };
            IReadOnlyList<ModelProfile> models = await new ModelProfileService().GetModelsAsync(provider, CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(10));
            await server.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.HasCount(2, models);
            Assert.AreEqual("qwen3.8-flash", models[0].ModelId);
            Assert.AreEqual("qwen3.8-flash", models[0].DisplayName);
            Assert.IsTrue(requestLine?.StartsWith("GET /compatible-mode/v1/models ", StringComparison.Ordinal));
        }
        finally { listener.Stop(); }
    }
}
