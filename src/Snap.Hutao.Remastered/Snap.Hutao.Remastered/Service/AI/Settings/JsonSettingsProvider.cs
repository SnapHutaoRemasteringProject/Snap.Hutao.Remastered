// Copyright (c) 2026 DGP Studio. All rights reserved.
// Licensed under the MIT license.
// Adapted from Snap.Nicole.

using Microsoft.Extensions.FileProviders;
using Sentry;
using Snap.Hutao.Remastered.Core.AI;
using Snap.Hutao.Remastered.Core.AI.Diagnostics;
using Snap.Hutao.Remastered.Core.AI.IO;
using Snap.Hutao.Remastered.Core.AI.Threading;
using Snap.Hutao.Remastered.Service.AI.Migration;
using Snap.Hutao.Remastered.Core.Setting;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Threading;

namespace Snap.Hutao.Remastered.Service.AI.Settings;

// A single change in the options can trigger the whole object graph to be serialized and written to disk,
// so it's recommended to use this provider only for relatively small options objects and avoid putting large data in them.
internal sealed class JsonSettingsProvider<TSettings> : ISettingsProvider<TSettings>, IDisposable
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

    public JsonSettingsProvider(string fileNameWithoutExtension, JsonSerializerOptions jsonOptions)
    {
        this.jsonOptions = jsonOptions;

        string directory = WellKnownLocations.Settings;
        Directory.CreateDirectory(directory);

        fileName = $"{fileNameWithoutExtension}.json";
        filePath = Path.Combine(directory, fileName);

        if (!TryLoadCore(newWhenMissing: true, out TSettings? value))
        {
            value = new TSettings();
        }

        if (value is AgentSettings settings)
        {
            if (!File.Exists(filePath))
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
                lock (self.syncRoot)
                {
                    if (!self.disposed)
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

        lock (syncRoot)
        {
            watchRegistration?.Dispose();
            fileProvider.Dispose();
        }

        changeObserver.Dispose();
    }

    private static void OnFileChanged(object? state)
    {
        if (state is not JsonSettingsProvider<TSettings> self)
        {
            return;
        }

        using SentryDiagnosticSpan span = SentryDiagnostics.StartSpan(SentryOperations.SettingsJsonFileChanged, $"Reload {self.fileName}");
        span.SetTag(SentryTags.SettingsOptions, typeof(TSettings).Name);

        if (self.disposed)
        {
            span.Finish(SpanStatus.Cancelled);
            return;
        }

        try
        {
            TSettings? newValue;
            bool loaded;

            lock (self.syncRoot)
            {
                loaded = self.TryLoadCore(newWhenMissing: false, out newValue);
            }

            if (loaded)
            {
                self.BeginApplyExternalChangeOnMainThread(newValue!);
            }
            else
            {
                span.Finish(SpanStatus.FailedPrecondition);
            }
        }
        catch (Exception ex)
        {
            SentryDiagnostics.CaptureException(ex, span, SentryOperations.SettingsJsonFileChanged);
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

        lock (syncRoot)
        {
            if (disposed)
            {
                registration.Dispose();
                return;
            }

            watchRegistration?.Dispose();
            watchRegistration = registration;
        }
    }

    private void BeginApplyExternalChangeOnMainThread(TSettings value)
    {
        if (ReferenceEquals(SynchronizationContext.Current, AgentThreadingContext.Current.SynchronizationContext))
        {
            ApplyExternalChange(value);
            return;
        }

        AgentThreadingContext.Current.SynchronizationContext.Post(static state =>
        {
            if (state is (JsonSettingsProvider<TSettings> provider, TSettings change))
            {
                provider.ApplyExternalChange(change);
            }
        }, Tuple.Create(this, value));
    }

    private void ApplyExternalChange(TSettings value)
    {
        using SentryDiagnosticSpan span = SentryDiagnostics.StartSpan(SentryOperations.SettingsJsonApplyExternalChange, $"Apply {fileName}");
        span.SetTag(SentryTags.SettingsOptions, typeof(TSettings).Name);

        if (disposed)
        {
            span.Finish(SpanStatus.Cancelled);
            return;
        }

        try
        {
            using (BooleanTrueScope.Create(ref changeObserver.Suppressed))
            {
                if (value is AgentSettings settings) { AgentCredentialStore.Load(settings); }
                changeObserver.Root.CopyFrom(value);
                changeObserver.Refresh();
            }
        }
        catch (Exception ex)
        {
            SentryDiagnostics.CaptureException(ex, span, SentryOperations.SettingsJsonApplyExternalChange);
        }
    }

    private bool TryLoadCore(bool newWhenMissing, [NotNullWhen(true)] out TSettings? value)
    {
        using SentryDiagnosticSpan span = SentryDiagnostics.StartSpan(SentryOperations.SettingsJsonLoad, $"Load {fileName}");
        span.SetTag(SentryTags.SettingsOptions, typeof(TSettings).Name);

        int loadRetry = 0;
        Exception? loadException = null;

        for (int retry = 0; retry < LoadRetryCount; retry++)
        {
            try
            {
                try
                {
                    if (!File.Exists(filePath))
                    {
                        span.SetTag(SentryTags.SettingsFileExists, false);

                        if (newWhenMissing)
                        {
                            value = new TSettings();
                            return true;
                        }

                        loadRetry = retry + 1;

                        if (retry < LoadRetryCount - 1)
                        {
                            SentryDiagnostics.AddBreadcrumb("Retry missing settings load", SentryBreadcrumbCategories.SettingsJson, SentryBreadcrumbTypes.Default);
                            Thread.Sleep(LoadRetryDelay);
                            continue;
                        }

                        break;
                    }

                    span.SetTag(SentryTags.SettingsFileExists, true);
                    using (FileStream stream = File.OpenRead(filePath))
                    {
                        value = JsonSerializer.Deserialize<TSettings>(stream, jsonOptions) ?? new TSettings();
                        return true;
                    }
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    loadRetry = retry + 1;
                    if (retry < LoadRetryCount - 1)
                    {
                        SentryDiagnostics.AddBreadcrumb("Retry settings load", SentryBreadcrumbCategories.SettingsJson, SentryBreadcrumbTypes.Default);
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
            finally
            {
                span.SetData(SentryData.SettingsLoadRetry, loadRetry);
            }
        }

        value = null;
        if (loadException is not null)
        {
            SentryDiagnostics.CaptureException(loadException, span, SentryOperations.SettingsJsonLoad);
        }
        else
        {
            span.Finish(SpanStatus.FailedPrecondition);
        }

        return false;
    }

    private void SaveCore(TSettings value)
    {
        using SentryDiagnosticSpan span = SentryDiagnostics.StartSpan(SentryOperations.SettingsJsonSave, $"Save {fileName}");
        span.SetTag(SentryTags.SettingsOptions, typeof(TSettings).Name);

        string tempFile = $"{filePath}.tmp";

        try
        {
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
            SentryDiagnostics.CaptureException(ex, span, SentryOperations.SettingsJsonSave);
            throw;
        }
    }
}
