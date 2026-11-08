// Copyright (c) 2026 DGP Studio. All rights reserved.
// Licensed under the MIT license.
// Adapted from Snap.Nicole.

using Microsoft.Extensions.FileProviders;
using Snap.Hutao.Remastered.Core.AI;
using Snap.Hutao.Remastered.Core.AI.IO;
using Snap.Hutao.Remastered.Core.AI.Threading;
using Snap.Hutao.Remastered.Core.Logging;
using Snap.Hutao.Remastered.Service.AI.Migration;
using Snap.Hutao.Remastered.Core.Setting;
using System.IO;
using System.Reflection;

namespace Snap.Hutao.Remastered.Service.AI.Settings;

// A single change in the options can trigger the whole object graph to be serialized and written to disk,
// so it's recommended to use this provider only for relatively small options objects and avoid putting large data in them.
internal class JsonSettingsProvider<TSettings> : ISettingsProvider<TSettings>, IDisposable
    where TSettings : class, INotifyPropertyChanged, ICopyFrom<TSettings>, new()
{
    private const int LoadRetryCount = 3;
    private static readonly TimeSpan LoadRetryDelay = TimeSpan.FromMilliseconds(50);

    private readonly Lock syncRoot = new();

    private readonly string filePath;
    private readonly string fileName;
    private readonly JsonSerializerOptions jsonOptions;

    private readonly PhysicalFileProvider fileProvider;
    private readonly ObservableObjectHierarchyObserver<TSettings, JsonSettingsProvider<TSettings>> changeObserver;

    private IDisposable? watchRegistration;

    private volatile bool disposed;
    private volatile bool writesEnabled;
    private long loadRevision;

    public JsonSettingsProvider(string fileNameWithoutExtension, JsonSerializerOptions jsonOptions)
    {
        this.jsonOptions = jsonOptions;

        string directory = WellKnownLocations.Settings;
        Directory.CreateDirectory(directory);

        fileName = $"{fileNameWithoutExtension}.json";
        filePath = Path.Combine(directory, fileName);

        if (TryLoadCore(newWhenMissing: true, out TSettings? value))
        {
            writesEnabled = true;
        }
        else
        {
            value = new TSettings();
        }

        if (value is AgentSettings settings)
        {
            if (writesEnabled && !File.Exists(filePath))
            {
                AgentLegacyMigration.ImportProfiles(Path.Combine(WellKnownLocations.Settings, ".."), settings,
                    LocalSetting.Get("AccountInsightsLlmEndpoint", string.Empty), LocalSetting.Get("AccountInsightsLlmModel", string.Empty));
            }
            AgentCredentialStore.Load(settings);
        }

        changeObserver = new(value, this, static (self, root) =>
        {
            if (!self.disposed)
            {
                // Invalidate queued reads before a lock wait can dispatch UI callbacks.
                if (self.writesEnabled)
                {
                    Interlocked.Increment(ref self.loadRevision);
                }
                lock (self.syncRoot)
                {
                    if (!self.disposed && self.writesEnabled)
                    {
                        self.SaveCore(root);
                    }
                }
            }
        });

        fileProvider = new(directory);
        StartWatchingFileChange();
    }

    public TSettings CurrentValue
    {
        get
        {
            lock (syncRoot)
            {
                return changeObserver.Root;
            }
        }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref disposed, true))
        {
            return;
        }

        IDisposable? registration;
        lock (syncRoot)
        {
            registration = watchRegistration;
            watchRegistration = null;
        }

        // Disposing a registration can wait for a callback that needs syncRoot.
        registration?.Dispose();
        fileProvider.Dispose();

        changeObserver.Dispose();
    }

    private static void OnFileChanged(object? state)
    {
        if (state is not JsonSettingsProvider<TSettings> self)
        {
            return;
        }

        if (self.disposed)
        {
            return;
        }

        try
        {
            TSettings? newValue;
            bool loaded;
            long revision;

            lock (self.syncRoot)
            {
                revision = Interlocked.Increment(ref self.loadRevision);
                loaded = self.TryLoadCore(newWhenMissing: false, out newValue);
                // A valid read must not block newer local edits while its snapshot
                // waits for the UI thread. Failed reads still protect the file.
                if (!loaded)
                {
                    self.writesEnabled = false;
                }
            }

            if (loaded)
            {
                self.BeginApplyExternalChangeOnMainThread(newValue!, revision);
            }
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
        }
        finally
        {
            self.StartWatchingFileChange();
        }
    }

    private void StartWatchingFileChange()
    {
        if (disposed)
        {
            return;
        }

        IDisposable registration;
        try
        {
            registration = fileProvider.Watch(fileName).RegisterChangeCallback(OnFileChanged, this);
        }
        catch (ObjectDisposedException) when (disposed)
        {
            return;
        }

        IDisposable? previous;
        lock (syncRoot)
        {
            if (disposed)
            {
                previous = registration;
            }
            else
            {
                previous = watchRegistration;
                watchRegistration = registration;
            }
        }
        previous?.Dispose();
    }

    private void BeginApplyExternalChangeOnMainThread(TSettings value, long revision)
    {
        if (ReferenceEquals(SynchronizationContext.Current, AgentThreadingContext.Current.SynchronizationContext))
        {
            ApplyExternalChange(value, revision);
            return;
        }

        AgentThreadingContext.Current.SynchronizationContext.Post(static state =>
        {
            if (state is (JsonSettingsProvider<TSettings> provider, TSettings change, long revision))
            {
                provider.ApplyExternalChange(change, revision);
            }
        }, Tuple.Create(this, value, revision));
    }

    private void ApplyExternalChange(TSettings value, long revision)
    {
        if (disposed)
        {
            return;
        }

        try
        {
            lock (syncRoot)
            {
                if (disposed || revision != loadRevision) { return; }
            }
            // Credential reads can dispatch UI callbacks. Keep observation enabled
            // until they finish, then reject the snapshot if a local edit occurred.
            if (value is AgentSettings settings) { AgentCredentialStore.Load(settings); }
            lock (syncRoot)
            {
                if (disposed || revision != loadRevision) { return; }
                using (BooleanTrueScope.Create(ref changeObserver.Suppressed))
                {
                    changeObserver.Root.CopyFrom(value);
                    changeObserver.Refresh();
                }
                writesEnabled = true;
            }
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
        }
    }

    private bool TryLoadCore(bool newWhenMissing, [NotNullWhen(true)] out TSettings? value)
    {
        Exception? loadException = null;

        for (int retry = 0; retry < LoadRetryCount; retry++)
        {
            try
            {
                bool missing;
                try
                {
                    File.GetAttributes(filePath);
                    missing = false;
                }
                catch (FileNotFoundException) { missing = true; }
                catch (DirectoryNotFoundException) { missing = true; }

                if (missing)
                {
                    if (newWhenMissing)
                    {
                        value = new TSettings();
                        return true;
                    }

                    if (retry < LoadRetryCount - 1)
                    {
                        SentrySdk.AddBreadcrumb(BreadcrumbFactory.CreateInfo("Retry missing settings load", "Service.AI.Settings.JsonSettingsProvider"));
                        Thread.Sleep(LoadRetryDelay);
                        continue;
                    }

                    break;
                }

                using (FileStream stream = File.OpenRead(filePath))
                {
                    value = JsonSerializer.Deserialize<TSettings>(stream, jsonOptions)
                        ?? throw new JsonException("Settings root must not be null.");
                    return true;
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                if (retry < LoadRetryCount - 1)
                {
                    SentrySdk.AddBreadcrumb(BreadcrumbFactory.CreateInfo("Retry settings load", "Service.AI.Settings.JsonSettingsProvider"));
                    Thread.Sleep(LoadRetryDelay);
                }
                else
                {
                    loadException = ex;
                    break;
                }
            }
            catch (Exception ex) when (ex is JsonException or NotSupportedException or InvalidOperationException or ArgumentException or TargetInvocationException)
            {
                loadException = ex;
                break;
            }
        }

        value = null;
        if (loadException is not null)
        {
            SentrySdk.CaptureException(loadException);
        }

        return false;
    }

    private void SaveCore(TSettings value)
    {
        string tempFile = $"{filePath}.tmp";

        try
        {
            // A file read can win the lock after the local-change notification.
            // Invalidate it again before credential writes can dispatch callbacks.
            Interlocked.Increment(ref loadRevision);
            if (value is AgentSettings settings) { AgentCredentialStore.Save(settings); }
            using (FileStream stream = File.Open(tempFile, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                JsonSerializer.Serialize(stream, value, jsonOptions);
            }

            File.ClearReadOnlyAttribute(filePath);
            File.Move(tempFile, filePath, true);
        }
        catch (Exception ex)
        {
            try
            {
                File.Delete(tempFile);
            }
            catch (Exception ex2) when (ex2 is IOException or UnauthorizedAccessException)
            {
                // Ignore
            }

            // Preserve the original save failure as the observable error.
            SentrySdk.CaptureException(ex);
            throw;
        }
    }
}
