// Copyright (c) 2026 DGP Studio. All rights reserved.
// Licensed under the MIT license.
// Adapted from Snap.Nicole.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Snap.Hutao.Remastered.Core.AI.Collections.ObjectModel;
using Snap.Hutao.Remastered.Service.AI.Settings;
using Snap.Hutao.Remastered.Service.AI;
using Snap.Hutao.Remastered.ViewModel.AI.Settings;
using System.Threading;
using System.Text;
using Windows.ApplicationModel.DataTransfer;
using Snap.Hutao.Remastered.Service.AI.Observables;
using Snap.Hutao.Remastered.Resource.Localization.AI;

namespace Snap.Hutao.Remastered.ViewModel.AI;

[Service(ServiceLifetime.Scoped)]
internal sealed partial class AgentViewModel : ObservableObject, IDisposable
{
    private readonly AgentConversationCollectionController conversationCollectionController;
    private readonly AgentAccountContext accountContext;

    private bool disposed;

    public AgentViewModel(ISettingsProvider<AgentSettings> settingsProvider, AgentConversationCollectionController conversationCollectionController, SettingsModelConfigurationViewModel modelConfiguration, AgentAccountContext accountContext)
    {
        Settings = settingsProvider.CurrentValue;
        this.conversationCollectionController = conversationCollectionController;
        ModelConfiguration = modelConfiguration;
        this.accountContext = accountContext;

    }

    public AgentSettings Settings { get; }

    public SettingsModelConfigurationViewModel ModelConfiguration { get; private set; } = null!;

    public void Initialize(AgentAccountContext context, string accountKey, SettingsModelConfigurationViewModel modelConfiguration)
    {
        context.AccountKey = accountKey;
        ModelConfiguration = modelConfiguration;
        conversationCollectionController.LoadConversations();
        OnPropertyChanged(nameof(ModelConfiguration));
    }

    public AgentConversationViewModel NewConversation()
    {
        return conversationCollectionController.CreateConversation();
    }

    public void SaveConversations()
    {
        conversationCollectionController.SaveConversations();
    }

    public Task[] GetPendingOperations()
    {
        return Conversations.Select(static item => item.PendingOperation).Append(ModelConfiguration.RefreshModelsCommand.ExecutionTask)
            .Append(ModelConfiguration.TestConnectionCommand.ExecutionTask).Append(GenerateAnalysisCommand.ExecutionTask)
            .OfType<Task>().Where(static task => !task.IsCompleted).ToArray();
    }

    public AdvancedObservableCollection<AgentConversationViewModel> Conversations { get => conversationCollectionController.Conversations; }

    [Command("CreateConversationCommand")]
    private void CreateConversation()
    {
        if (disposed)
        {
            return;
        }

        conversationCollectionController.CreateConversation();
    }

    [Command("GenerateAnalysisCommand")]
    private async Task GenerateAnalysisAsync()
    {
        if (disposed || accountContext.Snapshot is null) { return; }
        AgentConversationViewModel conversation = conversationCollectionController.CreateConversation();
        conversation.InputText = StringResourceProxy.Default[SHName.ViewDialogAccountInsightsAnalysisPrompt];
        if (conversation.SendMessageCommand.CanExecute(null))
        {
            await conversation.SendMessageCommand.ExecuteAsync(null);
        }
    }

    [Command("CopyConversationCommand")]
    private void CopyConversation()
    {
        if (Conversations.CurrentItem is not { } conversation) { return; }
        StringBuilder text = new();
        foreach (ObservableChatMessage message in conversation.Messages)
        {
            text.AppendLine(message.AuthorName ?? message.Role.ToString());
            foreach (ObservableTextContent content in message.Contents.OfType<ObservableTextContent>()) { text.AppendLine(content.Text); }
            text.AppendLine();
        }
        DataPackage package = new();
        package.SetText(text.ToString());
        Clipboard.SetContent(package);
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref disposed, true))
        {
            return;
        }

        ModelConfiguration?.RefreshModelsCommand.Cancel();
        ModelConfiguration?.TestConnectionCommand.Cancel();
        conversationCollectionController.Dispose();
    }
}
