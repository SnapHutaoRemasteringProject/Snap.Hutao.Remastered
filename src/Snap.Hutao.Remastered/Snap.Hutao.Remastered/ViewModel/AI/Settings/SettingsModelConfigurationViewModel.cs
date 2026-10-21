// Copyright (c) 2026 DGP Studio. All rights reserved.
// Licensed under the MIT license.
// Adapted from Snap.Nicole.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.AI;
using Microsoft.UI.Xaml.Controls;
using Sentry;
using Snap.Hutao.Remastered.Core.AI;
using Snap.Hutao.Remastered.Core.AI.Diagnostics;
using Snap.Hutao.Remastered.Resource.Localization.AI;
using Snap.Hutao.Remastered.Service.AI;
using Snap.Hutao.Remastered.Service.AI.Models;
using Snap.Hutao.Remastered.Service.AI.Settings;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Snap.Hutao.Remastered.ViewModel.AI.Settings;

[Service(ServiceLifetime.Scoped)]
internal sealed partial class SettingsModelConfigurationViewModel(IServiceProvider serviceProvider) : ObservableObject
{
    private const string RemovedModelNameSuffix = " [Removed]";

    private readonly IModelProfileService modelProfileService = serviceProvider.GetRequiredService<IModelProfileService>();

    public AgentSettings Settings { get; } = serviceProvider.GetRequiredService<ISettingsProvider<AgentSettings>>().CurrentValue;

    public IReadOnlyList<SettingsItem<EnumBox<ModelProviderType>>> ModelProviderTypes { get; } =
    [
        new("OpenAI Chat Completions | ~/chat/completions", EnumBox.Of(ModelProviderType.OpenAIChatCompletion)),
        new("OpenAI Responses | ~/responses", EnumBox.Of(ModelProviderType.OpenAIResponses)),
        new("Anthropic Messages | ~/v1/messages", EnumBox.Of(ModelProviderType.Anthropic)),
    ];

    public IReadOnlyList<SettingsItem<EnumBox<ReasoningEffort>?>> ReasoningEfforts { get; } =
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

    private bool CanRefreshModels { get => !IsModelListBusy; }

    private static void MergeModelProfiles(ObservableSettingsCollection<ModelProfile, Guid> destination, IReadOnlyList<ModelProfile> source)
    {
        // There can potentially be models with the same ModelId, so we didn't use ToDictionary.
        Dictionary<string, ModelProfile> existingMap = new(StringComparer.Ordinal);
        foreach (ModelProfile destModelProfile in destination)
        {
            if (!string.IsNullOrWhiteSpace(destModelProfile.ModelId))
            {
                existingMap.TryAdd(destModelProfile.ModelId, destModelProfile);
            }
        }

        HashSet<string> visitedModelIds = new(StringComparer.Ordinal);
        bool shouldSelectFirstFetched = destination.CurrentItem is null || string.IsNullOrWhiteSpace(destination.CurrentItem.ModelId);
        ModelProfile? firstFetched = null;
        int targetIndex = 0;

        foreach (ModelProfile sourceModelProfile in source)
        {
            if (string.IsNullOrWhiteSpace(sourceModelProfile.ModelId) || !visitedModelIds.Add(sourceModelProfile.ModelId))
            {
                continue;
            }

            if (!existingMap.TryGetValue(sourceModelProfile.ModelId, out ModelProfile? current))
            {
                current = new();
                destination.Insert(targetIndex, current);
            }
            else
            {
                int currentIndex = destination.IndexOf(current);
                if (currentIndex != targetIndex)
                {
                    destination.Move(currentIndex, targetIndex);
                }
            }

            // Keep Id stable because conversations and selections reference model profiles by Guid.
            current.Name = sourceModelProfile.Name;
            current.ModelId = sourceModelProfile.ModelId;

            firstFetched ??= current;
            targetIndex++;
        }

        for (int i = targetIndex; i < destination.Count; i++)
        {
            ModelProfile modelProfile = destination[i];
            if (string.IsNullOrWhiteSpace(modelProfile.ModelId) || visitedModelIds.Contains(modelProfile.ModelId))
            {
                continue;
            }

            modelProfile.Name = GetRemovedModelProfileName(modelProfile);
        }

        if (shouldSelectFirstFetched && firstFetched is not null)
        {
            destination.CurrentItem = firstFetched;
        }
    }

    private static string GetRemovedModelProfileName(ModelProfile modelProfile)
    {
        string name = string.IsNullOrWhiteSpace(modelProfile.Name) ? modelProfile.ModelId : modelProfile.Name;
        if (name.EndsWith(RemovedModelNameSuffix, StringComparison.Ordinal))
        {
            return name;
        }

        return $"{name}{RemovedModelNameSuffix}";
    }

    [Command("AddProfileCommand")]
    private void AddProfile()
    {
        SentryDiagnostics.AddBreadcrumb("Add provider profile", SentryBreadcrumbCategories.SettingsModelProfiles, SentryBreadcrumbTypes.UI);

        ModelProviderProfile providerProfile = new();
        ModelProfile modelProfile = new();

        providerProfile.ModelProfiles.Add(modelProfile);
        providerProfile.ModelProfiles.CurrentItem = modelProfile;

        Settings.ModelProviderProfiles.Add(providerProfile);
        Settings.ModelProviderProfiles.CurrentItem = providerProfile;
    }

    [Command("CopyProfileCommand")]
    private void CopyProfile()
    {
        if (Settings.ModelProviderProfiles.CurrentItem is not { } selected)
        {
            return;
        }

        SentryDiagnostics.AddBreadcrumb("Copy provider profile", SentryBreadcrumbCategories.SettingsModelProfiles, SentryBreadcrumbTypes.UI);

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

    [Command("DeleteProfileCommand")]
    private void DeleteProfile()
    {
        if (Settings.ModelProviderProfiles.CurrentItem is not { } selected)
        {
            return;
        }

        SentryDiagnostics.AddBreadcrumb("Delete provider profile", SentryBreadcrumbCategories.SettingsModelProfiles, SentryBreadcrumbTypes.UI);
        Settings.ModelProviderProfiles.Remove(selected);
    }

    [Command("AddModelCommand")]
    private void AddModel()
    {
        if (Settings.ModelProviderProfiles.CurrentItem is not { } providerProfile)
        {
            return;
        }

        SentryDiagnostics.AddBreadcrumb("Add model profile", SentryBreadcrumbCategories.SettingsModelProfiles, SentryBreadcrumbTypes.UI);
        ModelProfile modelProfile = new();

        providerProfile.ModelProfiles.Add(modelProfile);
        providerProfile.ModelProfiles.CurrentItem = modelProfile;
    }

    [Command("DeleteModelCommand")]
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

        SentryDiagnostics.AddBreadcrumb("Delete model profile", SentryBreadcrumbCategories.SettingsModelProfiles, SentryBreadcrumbTypes.UI);
        modelProfiles.Remove(selected);
    }

    [Command("ClearModelsCommand")]
    private void ClearModels()
    {
        SentryDiagnostics.AddBreadcrumb("Clear model profiles", SentryBreadcrumbCategories.SettingsModelProfiles, SentryBreadcrumbTypes.UI);
        Settings.ModelProviderProfiles.CurrentItem?.ModelProfiles?.Clear();
    }

    [RelayCommand(CanExecute = nameof(CanRefreshModels))]
    private async Task RefreshModelsAsync(CancellationToken cancellationToken)
    {
        using SentryDiagnosticSpan span = SentryDiagnostics.StartSpan(SentryOperations.SettingsModelProfilesRefresh, "Refresh model list");

        if (Settings.ModelProviderProfiles.CurrentItem is not { } providerProfile)
        {
            SetModelListStatus(InfoBarSeverity.Warning, SHName.UIXamlPagesSettingsPageModelListStatusFailedTitle, SHName.UIXamlPagesSettingsPageModelListStatusNoProviderMessage);
            span.Finish(SpanStatus.FailedPrecondition);
            return;
        }

        span.SetTag(SentryTags.AIProvider, providerProfile.ProviderType.Value.ToString());

        if (!string.IsNullOrWhiteSpace(providerProfile.ModelListDocumentationLink))
        {
            OpenModelListDocumentationLink(providerProfile.ModelListDocumentationLink);
            return;
        }

        if (string.IsNullOrWhiteSpace(providerProfile.ApiKey))
        {
            SetModelListStatus(InfoBarSeverity.Warning, SHName.UIXamlPagesSettingsPageModelListStatusFailedTitle, SHName.UIXamlPagesSettingsPageModelListStatusApiKeyMissingMessage);
            span.Finish(SpanStatus.FailedPrecondition);
            return;
        }

        IsModelListBusy = true;
        SetModelListStatus(InfoBarSeverity.Informational, SHName.UIXamlPagesSettingsPageModelListStatusFetchingTitle, SHName.UIXamlPagesSettingsPageModelListStatusFetchingMessage);

        try
        {
            IReadOnlyList<ModelProfile> modelProfiles = await modelProfileService.GetModelsAsync(providerProfile, cancellationToken);
            MergeModelProfiles(providerProfile.ModelProfiles, modelProfiles);

            SetModelListStatus(InfoBarSeverity.Success, SHName.UIXamlPagesSettingsPageModelListStatusSuccessTitle, StringResourceValue.FromName(SHName.UIXamlPagesSettingsPageModelListStatusSuccessMessage, modelProfiles.Count));
            span.SetData(SentryData.AIModelCount, modelProfiles.Count);
        }
        catch (OperationCanceledException)
        {
            SetModelListStatus(InfoBarSeverity.Warning, SHName.UIXamlPagesSettingsPageModelListStatusFailedTitle, SHName.UIXamlPagesSettingsPageModelListStatusCanceledMessage);
            span.Finish(SpanStatus.Cancelled);
        }
        catch (Exception ex)
        {
            SentryDiagnostics.CaptureException(ex, span, SentryOperations.SettingsModelProfilesRefresh);
            SetModelListStatus(InfoBarSeverity.Error, SHName.UIXamlPagesSettingsPageModelListStatusFailedTitle, ex.Message);
        }
        finally
        {
            IsModelListBusy = false;
        }
    }

    private void OpenModelListDocumentationLink(string link)
    {
        using SentryDiagnosticSpan span = SentryDiagnostics.StartSpan(SentryOperations.SettingsModelProfilesOpenDocs, "Open model list documentation");

        string trimmedLink = link.Trim();
        if (!Uri.TryCreate(trimmedLink, UriKind.Absolute, out Uri? uri) || uri.Scheme is not "http" and not "https")
        {
            SetModelListStatus(InfoBarSeverity.Error, SHName.UIXamlPagesSettingsPageModelListStatusFailedTitle, SHName.UIXamlPagesSettingsPageModelListStatusInvalidDocumentationLinkMessage);
            span.Finish(SpanStatus.InvalidArgument);
            return;
        }

        try
        {
            span.SetTag(SentryTags.UrlScheme, uri.Scheme);
            _ = Process.Start(new ProcessStartInfo
            {
                FileName = uri.AbsoluteUri,
                UseShellExecute = true,
            });

            SetModelListStatus(InfoBarSeverity.Informational, SHName.UIXamlPagesSettingsPageModelListStatusDocumentationOpenedTitle, SHName.UIXamlPagesSettingsPageModelListStatusDocumentationOpenedMessage);
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException)
        {
            SentryDiagnostics.CaptureException(ex, span, SentryOperations.SettingsModelProfilesOpenDocs);
            SetModelListStatus(InfoBarSeverity.Error, SHName.UIXamlPagesSettingsPageModelListStatusFailedTitle, StringResourceValue.FromText(ex.Message));
        }
    }

    [RelayCommand(CanExecute = nameof(CanRefreshModels))]
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
            ChatResponse response = await client.GetResponseAsync([new ChatMessage(ChatRole.User, "Reply only OK. This is a connection test without account data.")], new ChatOptions { ModelId = model.ModelId, MaxOutputTokens = 64 }, cancellationToken);
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
