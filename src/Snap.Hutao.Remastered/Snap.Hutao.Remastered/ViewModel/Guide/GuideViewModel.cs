// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Windows.AppLifecycle;
using Snap.Hutao.Remastered.Core;
using Snap.Hutao.Remastered.Core.Logging;
using Snap.Hutao.Remastered.Core.Setting;
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

    #endregion

    // 抄写校验相关
    /// <summary>
    /// 用户输入的抄写文本
    /// </summary>
    public string? ConfirmCopyText
    {
        get => field;
        set
        {
            if (SetProperty(ref field, value))
            {
                UpdateConfirmCopyState();
            }
        }
    }

    /// <summary>
    /// 抄写相似度，0.0 - 1.0
    /// </summary>
    public double ConfirmCopyAccuracy { get => field; set => SetProperty(ref field, value); }

    /// <summary>
    /// 抄写进度文本（百分比）用于 XAML 显示
    /// </summary>
    public string? ConfirmCopyProgressText { get => field; set => SetProperty(ref field, value); }

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

    public bool AgreementIsSingleLine => string.IsNullOrEmpty(SH.ViewGuideAgreementCopyTextLine1);

    public bool AgreementUseGrid => CultureOptions.LocaleName.StartsWith("zh", StringComparison.OrdinalIgnoreCase);

    private void UpdateConfirmCopyState()
    {
        string target = AgreementCopyTarget;
        string input = ConfirmCopyText ?? string.Empty;

        double accuracy = 0;
        if (target.Length == 0 && input.Length == 0)
        {
            accuracy = 1;
        }
        else
        {
            int dist = LevenshteinDistance(target, input);
            int max = Math.Max(target.Length, input.Length);
            accuracy = max == 0 ? 1 : 1.0 - (double)dist / max;
            accuracy = Math.Clamp(accuracy, 0, 1);
        }

        ConfirmCopyAccuracy = accuracy;
        ConfirmCopyProgressText = $"{(int)(accuracy * 100)}%";

        OnAgreementStateChanged();
    }

    private static int LevenshteinDistance(string? a, string? b)
    {
        if (string.IsNullOrEmpty(a))
        {
            return string.IsNullOrEmpty(b) ? 0 : b!.Length;
        }

        if (string.IsNullOrEmpty(b))
        {
            return a!.Length;
        }

        int n = a!.Length;
        int m = b!.Length;
        int[,] d = new int[n + 1, m + 1];

        for (int i = 0; i <= n; i++) d[i, 0] = i;
        for (int j = 0; j <= m; j++) d[0, j] = j;

        for (int i = 1; i <= n; i++)
        {
            for (int j = 1; j <= m; j++)
            {
                int cost = a[i - 1] == b[j - 1] ? 0 : 1;
                d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
            }
        }

        return d[n, m];
    }

    public ObservableCollection<DownloadSummary>? DownloadSummaries { get; set => SetProperty(ref field, value); }

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

    private static ObservableCollection<DownloadSummary> GetUnfulfilledCategoryCollection(IServiceProvider serviceProvider)
    {
        return StaticResource
            .GetUnfulfilledCategorySet()
            .Select(category => new DownloadSummary(serviceProvider, category))
            .ToObservableCollection();
    }

    [Command("NextOrCompleteCommand")]
    private void NextOrComplete()
    {
        SentrySdk.AddBreadcrumb(BreadcrumbFactory.CreateUI("Increase guide state", "GuideViewModel.Command"));

        ++State;
    }

    [Command("SetDataFolderCommand")]
    private async Task SetDataFolderAsync()
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

    private void OnAgreementStateChanged()
    {
        // 在文档步骤中，需要额外验证抄写相似度达到 80%
        // 使用底层存储获取状态，避免访问 State getter 导致的副作用（State getter 会重置同意项）
        GuideState current = UnsafeLocalSetting.Get(SettingKeys.GuideState, GuideState.Language);
        if (current == GuideState.Document)
        {
            IsNextOrCompleteButtonEnabled = IsTermOfServiceAgreed && IsPrivacyPolicyAgreed && IsIssueReportAgreed && IsOpenSourceLicenseAgreed && ConfirmCopyAccuracy >= 0.8;
        }
        else
        {
            IsNextOrCompleteButtonEnabled = IsTermOfServiceAgreed && IsPrivacyPolicyAgreed && IsIssueReportAgreed && IsOpenSourceLicenseAgreed;
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
}