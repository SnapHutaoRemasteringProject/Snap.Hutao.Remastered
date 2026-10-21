// Copyright (c) 2026 DGP Studio. All rights reserved.
// Licensed under the MIT license.
// Adapted from Snap.Nicole.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.AI;
using System.Collections.Generic;
using System.Linq;
using Snap.Hutao.Remastered.Core.AI.Diagnostics;
using Snap.Hutao.Remastered.Resource.Localization.AI;
using Snap.Hutao.Remastered.Service.AI.Models;
using Snap.Hutao.Remastered.Service.AI.Observables;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Snap.Hutao.Remastered.ViewModel.AI;

internal sealed partial class AgentConversationViewModel(IAgentConversationDeleteHandler conversationDeleteHandler, AgentConversationTurnController conversationTurnController)
    : ObservableObject, IDisposable
{
    private readonly IAgentConversationDeleteHandler conversationDeleteHandler = conversationDeleteHandler;
    private readonly AgentConversationTurnController conversationTurnController = conversationTurnController;

    public List<ObservableToolApprovalRequestContent> ToolApprovalRequests { get; } = [];

    private bool disposed;
    private Task? pendingOperationOnDispose;

    public Guid Id { get; set; } = Guid.NewGuid();

    public AgentConversationRuntime Runtime { get; } = new();

    public string? AccountSnapshot { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RegenerateCommand))]
    public partial AgentTurnCheckpoint? LastTurnCheckpoint { get; set; }

    private bool CanRegenerate { get => !disposed && !IsBusy && ToolApprovalRequests.Count == 0 && GenerationCommand is null && LastTurnCheckpoint is not null && AgentConversationProfileController.CanCreateRequestOptions(ModelProviderProfile, ModelProfile); }

    public Task? PendingOperation { get => GenerationCommand?.ExecutionTask ?? pendingOperationOnDispose; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TitleDisplay))]
    public partial string Title { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CreatedAtDisplay))]
    public partial DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.Now;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(UpdatedAtDisplay))]
    public partial DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.Now;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SendMessageCommand), nameof(RegenerateCommand))]
    public partial ModelProviderProfile? ModelProviderProfile { get; set; }

    partial void OnModelProviderProfileChanged(ModelProviderProfile? value)
    {
        Runtime.Reset();

        if (value is null)
        {
            ModelProfile = null;
            return;
        }

        if (ModelProfile is null || !value.ModelProfiles.Contains(ModelProfile))
        {
            ModelProfile = value.ModelProfiles.CurrentItem;
        }
    }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SendMessageCommand), nameof(RegenerateCommand))]
    public partial ModelProfile? ModelProfile { get; set; }

    partial void OnModelProfileChanged(ModelProfile? value)
    {
        Runtime.Reset();
    }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SendMessageCommand), nameof(RegenerateCommand))]
    public partial string InputText { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SendMessageCommand), nameof(RegenerateCommand), nameof(DeleteConversationCommand), nameof(ApproveToolApprovalCommand), nameof(DenyToolApprovalCommand))]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial AgentConversationStatisticsViewModel ConversationStatistics { get; private set; } = new();

    [ObservableProperty]
    public partial ObservableChatMessageCollection Messages { get; set; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SendMessageCommand), nameof(RegenerateCommand), nameof(ApproveToolApprovalCommand), nameof(DenyToolApprovalCommand))]
    public partial ObservableToolApprovalRequestContent? ToolApprovalRequest { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanStopGeneration))]
    [NotifyCanExecuteChangedFor(nameof(SendMessageCommand), nameof(RegenerateCommand), nameof(StopGenerationCommand), nameof(DeleteConversationCommand), nameof(ApproveToolApprovalCommand), nameof(DenyToolApprovalCommand))]
    private partial IAsyncRelayCommand? GenerationCommand { get; set; }

    private bool CanSendMessage { get => !disposed && GenerationCommand is null && !IsBusy && ToolApprovalRequest?.CanRespond is not true && ModelProviderProfile is not null && !string.IsNullOrWhiteSpace(InputText) && !string.IsNullOrWhiteSpace(ModelProfile?.ModelId); }

    public bool CanStopGeneration { get => !disposed && GenerationCommand is { IsCancellationRequested: false }; }

    private bool CanDeleteConversation { get => !disposed && GenerationCommand is null && !IsBusy; }

    private bool CanRespondToToolApproval { get => !disposed && GenerationCommand is null && AgentConversationTurnController.CanRespondToToolApproval(this); }

    public StringResourceValue TitleDisplay { get => string.IsNullOrWhiteSpace(Title) ? SHName.UIXamlPagesAgentPageLabelNewConversation : Title; }

    public string CreatedAtDisplay { get => CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"); }

    public string UpdatedAtDisplay { get => UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss"); }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref disposed, true))
        {
            return;
        }

        IAsyncRelayCommand? command = GenerationCommand;
        pendingOperationOnDispose = command?.ExecutionTask;
        GenerationCommand = null;
        command?.Cancel();
    }

    public void UpdateConversationStatistics()
    {
        ConversationStatistics = AgentConversationStatisticsViewModel.Create(Messages);
    }

    [RelayCommand(CanExecute = nameof(CanDeleteConversation))]
    private void DeleteConversation()
    {
        if (!CanDeleteConversation)
        {
            return;
        }

        conversationDeleteHandler.DeleteConversation(this);
    }

    [RelayCommand(CanExecute = nameof(CanStopGeneration))]
    private void StopGeneration()
    {
        if (disposed)
        {
            return;
        }

        if (GenerationCommand is not { } command)
        {
            return;
        }

        SentryDiagnostics.AddBreadcrumb("Stop chat generation", SentryBreadcrumbCategories.AIChat, SentryBreadcrumbTypes.UI);
        command.Cancel();
        OnPropertyChanged(nameof(CanStopGeneration));
        StopGenerationCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanSendMessage))]
    private async Task SendMessageAsync(CancellationToken cancellationToken)
    {
        if (!CanSendMessage)
        {
            return;
        }

        string input = InputText.Trim();
        InputText = string.Empty;

        // There should not be a pending tool approval request when sending a normal message.
        Debug.Assert(ToolApprovalRequest is null);

        ChatMessage inputMessage = new(ChatRole.User, input)
        {
            CreatedAt = DateTimeOffset.Now,
            AuthorName = conversationTurnController.UserName,
        };

        await SendMessageCoreAsync(inputMessage, input, SendMessageCommand, cancellationToken);
    }

    [RelayCommand(CanExecute = nameof(CanRegenerate))]
    private async Task RegenerateAsync(CancellationToken cancellationToken)
    {
        if (!CanRegenerate) { return; }
        try
        {
            GenerationCommand = RegenerateCommand;
            await conversationTurnController.RegenerateAsync(this, cancellationToken);
        }
        catch (Exception exception)
        {
            conversationTurnController.AddExceptionMessages(this, exception);
        }
        finally
        {
            GenerationCommand = null;
        }
    }

    [RelayCommand(CanExecute = nameof(CanRespondToToolApproval))]
    private Task ApproveToolApprovalAsync(CancellationToken cancellationToken)
    {
        return SendToolApprovalAsync(true, null, ApproveToolApprovalCommand, cancellationToken);
    }

    [RelayCommand(CanExecute = nameof(CanRespondToToolApproval))]
    private Task DenyToolApprovalAsync(CancellationToken cancellationToken)
    {
        return SendToolApprovalAsync(false, "Rejected by user", DenyToolApprovalCommand, cancellationToken);
    }

    private async Task SendToolApprovalAsync(bool approved, string? reason, IAsyncRelayCommand command, CancellationToken cancellationToken)
    {
        ObservableToolApprovalRequestContent? request = ToolApprovalRequest;
        if (request is null || disposed || !CanRespondToToolApproval || !ReferenceEquals(ToolApprovalRequest, request))
        {
            return;
        }

        request.Approved = approved;
        request.Reason = reason;
        request.IsHandled = true;
        ToolApprovalRequest = ToolApprovalRequests.FirstOrDefault(static pending => pending.CanRespond);
        if (ToolApprovalRequest is not null)
        {
            conversationTurnController.SaveApprovalProgress(this);
            return;
        }
        // Keep the final decision retryable until the agent accepts the responses.
        request.IsHandled = false;
        ToolApprovalRequest = request;

        List<AIContent> responses = ToolApprovalRequests.Select(static pending => (AIContent)(pending.RawRepresentation
            ?? throw new InvalidOperationException("Tool approval request content is unavailable."))
            .CreateResponse(pending.Approved == true, pending.Reason)).ToList();
        ChatMessage responseMessage = new(ChatRole.User, responses)
        {
            CreatedAt = DateTimeOffset.Now,
            AuthorName = conversationTurnController.UserName,
        };

        await SendMessageCoreAsync(responseMessage, null, command, cancellationToken);
    }

    private async Task SendMessageCoreAsync(ChatMessage message, string? titleInput, IAsyncRelayCommand command, CancellationToken cancellationToken)
    {
        try
        {
            try
            {
                GenerationCommand = command;
                await conversationTurnController.SendMessageAsync(this, message, titleInput, cancellationToken);
            }
            finally
            {
                GenerationCommand = null;
            }
        }
        catch (AgentConversationException ex)
        {
            conversationTurnController.AddExceptionMessages(this, ex);
        }
        catch (Exception exception)
        {
            conversationTurnController.AddExceptionMessages(this, exception);
        }
    }
}
