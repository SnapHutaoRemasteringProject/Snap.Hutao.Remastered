using Snap.Hutao.Remastered.Core.AI.IO;
using Snap.Hutao.Remastered.Core.AI.Threading;
using Snap.Hutao.Remastered.Service.AI;
using Snap.Hutao.Remastered.Service.AI.Models;
using Snap.Hutao.Remastered.Service.AI.Settings;
using System.Collections.Concurrent;
using System.IO;
using System.Reflection;

namespace Snap.Hutao.Remastered.Test.RuntimeBehavior;

[TestClass]
[DoNotParallelize]
public sealed class AgentSettingsWatchRaceTest
{
    [TestMethod]
    [DataRow("AfterRead")]
    [DataRow("DuringSave")]
    [DataRow("DuringLoad")]
    public void QueuedFileSnapshotCannotUndoACompleteModelRefresh(string timing)
    {
        string directory = Path.Combine(Path.GetTempPath(), "HutaoSettingsWatchRace", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string previousDirectory = WellKnownLocations.Settings;
        SynchronizationContext? previousContext = SynchronizationContext.Current;
        PropertyInfo contextProperty = typeof(AgentThreadingContext).GetProperty(nameof(AgentThreadingContext.Current))!;
        object? previousAgentContext = contextProperty.GetValue(null);
        QueuedContext context = new();
        JsonSettingsProvider<AgentSettings>? provider = null;
        try
        {
            WellKnownLocations.Settings = directory;
            SynchronizationContext.SetSynchronizationContext(context);
            contextProperty.SetValue(null, Activator.CreateInstance(typeof(AgentThreadingContext), nonPublic: true));
            AgentSettings initial = new();
            ModelProviderProfile profile = new();
            profile.ModelProfiles.Add(new() { ModelId = "model-0" });
            profile.ModelProfiles.MoveCurrentToFirst();
            initial.ModelProviderProfiles.Add(profile);
            initial.ModelProviderProfiles.MoveCurrentToFirst();
            string file = Path.Combine(directory, "test.json");
            File.WriteAllText(file, JsonSerializer.Serialize(initial, Snap.Hutao.Remastered.Core.Text.Json.AgentSettingsJsonContext.Default.AgentSettings));
            Assert.IsNotNull(JsonSerializer.Deserialize(File.ReadAllText(file), Snap.Hutao.Remastered.Core.Text.Json.AgentSettingsJsonContext.Default.AgentSettings));
            provider = new("test", new());
            StopWatching(provider);

            MethodInfo changed = typeof(JsonSettingsProvider<AgentSettings>).GetMethod("OnFileChanged", BindingFlags.Static | BindingFlags.NonPublic)!;
            Exception? reloadError = null;
            Thread reader = new(() =>
            {
                SynchronizationContext.SetSynchronizationContext(null);
                try { changed.Invoke(null, [provider]); }
                catch (Exception exception) { reloadError = exception; }
            });
            reader.Start();
            Assert.IsTrue(reader.Join(TimeSpan.FromSeconds(10)));
            Assert.IsNull(reloadError);
            StopWatching(provider);
            Assert.IsGreaterThan(0, context.PendingCount);

            ModelProviderProfile current = provider.CurrentValue.ModelProviderProfiles.CurrentItem!;
            ModelProfile[] fetched = Enumerable.Range(0, 178).Select(index => new ModelProfile { ModelId = $"model-{index}" }).ToArray();
            void RefreshModels()
            {
                current.ModelProfiles = ModelProfileMerger.CreateMerged(current.ModelProfiles, fetched);
            }
            if (timing == "DuringLoad")
            {
                AgentCredentialStore.BeforeLoad = () =>
                {
                    AgentCredentialStore.BeforeLoad = null;
                    RefreshModels();
                };
            }
            else
            {
                if (timing == "DuringSave") { AgentCredentialStore.BeforeSave = context.Drain; }
                RefreshModels();
            }
            context.Drain();

            Assert.HasCount(178, current.ModelProfiles);
            AgentSettings persisted = JsonSerializer.Deserialize(File.ReadAllText(file), Snap.Hutao.Remastered.Core.Text.Json.AgentSettingsJsonContext.Default.AgentSettings)!;
            Assert.HasCount(178, persisted.ModelProviderProfiles.Single().ModelProfiles);
        }
        finally
        {
            AgentCredentialStore.BeforeLoad = null;
            AgentCredentialStore.BeforeSave = null;
            provider?.Dispose();
            context.Drain();
            contextProperty.SetValue(null, previousAgentContext);
            SynchronizationContext.SetSynchronizationContext(previousContext);
            WellKnownLocations.Settings = previousDirectory;
            Directory.Delete(directory, true);
        }
    }

    private static void StopWatching(JsonSettingsProvider<AgentSettings> provider)
    {
        FieldInfo registration = typeof(JsonSettingsProvider<AgentSettings>).GetField("watchRegistration", BindingFlags.Instance | BindingFlags.NonPublic)!;
        ((IDisposable?)registration.GetValue(provider))?.Dispose();
        registration.SetValue(provider, null);
    }

    private sealed class QueuedContext : SynchronizationContext
    {
        private readonly ConcurrentQueue<(SendOrPostCallback Callback, object? State)> pending = new();

        public int PendingCount { get => pending.Count; }

        public override void Post(SendOrPostCallback callback, object? state)
        {
            pending.Enqueue((callback, state));
        }

        public void Drain()
        {
            while (pending.TryDequeue(out var item))
            {
                item.Callback(item.State);
            }
        }
    }
}
