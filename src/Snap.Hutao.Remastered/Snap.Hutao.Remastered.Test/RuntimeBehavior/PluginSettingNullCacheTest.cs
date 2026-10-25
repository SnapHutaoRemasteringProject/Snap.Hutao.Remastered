using System.IO;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Snap.Hutao.Remastered.Service.Plugin;

namespace Snap.Hutao.Remastered.Test.RuntimeBehavior;

[TestClass]
[DoNotParallelize]
public sealed class PluginSettingNullCacheTest
{
    [TestMethod]
    [DataRow(false, false, "fallback")]
    [DataRow(false, true, "fallback")]
    [DataRow(true, false, "fallback")]
    [DataRow(true, true, "fallback")]
    [DataRow(false, false, null)]
    [DataRow(false, true, null)]
    [DataRow(true, false, null)]
    [DataRow(true, true, null)]
    public async Task ClearingStringSettingMatchesReopenedService(bool runtimeSetter, bool runtimeGetter, string? defaultValue)
    {
        using SyntheticSettings fixture = new();
        PluginSettingService service = fixture.CreateService();
        await service.SetSettingAsync("plugin", "name", "old");
        await service.SetSettingAsync("plugin", "other", "preserved");

        if (runtimeSetter)
        {
            await service.SetSettingAsync(typeof(string), "plugin", "name", null);
        }
        else
        {
            await service.SetSettingAsync<string>("plugin", "name", null);
        }

        Assert.IsFalse(service.TryGetCachedSetting<string>("plugin", "name", out _));
        Assert.IsTrue(service.TryGetCachedSetting<string>("plugin", "other", out string? other));
        Assert.AreEqual("preserved", other);
        Assert.AreEqual(string.Empty, File.ReadAllText(Path.Combine(fixture.Directory, "plugin", "name.json")));

        object? current = runtimeGetter
            ? await service.GetSettingAsync(typeof(string), "plugin", "name", defaultValue)
            : await service.GetSettingAsync("plugin", "name", defaultValue);
        PluginSettingService reopened = fixture.CreateService();
        object? persisted = runtimeGetter
            ? await reopened.GetSettingAsync(typeof(string), "plugin", "name", defaultValue)
            : await reopened.GetSettingAsync("plugin", "name", defaultValue);
        Assert.AreEqual(defaultValue, persisted);
        Assert.AreEqual(persisted, current);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task FailedNullWritePreservesCachedValue(bool runtimeSetter)
    {
        using SyntheticSettings fixture = new();
        PluginSettingService service = fixture.CreateService();
        await service.SetSettingAsync("plugin", "name", "old");
        string path = Path.Combine(fixture.Directory, "plugin", "name.json");
        File.Delete(path);
        System.IO.Directory.CreateDirectory(path);

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => runtimeSetter
            ? service.SetSettingAsync(typeof(string), "plugin", "name", null)
            : service.SetSettingAsync<string>("plugin", "name", null));

        Assert.IsTrue(service.TryGetCachedSetting<string>("plugin", "name", out string? cached));
        Assert.AreEqual("old", cached);
    }

    private sealed class SyntheticSettings : IPluginService, IDisposable
    {
        public string Directory { get; } = Path.Combine(Path.GetTempPath(), "HutaoPluginSettingTests", Guid.NewGuid().ToString("N"));

        public string GetPluginsDirectory() => Directory;

        public PluginSettingService CreateService()
        {
            using ServiceProvider services = new ServiceCollection()
                .AddSingleton<IPluginService>(this)
                .AddSingleton<ILogger<PluginSettingService>>(NullLogger<PluginSettingService>.Instance)
                .BuildServiceProvider();
            return new PluginSettingService(services);
        }

        public void Dispose()
        {
            if (System.IO.Directory.Exists(Directory))
            {
                System.IO.Directory.Delete(Directory, true);
            }
        }
    }
}
