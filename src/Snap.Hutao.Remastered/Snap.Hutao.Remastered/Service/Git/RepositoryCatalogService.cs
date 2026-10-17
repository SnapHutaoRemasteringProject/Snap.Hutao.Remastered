// Copyright (c) Snap Hutao RP. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;
using Snap.Hutao.Remastered.Service.Network;
using Snap.Hutao.Remastered.Service.Notification;
using Snap.Hutao.Remastered.Web.Hutao;
using System.Collections.Immutable;

namespace Snap.Hutao.Remastered.Service.Git;

[Service(ServiceLifetime.Singleton)]
public sealed partial class RepositoryCatalogService : ObservableObject, IDisposable
{
    private const string RetryKey = "RepositoryCatalogService.Connection";
    private readonly IServiceScopeFactory serviceScopeFactory;
    private readonly ITaskContext taskContext;
    private readonly IMessenger messenger;
    private readonly ILogger<RepositoryCatalogService> logger;
    private readonly INetworkRetryCoordinator networkRetryCoordinator;
    private readonly SemaphoreSlim requestGate = new(1, 1);
    private readonly CancellationTokenSource lifetime = new();
    private readonly RepositoryCatalogRequest requestPolicy = new();
    private readonly AsyncLoadOperation reconnectLoad = new();
    private CancellationToken lifetimeToken;
    private IDisposable? retryRegistration;
    private ImmutableArray<GitRepository> repositories = [];
    private bool hasCatalogue;
    private bool usingFallback;
    private int disposed;

    [GeneratedConstructor]
    public partial RepositoryCatalogService(IServiceProvider serviceProvider);

    partial void PostConstruct(IServiceProvider serviceProvider)
    {
        lifetimeToken = lifetime.Token;
        retryRegistration = networkRetryCoordinator.Register(RetryKey, ReconnectSharedAsync);
    }

    [ObservableProperty]
    public partial bool IsOffline { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanReconnect))]
    [NotifyPropertyChangedFor(nameof(ConnectionStatus))]
    public partial bool IsConnecting { get; private set; }

    public bool CanReconnect => !IsConnecting;

    public string ConnectionStatus => IsConnecting ? SH.RepositoryServerReconnecting : SH.RepositoryServerOfflineStatus;

    public async ValueTask<ImmutableArray<GitRepository>> GetRepositoriesAsync(string name, CancellationToken token)
    {
        if (XamlApplicationLifetime.Exiting) throw new OperationCanceledException();
        using CancellationTokenSource linked = CancellationTokenSource.CreateLinkedTokenSource(token, lifetimeToken, XamlApplicationLifetime.ShutdownToken);
        await requestGate.WaitAsync(linked.Token).ConfigureAwait(false);
        try
        {
            if (!hasCatalogue)
            {
                ImmutableArray<GitRepository> online = await FetchOnlineAsync(linked.Token).ConfigureAwait(false);
                linked.Token.ThrowIfCancellationRequested();
                usingFallback = online.IsDefault;
                repositories = usingFallback ? BuiltInRepositoryCatalog.Create() : online;
                hasCatalogue = true;
            }

            await SetConnectionStateAsync(usingFallback, linked.Token).ConfigureAwait(false);
            return [.. repositories.Where(repository => string.Equals(repository.Name, name, StringComparison.Ordinal))];
        }
        finally { requestGate.Release(); }
    }

    [Command("ReconnectCommand")]
    private async Task ReconnectAsync()
    {
        try
        {
            bool connected = await ReconnectSharedAsync(lifetimeToken).ConfigureAwait(false);
            if (!connected && !XamlApplicationLifetime.Exiting && !lifetimeToken.IsCancellationRequested)
            {
                await taskContext.SwitchToMainThreadAsync();
                if (!XamlApplicationLifetime.Exiting && IsOffline)
                    messenger.Send(InfoBarMessage.Warning(SH.RepositoryServerReconnectFailed));
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception exception) { logger.LogWarning(exception, "Repository server reconnect failed"); }
    }

    private async ValueTask<bool> ReconnectSharedAsync(CancellationToken token)
    {
        // Manual and automatic reconnects join one request, including when a
        // network-status event arrives while the user is clicking the button.
        return await reconnectLoad.RunAsync(_ => ReconnectCoreAsync(), lifetimeToken).WaitAsync(token).ConfigureAwait(false);
    }

    private async Task<bool> ReconnectCoreAsync()
    {
        using CancellationTokenSource linked = CancellationTokenSource.CreateLinkedTokenSource(lifetimeToken, XamlApplicationLifetime.ShutdownToken);
        CancellationToken token = linked.Token;
        await requestGate.WaitAsync(token).ConfigureAwait(false);
        try
        {
            if (hasCatalogue && !usingFallback) return true;
            await taskContext.SwitchToMainThreadAsync();
            token.ThrowIfCancellationRequested();
            IsConnecting = true;

            ImmutableArray<GitRepository> online = await FetchOnlineAsync(token).ConfigureAwait(false);
            token.ThrowIfCancellationRequested();
            if (online.IsDefault) return false;
            repositories = online;
            hasCatalogue = true;
            usingFallback = false;
            await SetConnectionStateAsync(false, token).ConfigureAwait(false);
            return true;
        }
        finally
        {
            requestGate.Release();
            if (!token.IsCancellationRequested)
            {
                await taskContext.SwitchToMainThreadAsync();
                if (!token.IsCancellationRequested) IsConnecting = false;
            }
        }
    }

    private async Task<ImmutableArray<GitRepository>> FetchOnlineAsync(CancellationToken token)
    {
        using CancellationTokenSource linked = CancellationTokenSource.CreateLinkedTokenSource(token, XamlApplicationLifetime.ShutdownToken);
        return await requestPolicy.FetchAsync(async requestToken =>
        {
            using IServiceScope scope = serviceScopeFactory.CreateScope();
            HutaoInfrastructureClient client = scope.ServiceProvider.GetRequiredService<HutaoInfrastructureClient>();
            return await client.GetAllGitRepositoriesAsync(requestToken).ConfigureAwait(false);
        }, linked.Token, (attempt, exception) =>
            logger.LogWarning(exception, "Repository catalogue unavailable, attempt {Attempt}", attempt)).ConfigureAwait(false);
    }

    private async Task SetConnectionStateAsync(bool offline, CancellationToken token)
    {
        if (XamlApplicationLifetime.Exiting) return;
        await taskContext.SwitchToMainThreadAsync();
        token.ThrowIfCancellationRequested();
        bool changed = IsOffline != offline;
        IsOffline = offline;
        if (offline)
        {
            // The catalogue already owns the user-facing offline notification.
            // Keep retrying this exact endpoint silently in the background.
            networkRetryCoordinator.MarkPending(RetryKey, string.Empty);
        }
        else networkRetryCoordinator.ClearPending(RetryKey);
        if (changed)
            messenger.Send(offline ? InfoBarMessage.Warning(SH.RepositoryServerOfflineWarning)
                : InfoBarMessage.Success(SH.RepositoryServerConnectionRestored));
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref disposed, 1) is not 0) return;
        retryRegistration?.Dispose();
        lifetime.Cancel();
        lifetime.Dispose();
        // Pending async requests own their gate releases; do not dispose beneath them.
    }
}
