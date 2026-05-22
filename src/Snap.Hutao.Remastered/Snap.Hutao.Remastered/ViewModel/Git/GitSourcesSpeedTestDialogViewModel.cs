// Copyright (c) Snap Hutao RP. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Dispatching;
using Snap.Hutao.Remastered.Service.Git;
using Snap.Hutao.Remastered.Web.Hutao;
using System.Collections.ObjectModel;
using System.Collections.Immutable;
using Snap.Hutao.Remastered.Web.Hutao.Response;

namespace Snap.Hutao.Remastered.ViewModel.Git;

[BindableCustomPropertyProvider]
internal sealed partial class GitSourcesSpeedTestDialogViewModel : ObservableObject
{
    private readonly GitMirrorSpeedTester? tester;
    private readonly IServiceProvider serviceProvider;

    public ObservableCollection<MirrorTestResult> TestResults { get; } = new();

    [ObservableProperty]
    public partial bool IsRunning { get; private set; }

    [ObservableProperty]
    public partial string StatusMessage { get; private set; }

    public GitSourcesSpeedTestDialogViewModel(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
        tester = serviceProvider.GetService<GitMirrorSpeedTester>();
        StatusMessage = SH.ViewDialogGitSourcesSpeedTestDialogInitialStatusMessage;
    }

    [Command("RunCommand")]
    public async Task RunSpeedTestAsync()
    {
        if (IsRunning)
        {
            return;
        }

        IsRunning = true;
        StatusMessage = SH.ViewDialogGitSourcesSpeedTestDialogFetchingMirrorListStatusMessage;
        TestResults.Clear();

        DispatcherQueue dispatcherQueue = DispatcherQueue.GetForCurrentThread();

        try
        {
            using IServiceScope scope = serviceProvider.CreateScope();
            HutaoInfrastructureClient client = scope.ServiceProvider.GetRequiredService<HutaoInfrastructureClient>();

            HutaoResponse<ImmutableArray<GitRepository>> response = await client.GetGitRepositoryAsync("Snap.Metadata").ConfigureAwait(false);
            ImmutableArray<GitRepository> infos = response.Data;

            if (infos.IsDefaultOrEmpty)
            {
                dispatcherQueue.TryEnqueue(() => StatusMessage = SH.ViewDialogGitSourcesSpeedTestDialogFailedToFetchMirrorListStatusMessage);
                return;
            }

            dispatcherQueue.TryEnqueue(() => StatusMessage = string.Format(SH.ViewDialogGitSourcesSpeedTestDialogTestingMirrorsStatusMessage, infos.Length));

            // Initialize result items for each mirror
            dispatcherQueue.TryEnqueue(() =>
            {
                foreach (GitRepository repo in infos)
                {
                    TestResults.Add(new MirrorTestResult { Url = repo.HttpsUrl.OriginalString });
                }
            });

            if (tester is not null)
            {
                await tester.RunOnceAsync(infos, CancellationToken.None).ConfigureAwait(false);
            }

            // Fetch results from scheduler
            IMirrorScheduler scheduler = scope.ServiceProvider.GetRequiredService<IMirrorScheduler>();
            IReadOnlyList<GitRepository> sorted = scheduler.GetSortedMirrors(infos);

            // Update results list on UI thread
            dispatcherQueue.TryEnqueue(() =>
            {
                TestResults.Clear();
                foreach (GitRepository repo in sorted)
                {
                    TestResults.Add(new MirrorTestResult
                    {
                        Url = repo.HttpsUrl.OriginalString,
                        Status = SH.ViewDialogGitSourcesSpeedTestDialogCompletedStatusMessage
                    });
                }

                StatusMessage = SH.ViewDialogGitSourcesSpeedTestDialogAllCompletedStatusMessage;
            });
        }
        catch (Exception ex)
        {
            dispatcherQueue.TryEnqueue(() => StatusMessage = string.Format(SH.ViewDialogGitSourcesSpeedTestDialogErrorStatusMessage, ex.Message));
        }
        finally
        {
            dispatcherQueue.TryEnqueue(() => IsRunning = false);
        }
    }
}

public sealed class MirrorTestResult
{
    public string Url { get; set; } = string.Empty;
    public string Status { get; set; } = SH.ViewDialogGitSourcesSpeedTestDialogPendingStatusMessage;
}
