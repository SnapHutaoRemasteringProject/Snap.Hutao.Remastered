using System.IO;
using System.Reflection;
using Snap.Hutao.Remastered.Service.AI.Settings;
using Snap.Hutao.Remastered.Core.AI.IO;
using Snap.Hutao.Remastered.Core.AI.Threading;

namespace Snap.Hutao.Remastered.Test.RuntimeBehavior;

[TestClass]
[DoNotParallelize]
public sealed class AgentSettingsRecoveryTest
{
    [TestMethod]
    [DataRow("{")]
    [DataRow("null")]
    public void FailedInitialLoadPreservesFileAndCredentials(string original)
    {
        WithDirectory(path =>
        {
            File.WriteAllText(path, original);
            using var provider = new JsonSettingsProvider<AgentSettings>("test", new());
            provider.CurrentValue.AgentOptions.UserName = "edited after failed load";
            Assert.AreEqual(original, File.ReadAllText(path));
            Assert.AreEqual(0, AgentCredentialStore.SaveCalls);
        });
    }

    [TestMethod]
    public void TemporarilyLockedSettingsCannotBeOverwritten()
    {
        WithDirectory(path =>
        {
            File.WriteAllText(path, "{}");
            using (FileStream locked = File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            using (var provider = new JsonSettingsProvider<AgentSettings>("test", new()))
            {
                provider.CurrentValue.AgentOptions.UserName = "edited while locked";
                Assert.AreEqual(0, AgentCredentialStore.SaveCalls);
            }
            Assert.AreEqual("{}", File.ReadAllText(path));
        });
    }

    [TestMethod]
    public void SuccessfulRecoveryAllowsSavingAgain()
    {
        WithDirectory(path =>
        {
            File.WriteAllText(path, "{");
            using var provider = new JsonSettingsProvider<AgentSettings>("test", new());
            provider.CurrentValue.AgentOptions.UserName = "ignored";
            File.WriteAllText(path, JsonSerializer.Serialize(new AgentSettings()));
            typeof(JsonSettingsProvider<AgentSettings>).GetMethod("OnFileChanged", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, [provider]);
            provider.CurrentValue.AgentOptions.UserName = "recovered";
            Assert.AreEqual("recovered", JsonSerializer.Deserialize<AgentSettings>(File.ReadAllText(path))!.AgentOptions.UserName);
            Assert.IsGreaterThan(0, AgentCredentialStore.SaveCalls);
        });
    }

    private static void WithDirectory(Action<string> test)
    {
        string directory = Path.Combine(Path.GetTempPath(), "HutaoAgentSettingsTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        WellKnownLocations.Settings = directory;
        AgentCredentialStore.SaveCalls = 0;
        SynchronizationContext? previous = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(TestContext);
        AgentThreadingContext.Initialize();
        try { test(Path.Combine(directory, "test.json")); }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previous);
            Directory.Delete(directory, true);
        }
    }

    private static readonly SynchronizationContext TestContext = new ImmediateContext();
    private sealed class ImmediateContext : SynchronizationContext
    {
        public override void Post(SendOrPostCallback callback, object? state) => callback(state);
    }
}
