// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Windows.AppLifecycle;
using Snap.Hutao.Remastered.Core;
using Snap.Hutao.Remastered.Core.Logging;
using Snap.Hutao.Remastered.Core.Setting;
using Snap.Hutao.Remastered.Core.Shell;
using Snap.Hutao.Remastered.Factory.ContentDialog;
using Snap.Hutao.Remastered.Factory.Picker;
using Snap.Hutao.Remastered.Model;
using Snap.Hutao.Remastered.Service;
using Snap.Hutao.Remastered.Service.Notification;
using Snap.Hutao.Remastered.ViewModel.Setting;
using Snap.Hutao.Remastered.Web.Hoyolab;
using Snap.Hutao.Remastered.Web.Hutao;
using Snap.Hutao.Remastered.Web.Hutao.Response;
using Snap.Hutao.Remastered.Web.Response;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;

namespace Snap.Hutao.Remastered.ViewModel.Guide;

[BindableCustomPropertyProvider]
[Service(ServiceLifetime.Singleton)]
public sealed partial class GuideViewModel : Abstraction.ViewModel
{
    private readonly IFileSystemPickerInteraction fileSystemPickerInteraction;
    private readonly IContentDialogFactory contentDialogFactory;
    private readonly IServiceProvider serviceProvider;
    private readonly ITaskContext taskContext;
    private readonly IMessenger messenger;
    private readonly IShellLinkInterop shellLinkInterop;

    [GeneratedConstructor]
    public partial GuideViewModel(IServiceProvider serviceProvider);

    public static string AllCulturesWelcomeText
    {
        get => string.Join('+', CultureOptions.Cultures.Select(c => SH.GetString("GuideWindowTitle", c.Value)));
    }

    public uint State
    {
        get
        {
            GuideState state = UnsafeLocalSetting.Get(SettingKeys.GuideState, GuideState.Language);

            switch (state)
            {
                case GuideState.Document:
                    IsTermOfServiceAgreed = false;
                    IsPrivacyPolicyAgreed = false;
                    IsIssueReportAgreed = false;
                    IsOpenSourceLicenseAgreed = false;
                    IsAgreementCopyAgreed = false;
                    (NextOrCompleteButtonText, IsNextOrCompleteButtonEnabled) = (SH.ViewModelGuideActionNext, false);
                    break;
                case GuideState.StaticResourceBegin:
                    (NextOrCompleteButtonText, IsNextOrCompleteButtonEnabled) = (SH.ViewModelGuideActionStaticResourceBegin, false);
                    DownloadStaticResourceAsync().SafeForget();
                    break;
                case GuideState.Completed:
                    (NextOrCompleteButtonText, IsNextOrCompleteButtonEnabled) = (SH.ViewModelGuideActionComplete, true);
                    break;
                default:
                    (NextOrCompleteButtonText, IsNextOrCompleteButtonEnabled) = (SH.ViewModelGuideActionNext, true);
                    break;
            }

            return (uint)state;
        }

        set
        {
            value = Math.Clamp(value, 0, (uint)GuideState.Completed);
            LocalSetting.Set(SettingKeys.GuideState, value);
            OnPropertyChanged();
        }
    }

    public string NextOrCompleteButtonText { get; set => SetProperty(ref field, value); } = SH.ViewModelGuideActionNext;

    public bool IsNextOrCompleteButtonEnabled { get; set => SetProperty(ref field, value); } = true;

    public bool IsSetDataFolderEnabled { get; set => SetProperty(ref field, value); } = true;

    public partial CultureOptions CultureOptions { get; }

    public partial RuntimeOptions RuntimeOptions { get; }

    public partial AppOptions AppOptions { get; }

    public partial StaticResourceOptions StaticResourceOptions { get; }

    // TODO: Replace with IObservableProperty
    public NameCultureInfoValue? SelectedCulture
    {
        get => field ??= Selection.Initialize(CultureOptions.Cultures, CultureOptions.CurrentCulture.Value);
        set
        {
            if (SetProperty(ref field, value) && value is not null)
            {
                CultureOptions.CurrentCulture.Value = value.Value;
                AppInstance.Restart(string.Empty);
            }
        }
    }

    // TODO: Replace with IObservableProperty
    public NameValue<Region>? SelectedRegion
    {
        get => field ??= Selection.Initialize(AppOptions.LazyRegions, AppOptions.Region.Value);
        set
        {
            if (SetProperty(ref field, value) && value is not null)
            {
                AppOptions.Region.Value = value.Value;
            }
        }
    }

    #region Agreement

    public bool IsTermOfServiceAgreed
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                OnAgreementStateChanged();
            }
        }
    }

    public bool IsPrivacyPolicyAgreed
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                OnAgreementStateChanged();
            }
        }
    }

    public bool IsIssueReportAgreed
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                OnAgreementStateChanged();
            }
        }
    }

    public bool IsOpenSourceLicenseAgreed
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                OnAgreementStateChanged();
            }
        }
    }

    public bool IsAgreementCopyAgreed
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                OnAgreementStateChanged();
            }
        }
    }

    #endregion

    public string AgreementCopyTarget
    {
        get
        {
            string line1 = SH.ViewGuideAgreementCopyTextLine1;
            string line2 = SH.ViewGuideAgreementCopyTextLine2;

            if (string.IsNullOrEmpty(line1))
            {
                return line2 ?? string.Empty;
            }

            return (line1 ?? string.Empty) + Environment.NewLine + (line2 ?? string.Empty);
        }
    }

    public bool AgreementUseGrid => CultureOptions.LocaleName.StartsWith("zh", StringComparison.OrdinalIgnoreCase);

    public ObservableCollection<DownloadSummary>? DownloadSummaries { get; set => SetProperty(ref field, value); }

    private void OnAgreementStateChanged()
    {
        // 使用底层存储获取状态，避免访问 State getter 导致的副作用（State getter 会重置同意项）
        GuideState current = UnsafeLocalSetting.Get(SettingKeys.GuideState, GuideState.Language);
        if (current == GuideState.Document)
        {
            IsNextOrCompleteButtonEnabled = IsTermOfServiceAgreed
                && IsPrivacyPolicyAgreed
                && IsIssueReportAgreed
                && IsOpenSourceLicenseAgreed
                && IsAgreementCopyAgreed;
        }
        else
        {
            IsNextOrCompleteButtonEnabled = IsTermOfServiceAgreed && IsPrivacyPolicyAgreed && IsIssueReportAgreed && IsOpenSourceLicenseAgreed;
        }
    }

    [Command("NextOrCompleteCommand")]
    private void NextOrComplete()
    {
        SentrySdk.AddBreadcrumb(BreadcrumbFactory.CreateUI("Increase guide state", "GuideViewModel.Command"));

        ++State;
    }

    [Command("CreateGameLaunchShortcutCommand")]
    private void CreateGameLaunchShortcut()
    {
        SentrySdk.AddBreadcrumb(BreadcrumbFactory.CreateUI("Create game launch shortcut", "GuideViewModel.Command"));

        _ = shellLinkInterop.TryCreateGameLaunchShortcut()
            ? messenger.Send(InfoBarMessage.Success(SH.ViewModelSettingActionComplete))
            : messenger.Send(InfoBarMessage.Warning(SH.ViewModelSettingCreateDesktopShortcutFailed));
    }

    [Command("CreateDesktopShortcutCommand")]
    private void CreateDesktopShortcutForElevatedLaunchAsync()
    {
        SentrySdk.AddBreadcrumb(BreadcrumbFactory.CreateUI("Create desktop shortcut for elevated launch", "SettingViewModel.Command"));

        _ = shellLinkInterop.TryCreateDesktopShortcut()
            ? messenger.Send(InfoBarMessage.Success(SH.ViewModelSettingActionComplete))
            : messenger.Send(InfoBarMessage.Warning(SH.ViewModelSettingCreateDesktopShortcutFailed));
    }

    [Command("SetDataFolderCommand")]
    private async Task SetDataFolderAsync()
    {
        // prevent reentrancy
        if (!IsSetDataFolderEnabled)
        {
            return;
        }

        IsSetDataFolderEnabled = false;
        try
        {
            SentrySdk.AddBreadcrumb(BreadcrumbFactory.CreateUI("Set data folder path", "GuideViewModel.Command"));

            SettingStorageSetDataFolderOperation operation = new()
            {
                FileSystemPickerInteraction = fileSystemPickerInteraction,
                ContentDialogFactory = contentDialogFactory,
                Messenger = messenger,
            };

            if (await operation.TryExecuteAsync().ConfigureAwait(false))
            {
                try
                {
                    AppInstance.Restart(string.Empty);
                }
                catch (COMException ex)
                {
                    messenger.Send(InfoBarMessage.Error(ex));
                }
            }
        }
        finally
        {
            IsSetDataFolderEnabled = true;
        }
    }

    [SuppressMessage("", "SH003")]
    private async Task DownloadStaticResourceAsync()
    {
        DownloadSummaries = GetUnfulfilledCategoryCollection(serviceProvider);

        // Pass a collection copy, so that we can remove element in loop
        await Parallel.ForEachAsync((DownloadSummary[])[.. DownloadSummaries], async (summary, token) =>
        {
            if (await summary.DownloadAndExtractAsync().ConfigureAwait(true))
            {
                taskContext.InvokeOnMainThread(() => DownloadSummaries.Remove(summary));
            }
        }).ConfigureAwait(false);

        StaticResource.FulfillAll();
        UnsafeLocalSetting.Set(SettingKeys.GuideState, GuideState.Completed);
        AppInstance.Restart(string.Empty);
    }

    private static ObservableCollection<DownloadSummary> GetUnfulfilledCategoryCollection(IServiceProvider serviceProvider)
    {
        return StaticResource
            .GetUnfulfilledCategorySet()
            .Select(category => new DownloadSummary(serviceProvider, category))
            .ToObservableCollection();
    }

    protected override async ValueTask<bool> LoadOverrideAsync(CancellationToken token)
    {
        using (IServiceScope scope = serviceProvider.CreateScope())
        {
            HutaoInfrastructureClient hutaoInfrastructureClient = scope.ServiceProvider.GetRequiredService<HutaoInfrastructureClient>();
            HutaoResponse<StaticResourceSizeInformation> response = await hutaoInfrastructureClient.GetStaticSizeAsync(token).ConfigureAwait(false);
            if (ResponseValidator.TryValidate(response, scope.ServiceProvider, out StaticResourceSizeInformation? sizeInformation))
            {
                await taskContext.SwitchToMainThreadAsync();
                StaticResourceOptions.SizeInformation = sizeInformation;
            }

            return true;
        }
    }
}