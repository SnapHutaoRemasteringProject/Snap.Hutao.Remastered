// Copyright (c) 2026 DGP Studio. All rights reserved.
// Licensed under the MIT license.
// Adapted from Snap.Nicole.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.AI;
using Microsoft.UI.Xaml.Controls;
using Snap.Hutao.Remastered.Core.AI;
using Snap.Hutao.Remastered.Core.Logging;
using Snap.Hutao.Remastered.Resource.Localization.AI;
using Snap.Hutao.Remastered.Service.AI;
using Snap.Hutao.Remastered.Service.AI.Models;
using Snap.Hutao.Remastered.Service.AI.Settings;
using System.Diagnostics;

namespace Snap.Hutao.Remastered.ViewModel.AI.Settings;

[WinRT.GeneratedBindableCustomProperty]
[Service(ServiceLifetime.Scoped)]
internal sealed partial class SettingsModelConfigurationViewModel(IServiceProvider serviceProvider) : ObservableObject
{

    private readonly IModelProfileService modelProfileService = serviceProvider.GetRequiredService<IModelProfileService>();

    public AgentSettings Settings { get; } = serviceProvider.GetRequiredService<ISettingsProvider<AgentSettings>>().CurrentValue;

    public List<SettingsItem<EnumBox<ModelProviderType>>> ModelProviderTypes { get; } =
    [
        new("OpenAI Chat Completions | ~/chat/completions", EnumBox.Of(ModelProviderType.OpenAIChatCompletion)),
        new("OpenAI Responses | ~/responses", EnumBox.Of(ModelProviderType.OpenAIResponses)),
        new("Anthropic Messages | ~/v1/messages", EnumBox.Of(ModelProviderType.Anthropic)),
    ];

    public List<SettingsItem<EnumBox<ReasoningEffort>?>> ReasoningEfforts { get; } =
    [
        new(SHName.UIXamlPagesSettingsPageOptionProviderDefault, null),
        new(SHName.UIXamlPagesSettingsPageOptionReasoningEffortNone, EnumBox.Of(ReasoningEffort.None)),
        new(SHName.UIXamlPagesSettingsPageOptionReasoningEffortLow, EnumBox.Of(ReasoningEffort.Low)),
        new(SHName.UIXamlPagesSettingsPageOptionReasoningEffortMedium, EnumBox.Of(ReasoningEffort.Medium)),
        new(SHName.UIXamlPagesSettingsPageOptionReasoningEffortHigh, EnumBox.Of(ReasoningEffort.High)),
        new(SHName.UIXamlPagesSettingsPageOptionReasoningEffortExtraHigh, EnumBox.Of(ReasoningEffort.ExtraHigh)),
    ];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RefreshModelsCommand), nameof(TestConnectionCommand))]
    public partial bool IsModelListBusy { get; set; }

    [ObservableProperty]
    public partial StringResourceValue ModelListStatusTitle { get; set; } = string.Empty;

    [ObservableProperty]
    public partial StringResourceValue ModelListStatusMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsModelListStatusOpen { get; set; }

    [ObservableProperty]
    public partial InfoBarSeverity ModelListInfoBarSeverity { get; set; } = InfoBarSeverity.Informational;

    [field: MaybeNull]
    public IRelayCommand AddProfileCommand => field ??= new RelayCommand(AddProfile);

    [field: MaybeNull]
    public IRelayCommand CopyProfileCommand => field ??= new RelayCommand(CopyProfile);

    [field: MaybeNull]
    public IRelayCommand DeleteProfileCommand => field ??= new RelayCommand(DeleteProfile);

    [field: MaybeNull]
    public IRelayCommand AddModelCommand => field ??= new RelayCommand(AddModel);

    [field: MaybeNull]
    public IRelayCommand DeleteModelCommand => field ??= new RelayCommand(DeleteModel);

    [field: MaybeNull]
    public IRelayCommand ClearModelsCommand => field ??= new RelayCommand(ClearModels);

    [field: MaybeNull]
    public IAsyncRelayCommand RefreshModelsCommand => field ??= new AsyncRelayCommand(RefreshModelsAsync, () => CanRefreshModels);

    [field: MaybeNull]
    public IAsyncRelayCommand TestConnectionCommand => field ??= new AsyncRelayCommand(TestConnectionAsync, () => CanRefreshModels);

    private bool CanRefreshModels { get => !IsModelListBusy; }

    private void AddProfile()
    {
        SentrySdk.AddBreadcrumb(BreadcrumbFactory.CreateUI("Add provider profile", "SettingsModelConfigurationViewModel.Command"));

        ModelProviderProfile providerProfile = new();
        ModelProfile modelProfile = new();

        providerProfile.ModelProfiles.Add(modelProfile);
        providerProfile.ModelProfiles.CurrentItem = modelProfile;

        Settings.ModelProviderProfiles.Add(providerProfile);
        Settings.ModelProviderProfiles.CurrentItem = providerProfile;
    }

    private void CopyProfile()
    {
        if (Settings.ModelProviderProfiles.CurrentItem is not { } selected)
        {
            return;
        }

        SentrySdk.AddBreadcrumb(BreadcrumbFactory.CreateUI("Copy provider profile", "SettingsModelConfigurationViewModel.Command"));

        ModelProviderProfile copy = new();
        copy.CopyFrom(selected);

        copy.Id = Guid.NewGuid();

        ModelProfile? selectedModelProfile = copy.ModelProfiles.CurrentItem;
        foreach (ModelProfile modelProfile in copy.ModelProfiles)
        {
            modelProfile.Id = Guid.NewGuid();
        }

        copy.ModelProfiles.CurrentItem = selectedModelProfile;

        int selectedIndex = Settings.ModelProviderProfiles.IndexOf(selected);
        int targetIndex = selectedIndex < 0 ? Settings.ModelProviderProfiles.Count : selectedIndex + 1;

        Settings.ModelProviderProfiles.Insert(targetIndex, copy);
        Settings.ModelProviderProfiles.CurrentItem = copy;
    }

    private void DeleteProfile()
    {
        if (Settings.ModelProviderProfiles.CurrentItem is not { } selected)
        {
            return;
        }

        SentrySdk.AddBreadcrumb(BreadcrumbFactory.CreateUI("Delete provider profile", "SettingsModelConfigurationViewModel.Command"));
        Settings.ModelProviderProfiles.Remove(selected);
    }

    private void AddModel()
    {
        if (Settings.ModelProviderProfiles.CurrentItem is not { } providerProfile)
        {
            return;
        }

        SentrySdk.AddBreadcrumb(BreadcrumbFactory.CreateUI("Add model profile", "SettingsModelConfigurationViewModel.Command"));
        ModelProfile modelProfile = new();

        providerProfile.ModelProfiles.Add(modelProfile);
        providerProfile.ModelProfiles.CurrentItem = modelProfile;
    }

    private void DeleteModel()
    {
        if (Settings.ModelProviderProfiles.CurrentItem?.ModelProfiles is not { } modelProfiles)
        {
            return;
        }

        if (modelProfiles.CurrentItem is not { } selected)
        {
            return;
        }

        SentrySdk.AddBreadcrumb(BreadcrumbFactory.CreateUI("Delete model profile", "SettingsModelConfigurationViewModel.Command"));
        modelProfiles.Remove(selected);
    }

    private void ClearModels()
    {
        SentrySdk.AddBreadcrumb(BreadcrumbFactory.CreateUI("Clear model profiles", "SettingsModelConfigurationViewModel.Command"));
        Settings.ModelProviderProfiles.CurrentItem?.ModelProfiles?.Clear();
    }

    private async Task RefreshModelsAsync(CancellationToken cancellationToken)
    {
        if (Settings.ModelProviderProfiles.CurrentItem is not { } providerProfile)
        {
            SetModelListStatus(InfoBarSeverity.Warning, SHName.UIXamlPagesSettingsPageModelListStatusFailedTitle, SHName.UIXamlPagesSettingsPageModelListStatusNoProviderMessage);
            return;
        }

        if (!string.IsNullOrWhiteSpace(providerProfile.ModelListDocumentationLink))
        {
            OpenModelListDocumentationLink(providerProfile.ModelListDocumentationLink);
            return;
        }

        if (string.IsNullOrWhiteSpace(providerProfile.ApiKey))
        {
            SetModelListStatus(InfoBarSeverity.Warning, SHName.UIXamlPagesSettingsPageModelListStatusFailedTitle, SHName.UIXamlPagesSettingsPageModelListStatusApiKeyMissingMessage);
            return;
        }

        IsModelListBusy = true;
        SetModelListStatus(InfoBarSeverity.Informational, SHName.UIXamlPagesSettingsPageModelListStatusFetchingTitle, SHName.UIXamlPagesSettingsPageModelListStatusFetchingMessage);

        try
        {
            IReadOnlyList<ModelProfile> modelProfiles = await modelProfileService.GetModelsAsync(providerProfile, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            providerProfile.ModelProfiles = ModelProfileMerger.CreateMerged(providerProfile.ModelProfiles, modelProfiles);

            SetModelListStatus(InfoBarSeverity.Success, SHName.UIXamlPagesSettingsPageModelListStatusSuccessTitle, StringResourceValue.FromName(SHName.UIXamlPagesSettingsPageModelListStatusSuccessMessage, modelProfiles.Count));
        }
        catch (OperationCanceledException)
        {
            SetModelListStatus(InfoBarSeverity.Warning, SHName.UIXamlPagesSettingsPageModelListStatusFailedTitle, SHName.UIXamlPagesSettingsPageModelListStatusCanceledMessage);
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
            SetModelListStatus(InfoBarSeverity.Error, SHName.UIXamlPagesSettingsPageModelListStatusFailedTitle, ex.Message);
        }
        finally
        {
            IsModelListBusy = false;
        }
    }

    private void OpenModelListDocumentationLink(string link)
    {
        string trimmedLink = link.Trim();
        if (!Uri.TryCreate(trimmedLink, UriKind.Absolute, out Uri? uri) || uri.Scheme is not "http" and not "https")
        {
            SetModelListStatus(InfoBarSeverity.Error, SHName.UIXamlPagesSettingsPageModelListStatusFailedTitle, SHName.UIXamlPagesSettingsPageModelListStatusInvalidDocumentationLinkMessage);
            return;
        }

        try
        {
            _ = Process.Start(new ProcessStartInfo
            {
                FileName = uri.AbsoluteUri,
                UseShellExecute = true,
            });

            SetModelListStatus(InfoBarSeverity.Informational, SHName.UIXamlPagesSettingsPageModelListStatusDocumentationOpenedTitle, SHName.UIXamlPagesSettingsPageModelListStatusDocumentationOpenedMessage);
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException)
        {
            SentrySdk.CaptureException(ex);
            SetModelListStatus(InfoBarSeverity.Error, SHName.UIXamlPagesSettingsPageModelListStatusFailedTitle, StringResourceValue.FromText(ex.Message));
        }
    }

    private async Task TestConnectionAsync(CancellationToken cancellationToken)
    {
        if (Settings.ModelProviderProfiles.CurrentItem is not { } provider || provider.ModelProfiles.CurrentItem is not { } model || string.IsNullOrWhiteSpace(model.ModelId))
        {
            SetModelListStatus(InfoBarSeverity.Warning, SHName.UIXamlPagesSettingsPageModelListStatusFailedTitle, SHName.ViewDialogAccountInsightsMissingModel);
            return;
        }
        if (string.IsNullOrWhiteSpace(provider.ApiKey))
        {
            SetModelListStatus(InfoBarSeverity.Warning, SHName.UIXamlPagesSettingsPageModelListStatusFailedTitle, SHName.ViewDialogAccountInsightsMissingApiKey);
            return;
        }
        IsModelListBusy = true;
        try
        {
            ExtendedAgentOptions options = ExtendedAgentOptions.Create(provider, model, Settings.AgentOptions) with { MaxOutputTokens = 64 };
            using IChatClient client = serviceProvider.GetRequiredService<AgentChatClientFactory>().Create(options);
            ChatResponse response = await client.GetResponseAsync((ChatMessage[])[new ChatMessage(ChatRole.User, "Reply only OK. This is a connection test without account data.")], new ChatOptions { ModelId = model.ModelId, MaxOutputTokens = 64 }, cancellationToken);
            SetModelListStatus(InfoBarSeverity.Success, SHName.ViewDialogAccountInsightsTestConnection,
                string.IsNullOrWhiteSpace(response.Text) ? SHName.ViewDialogAccountInsightsEmptyReply : SHName.ViewDialogAccountInsightsTestSuccess);
        }
        catch (Exception exception)
        {
            SetModelListStatus(InfoBarSeverity.Error, SHName.UIXamlPagesSettingsPageModelListStatusFailedTitle, exception.Message);
        }
        finally
        {
            IsModelListBusy = false;
        }
    }

    private void SetModelListStatus(InfoBarSeverity severity, StringResourceValue title, StringResourceValue message)
    {
        ModelListInfoBarSeverity = severity;
        ModelListStatusTitle = title;
        ModelListStatusMessage = message;
        IsModelListStatusOpen = true;
    }
}
