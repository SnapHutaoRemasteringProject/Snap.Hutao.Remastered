using System.IO;
using Microsoft.Extensions.Logging;
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
        Assert.HasCount(0, fixture.Logger.Errors);
    }

    [TestMethod]
    public async Task RegisteringClearedSettingUsesDefaultWithoutReportingCorruption()
    {
        using SyntheticSettings fixture = new();
        PluginSettingService service = fixture.CreateService();
        await service.SetSettingAsync<string>("plugin", "name", null);

        PluginSettingService reopened = fixture.CreateService();
        reopened.RegisterSetting("plugin", "name", "fallback");

        Assert.IsTrue(reopened.TryGetCachedSetting<string>("plugin", "name", out string? cached));
        Assert.AreEqual("fallback", cached);
        Assert.HasCount(0, fixture.Logger.Errors);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task EmptyStringValueRemainsDistinctFromClearedSetting(bool runtimeGetter)
    {
        using SyntheticSettings fixture = new();
        await fixture.CreateService().SetSettingAsync("plugin", "name", string.Empty);
        PluginSettingService reopened = fixture.CreateService();

        object? actual = runtimeGetter
            ? await reopened.GetSettingAsync(typeof(string), "plugin", "name", "fallback")
            : await reopened.GetSettingAsync("plugin", "name", "fallback");

        Assert.AreEqual(string.Empty, actual);
        Assert.AreEqual("\"\"", File.ReadAllText(Path.Combine(fixture.Directory, "plugin", "name.json")));
        Assert.HasCount(0, fixture.Logger.Errors);
    }

    [TestMethod]
    [DataRow(false, "{")]
    [DataRow(true, "{")]
    [DataRow(false, " ")]
    [DataRow(true, " ")]
    public async Task NonemptyMalformedSettingStillReportsReadFailure(bool runtimeGetter, string json)
    {
        using SyntheticSettings fixture = new();
        string folder = Path.Combine(fixture.Directory, "plugin");
        System.IO.Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, "name.json"), json);
        PluginSettingService service = fixture.CreateService();

        object? actual = runtimeGetter
            ? await service.GetSettingAsync(typeof(string), "plugin", "name", "fallback")
            : await service.GetSettingAsync("plugin", "name", "fallback");

        Assert.AreEqual("fallback", actual);
        Assert.HasCount(1, fixture.Logger.Errors);
        Assert.IsInstanceOfType<JsonException>(fixture.Logger.Errors[0]);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task LockedSettingStillReportsReadFailure(bool runtimeGetter)
    {
        using SyntheticSettings fixture = new();
        string folder = Path.Combine(fixture.Directory, "plugin");
        System.IO.Directory.CreateDirectory(folder);
        string path = Path.Combine(folder, "name.json");
        File.WriteAllText(path, "\"old\"");
        PluginSettingService service = fixture.CreateService();
        using FileStream locked = File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);

        object? actual = runtimeGetter
            ? await service.GetSettingAsync(typeof(string), "plugin", "name", "fallback")
            : await service.GetSettingAsync("plugin", "name", "fallback");

        Assert.AreEqual("fallback", actual);
        Assert.HasCount(1, fixture.Logger.Errors);
        Assert.IsInstanceOfType<IOException>(fixture.Logger.Errors[0]);
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

        public RecordingLogger Logger { get; } = new();

        public string GetPluginsDirectory() => Directory;

        public PluginSettingService CreateService()
        {
            using ServiceProvider services = new ServiceCollection()
                .AddSingleton<IPluginService>(this)
                .AddSingleton<ILogger<PluginSettingService>>(Logger)
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

    private sealed class RecordingLogger : ILogger<PluginSettingService>
    {
        public List<Exception> Errors { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (logLevel == LogLevel.Error && exception is not null)
            {
                Errors.Add(exception);
            }
        }
    }
}
