// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core.ExceptionService;
using Snap.Hutao.Remastered.Core.Property;
using Snap.Hutao.Remastered.Service.BackgroundActivity;
using Snap.Hutao.Remastered.Service.Metadata.Provider;
using Snap.Hutao.Remastered.Service.Network;
using Snap.Hutao.Remastered.Service.Notification;
using System.Collections.Immutable;
using BackgroundActivityTask = Snap.Hutao.Remastered.Service.BackgroundActivity.BackgroundActivity;

namespace Snap.Hutao.Remastered.Service.Metadata;

/// <summary>
/// Metadata manager: creates and holds providers, chooses a source by preference, falls back,
/// switches, broadcasts cache clears and schedules retries after the network recovers.
/// </summary>
[Service(ServiceLifetime.Singleton, typeof(IMetadataService))]
public sealed partial class MetadataService : IMetadataService, IDisposable
{
    private const string RetryKey = "MetadataService.Initialization";

    private readonly IMetadataProviderFactory providerFactory;
    private readonly MetadataSourceOptions sourceOptions;
    private readonly INetworkRetryCoordinator networkRetryCoordinator;
    private readonly IMessenger messenger;
    private readonly ITaskContext taskContext;
    private readonly BackgroundActivityOptions backgroundActivityOptions;
    private readonly ILogger<MetadataService> logger;
    private readonly Lock gate = new();
    private readonly Dictionary<MetadataSourceKind, IMetadataProvider> created = [];

    private TaskCompletionSource initialized = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private Task<bool>? attempt;
    private MetadataSourceKind? requested;
    private volatile IMetadataProvider? active;
    private IDisposable? retryRegistration;

    [GeneratedConstructor]
    public partial MetadataService(IServiceProvider serviceProvider);

    public MetadataSourceKind ActiveSource
    {
        get => active?.Kind ?? MetadataSourceKind.Auto;
    }

    public IReadOnlyList<MetadataSourceKind> AvailableSources
    {
        get => providerFactory.AvailableKinds;
    }

    public bool IsInitialized
    {
        get => active?.IsInitialized is true;
    }

    public MetadataCache Cache
    {
        get => Active.Cache;
    }

    private IMetadataProvider Active
    {
        get => active ?? throw HutaoException.InvalidOperation(SH.ServiceMetadataNotInitialized);
    }

    public async ValueTask<bool> InitializeAsync()
    {
        if (IsInitialized)
        {
            return true;
        }

        Task completion;
        lock (gate)
        {
            completion = initialized.Task;
        }

        // Self-starting: a page must never wait for initialization that nobody triggered.
        _ = InitializeInternalAsync();

        await completion.WaitAsync(XamlApplicationLifetime.ShutdownToken).ConfigureAwait(false);
        return IsInitialized;
    }

    public ValueTask<bool> InitializeInternalAsync(CancellationToken token = default)
    {
        lock (gate)
        {
            MetadataSourceKind preference = requested ?? sourceOptions.Source.Value;
            return new(attempt ??= AttemptAsync(initialized, preference, token));
        }
    }

    public async ValueTask<bool> SwitchAsync(MetadataSourceKind kind, CancellationToken token = default)
    {
        // Clear the previous source first so in-flight loads cannot repopulate the old cache.
        if (active is { } previous)
        {
            previous.ClearCache();
        }

        TaskCompletionSource next = new(TaskCreationOptions.RunContinuationsAsynchronously);
        lock (gate)
        {
            requested = kind;
            active = null;
            attempt = null;
            initialized = next;
        }

        return await InitializeInternalAsync(token).ConfigureAwait(false);
    }

    public ValueTask<ImmutableArray<T>> FromCacheOrSourceAsync<T>(MetadataCategory category, CancellationToken token = default)
        where T : class
    {
        return Active.FromCacheOrSourceAsync<T>(category, token);
    }

    public void ClearCache()
    {
        IMetadataProvider[] providers;
        lock (gate)
        {
            providers = [.. created.Values];
        }

        foreach (IMetadataProvider provider in providers)
        {
            provider.ClearCache();
        }
    }

    public void Dispose()
    {
        sourceOptions.Source.PropertyChanged -= OnSourceOptionChanged;
        retryRegistration?.Dispose();
    }

    partial void PostConstruct(IServiceProvider serviceProvider)
    {
        retryRegistration = networkRetryCoordinator.Register(RetryKey, RetryAfterNetworkRecoveryAsync);
        sourceOptions.Source.PropertyChanged += OnSourceOptionChanged;
    }

    private static IEnumerable<MetadataSourceKind> EnumerateCandidates(MetadataSourceKind preference)
    {
        if (preference is MetadataSourceKind.Proto or MetadataSourceKind.Auto)
        {
            yield return MetadataSourceKind.Proto;
        }

        yield return MetadataSourceKind.Git;
    }

    private IMetadataProvider GetProvider(MetadataSourceKind kind)
    {
        IMetadataProvider provider = providerFactory.GetOrCreate(kind);
        lock (gate)
        {
            created[provider.Kind] = provider;
        }

        return provider;
    }

    private async Task<bool> AttemptAsync(TaskCompletionSource completion, MetadataSourceKind preference, CancellationToken token)
    {
        BackgroundActivityTask activity = backgroundActivityOptions.MetadataInitialization;
        activity.Update(taskContext, SH.ServiceBackgroundActivityMetadataInitializationDescription, false, false, true, true);

        bool fellBack = false;
        try
        {
            foreach (MetadataSourceKind kind in EnumerateCandidates(preference))
            {
                IMetadataProvider provider = GetProvider(kind);
                bool providerInitialized = await provider.InitializeAsync(token).ConfigureAwait(false);
                if (providerInitialized)
                {
                    // Commit active and the completion source under one lock: a SwitchAsync that ran
                    // during initialization must not be overwritten by this (stale) attempt.
                    lock (gate)
                    {
                        if (!ReferenceEquals(initialized, completion))
                        {
                            return false;
                        }

                        active = provider;
                        completion.TrySetResult();
                    }

                    activity.Update(taskContext, SH.ServiceBackgroundActivityMetadataInitializationCompleted, true, false, false, false);
                    networkRetryCoordinator.ClearPending(RetryKey);

                    // Only an explicit Proto preference is worth telling the user about; Auto falls
                    // back silently.
                    if (fellBack && preference is MetadataSourceKind.Proto)
                    {
                        NotifyOnMainThread(InfoBarMessage.Warning(SH.ServiceMetadataSourceFallback));
                    }

                    return true;
                }

                fellBack = true;
            }

            // Keep the completion source pending: a later call or the network retry may succeed.
            lock (gate)
            {
                attempt = null;
            }

            activity.Update(taskContext, SH.ServiceMetadataInitializationFailed, false, true, false, false);
            networkRetryCoordinator.MarkPending(RetryKey, SH.ServiceMetadataInitializationFailed);
            return false;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            // The self-start path discards this task, so every non-cancellation failure must be
            // observed here.
            activity.Update(taskContext, SH.ServiceMetadataInitializationFailed, false, true, false, false);
            logger.LogWarning(exception, "Metadata initialization attempt failed");
            return false;
        }
        finally
        {
            lock (gate)
            {
                if (!ReferenceEquals(initialized, completion) || !IsInitialized)
                {
                    attempt = null;
                }
            }
        }
    }

    private async ValueTask<bool> RetryAfterNetworkRecoveryAsync(CancellationToken token)
    {
        lock (gate)
        {
            attempt = null;
        }

        return await InitializeInternalAsync(token).ConfigureAwait(false);
    }

    private void OnSourceOptionChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName is nameof(IObservableProperty<MetadataSourceKind>.Value))
        {
            // The setting is already persisted; switching is this service's only job here.
            SwitchAsync(sourceOptions.Source.Value).AsTask().SafeForget();
        }
    }

    private void NotifyOnMainThread(InfoBarMessage message)
    {
        if (XamlApplicationLifetime.Exiting)
        {
            return;
        }

        taskContext.BeginInvokeOnMainThread(() => messenger.Send(message));
    }
}
