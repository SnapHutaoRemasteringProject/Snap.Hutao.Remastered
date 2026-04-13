// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.Common;
using CommunityToolkit.Mvvm.ComponentModel;
using Snap.Hutao.Remastered.Core.DependencyInjection.Abstraction;
using Snap.Hutao.Remastered.Core.Setting;
using Snap.Hutao.Remastered.Service;
using Snap.Hutao.Remastered.Service.Announcement;
using Snap.Hutao.Remastered.Service.Hutao;
using Snap.Hutao.Remastered.Service.Network;
using Snap.Hutao.Remastered.Service.User;
using Snap.Hutao.Remastered.UI.Xaml.Control.Card;
using Snap.Hutao.Remastered.UI.Xaml.View.Card;
using Snap.Hutao.Remastered.Web;
using Snap.Hutao.Remastered.Web.Hoyolab.Bbs.Home;
using Snap.Hutao.Remastered.Web.Hoyolab.Hk4e.Common.Announcement;
using Snap.Hutao.Remastered.Web.Hoyolab.Takumi.Event.Miyolive;
using Snap.Hutao.Remastered.Web.Hoyolab.Takumi.GameRecord;
using Snap.Hutao.Remastered.Web.Hoyolab.Takumi.GameRecord.ActCalendar;
using Snap.Hutao.Remastered.Web.Request.Builder;
using Snap.Hutao.Remastered.Web.Response;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Text.RegularExpressions;

namespace Snap.Hutao.Remastered.ViewModel.Home;

[BindableCustomPropertyProvider]
[Service(ServiceLifetime.Scoped)]
internal sealed partial class AnnouncementViewModel : Abstraction.ViewModel
{
    private readonly IAnnouncementService announcementService;
    private readonly IServiceProvider serviceProvider;
    private readonly IHutaoAsAService hutaoAsAService;
    private readonly CultureOptions cultureOptions;
    private readonly ITaskContext taskContext;
    private readonly AppOptions appOptions;
    private readonly INetworkRetryCoordinator networkRetryCoordinator;

    private IDisposable? homeRetryRegistration;
    private int shouldRefreshDashboardOnSuccess;

    [GeneratedConstructor]
    public partial AnnouncementViewModel(IServiceProvider serviceProvider);

    [ObservableProperty]
    public partial AnnouncementWrapper? Announcement { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<Web.Hutao.HutaoAsAService.Announcement>? HutaoAnnouncements { get; set; }

    [ObservableProperty]
    public partial string GreetingText { get; set; } = SH.ViewPageHomeGreetingTextDefault;

    [ObservableProperty]
    public partial ImmutableArray<CodeWrapper> RedeemCodes { get; set; } = [];

    [ObservableProperty]
    public partial List<CardReference>? Cards { get; set; }

    [ObservableProperty]
    public partial ActCalendar ActivityCalendar { get; set; }

    [ObservableProperty]
    public partial ImmutableArray<Act> DisplayedActivityCards { get; set; } = [];

    [GeneratedRegex("act_id=(.*?)&")]
    private static partial Regex ActIdExtractor { get; }

    protected override async ValueTask<bool> LoadOverrideAsync(CancellationToken token)
    {
        homeRetryRegistration ??= networkRetryCoordinator.Register("AnnouncementViewModel.LoadHome", RetryHomeAsync);
        await taskContext.SwitchToMainThreadAsync();
        RefreshDashboard();
        UpdateGreetingText();

        RetryHomeAsync(token).AsTask().SafeForget();
        return true;
    }

    protected override void UninitializeOverride()
    {
        homeRetryRegistration?.Dispose();
        homeRetryRegistration = default;
    }

    [SuppressMessage("", "SH003")]
    private async ValueTask<bool> InitializeInGameAnnouncementAsync(CancellationToken token)
    {
        try
        {
            AnnouncementWrapper? announcementWrapper = await announcementService.GetAnnouncementWrapperAsync(cultureOptions.LanguageCode, appOptions.Region.Value, token).ConfigureAwait(false);
            if (announcementWrapper is null)
            {
                MarkHomePendingForRetry();
                return false;
            }

            await taskContext.SwitchToMainThreadAsync();
            Announcement = announcementWrapper;
            DeferContentLoader?.Load("GameAnnouncementPivot");
            return true;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) when (IsNetworkRelatedException(ex))
        {
            MarkHomePendingForRetry();
            return false;
        }

        return false;
    }

    [SuppressMessage("", "SH003")]
    private async ValueTask<bool> InitializeHutaoAnnouncementAsync(CancellationToken token)
    {
        try
        {
            ObservableCollection<Web.Hutao.HutaoAsAService.Announcement>? hutaoAnnouncements = await hutaoAsAService.GetHutaoAnnouncementCollectionAsync(token).ConfigureAwait(false);
            if (hutaoAnnouncements is null)
            {
                MarkHomePendingForRetry();
                return false;
            }

            await taskContext.SwitchToMainThreadAsync();
            HutaoAnnouncements = hutaoAnnouncements;
            DeferContentLoader?.Load("HutaoAnnouncementControl");
            return true;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) when (IsNetworkRelatedException(ex))
        {
            MarkHomePendingForRetry();
            return false;
        }

        return false;
    }

    [SuppressMessage("", "SH003")]
    private async ValueTask<bool> InitializeMiyoliveCodeAsync(CancellationToken token)
    {
        using (IServiceScope scope = serviceProvider.CreateScope())
        {
            IUserService userService = scope.ServiceProvider.GetRequiredService<IUserService>();
            if (await userService.GetCurrentUserAndUidAsync().ConfigureAwait(false) is not { IsOversea: false } userAndUid)
            {
                return true;
            }

            IHomeClient homeClient = scope.ServiceProvider
                .GetRequiredService<IOverseaSupportFactory<IHomeClient>>()
                .CreateFor(userAndUid);

            try
            {
                Response<NewHomeNewInfo> newHomeInfoResponse = await homeClient.GetNewHomeInfoAsync(2, token).ConfigureAwait(false);

                if (!ResponseValidator.TryValidateWithoutUINotification(newHomeInfoResponse, out NewHomeNewInfo? newHomeInfo))
                {
                    return true;
                }

                Uri url;
                if (newHomeInfo.Lives is [{ Data.LiveUrl: { } url1 }, ..])
                {
                    url = url1;
                }
                else if (newHomeInfo.Navigator.SingleOrDefault(nav => nav.Name.EqualsAny(["直播兑换码", "前瞻直播"], StringComparison.OrdinalIgnoreCase)) is { AppPath: { } url2 })
                {
                    url = url2;
                }
                else
                {
                    return true;
                }

                if (ActIdExtractor.Match(url.OriginalString) is not { Success: true, Groups: [_, { Value: { } actId }, ..] })
                {
                    return true;
                }

                IMiyoliveClient miyoliveClient = scope.ServiceProvider
                    .GetRequiredService<IOverseaSupportFactory<IMiyoliveClient>>()
                    .CreateFor(userAndUid);

                Response<CodeListWrapper> codeListResponse = await miyoliveClient.RefreshCodeAsync(actId, token).ConfigureAwait(false);
                if (!ResponseValidator.TryValidateWithoutUINotification(codeListResponse, out CodeListWrapper? wrapper))
                {
                    return true;
                }

                ImmutableArray<CodeWrapper> wrappers = wrapper.CodeList.SelectAsArray(static wrapper => wrapper.WithTitle(wrapper.Title.DecodeHtml()));
                wrappers = [.. wrappers.Where(static wrapper => !string.IsNullOrEmpty(wrapper.Code))];
                if (wrappers.IsEmpty)
                {
                    return true;
                }

                await taskContext.SwitchToMainThreadAsync();
                RedeemCodes = wrappers;
                return true;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex) when (IsNetworkRelatedException(ex))
            {
                MarkHomePendingForRetry();
                return false;
            }
        }

        return false;
    }

    private async ValueTask<bool> InitializeActivityCalendarAsync(CancellationToken token)
    {
        try
        {
            using (IServiceScope scope = serviceProvider.CreateScope())
            {
                IUserService userService = scope.ServiceProvider.GetRequiredService<IUserService>();
                if (await userService.GetCurrentUserAndUidAsync().ConfigureAwait(false) is not { IsOversea: false } userAndUid)
                {
                    return true;
                }

                IGameRecordClient gameRecordClient = scope.ServiceProvider
                    .GetRequiredService<IOverseaSupportFactory<IGameRecordClient>>()
                    .CreateFor(userAndUid);

                Response<ActCalendar> response = await gameRecordClient.GetActCalendarAsync(userAndUid, token).ConfigureAwait(false);

                if (ResponseValidator.TryValidateWithoutUINotification(response, out ActCalendar? calendar))
                {
                    await taskContext.SwitchToMainThreadAsync();
                    ActivityCalendar = calendar;
                    UpdateDisplayedActivityCards();
                    DeferContentLoader?.Load(nameof(UI.Xaml.View.Page.AnnouncementPage.ActivityCalendarGrid));
                }
            }

            return true;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) when (IsNetworkRelatedException(ex))
        {
            MarkHomePendingForRetry();
            return false;
        }

        return false;
    }

    private async ValueTask<bool> RetryHomeAsync(CancellationToken token)
    {
        Task<bool> inGameTask = InitializeInGameAnnouncementAsync(token).AsTask();
        Task<bool> hutaoTask = InitializeHutaoAnnouncementAsync(token).AsTask();
        Task<bool> miHoYoTask = InitializeMiyoliveCodeAsync(token).AsTask();
        Task<bool> calendarTask = InitializeActivityCalendarAsync(token).AsTask();

        bool[] results = await Task.WhenAll(inGameTask, hutaoTask, miHoYoTask, calendarTask).ConfigureAwait(false);
        bool inGameSuccess = results[0];
        bool hutaoSuccess = results[1];
        bool miHoYoSuccess = results[2];
        bool calendarSuccess = results[3];

        bool success = inGameSuccess && hutaoSuccess && miHoYoSuccess && calendarSuccess;
        if (success)
        {
            if (Interlocked.Exchange(ref shouldRefreshDashboardOnSuccess, 0) is not 0)
            {
                RefreshDashboard();
            }

            networkRetryCoordinator.ClearPending("AnnouncementViewModel.LoadHome");
        }

        return success;
    }

    private static bool IsNetworkRelatedException(Exception ex)
    {
        return ex switch
        {
            HttpRequestException httpRequestException => HttpRequestExceptionHandling.HttpRequestExceptionToNetworkError(httpRequestException) is not NetworkError.NULL,
            TimeoutException => true,
            TaskCanceledException => true,
            _ => false,
        };
    }

    private void UpdateGreetingText()
    {
        taskContext.InvokeOnMainThread(UpdateGreetingTextCore);
    }

    private void UpdateGreetingTextCore()
    {
        // TODO avatar birthday override.
        int rand = Random.Shared.Next(0, 1000);

        if (rand is >= 0 and < 6)
        {
            GreetingText = SH.ViewPageHomeGreetingTextEasterEgg;
        }
        else if (rand is >= 6 and < 57)
        {
            // TODO: retrieve days
            // GreetingText = string.Format(SH.ViewPageHomeGreetingTextEpic1, 0);
        }
        else if (rand is >= 57 and < 1000)
        {
            rand = Random.Shared.Next(0, 2);
            if (rand is 0)
            {
                // TODO: impl game launch times
                // GreetingText = string.Format(SH.ViewPageHomeGreetingTextCommon1, 0);
            }
            else if (rand is 1)
            {
                GreetingText = SH.FormatViewPageHomeGreetingTextCommon2(LocalSetting.Get(SettingKeys.LaunchTimes, 0));
            }
        }
    }

    private void RefreshDashboard()
    {
        taskContext.InvokeOnMainThread(InitializeDashboard);
    }

    private void MarkHomePendingForRetry()
    {
        Interlocked.Exchange(ref shouldRefreshDashboardOnSuccess, 1);
        networkRetryCoordinator.MarkPending("AnnouncementViewModel.LoadHome", SH.ViewModelMainNetworkConnectionFailedWillAutoRetry);
    }

    private void UpdateDisplayedActivityCards()
    {
        if (ActivityCalendar is null)
        {
            DisplayedActivityCards = [];
            return;
        }

        bool includeUnscheduledActs = LocalSetting.Get(SettingKeys.HomeAnnouncementActPreviewEnabled, false);

        ImmutableArray<Act> filtered = includeUnscheduledActs
            ? ActivityCalendar.CompositeActs
            : ActivityCalendar.CompositeActs.Where(static act => act.StartTimestamp > 0 && act.EndTimestamp > 0).ToImmutableArray();

        DisplayedActivityCards = filtered;
    }

    private void InitializeDashboard()
    {
        List<CardReference> result = [];

        if (LocalSetting.Get(SettingKeys.IsHomeCardLaunchGamePresented, true))
        {
            result.Add(CardReference.Create(new LaunchGameCard(serviceProvider), SettingKeys.HomeCardLaunchGameOrder));
        }

        if (LocalSetting.Get(SettingKeys.IsHomeCardGachaStatisticsPresented, true))
        {
            result.Add(CardReference.Create(new GachaStatisticsCard(serviceProvider), SettingKeys.HomeCardGachaStatisticsOrder));
        }

        if (LocalSetting.Get(SettingKeys.IsHomeCardAchievementPresented, true))
        {
            result.Add(CardReference.Create(new AchievementCard(serviceProvider), SettingKeys.HomeCardAchievementOrder));
        }

        if (LocalSetting.Get(SettingKeys.IsHomeCardDailyNotePresented, true))
        {
            result.Add(CardReference.Create(new DailyNoteCard(serviceProvider), SettingKeys.HomeCardDailyNoteOrder));
        }

        if (LocalSetting.Get(SettingKeys.IsHomeCardCalendarPresented, true))
        {
            result.Add(CardReference.Create(new CalendarCard(serviceProvider), SettingKeys.HomeCardCalendarOrder));
        }

        if (LocalSetting.Get(SettingKeys.IsHomeCardSignInPresented, true))
        {
            result.Add(CardReference.Create(new SignInCard(serviceProvider), SettingKeys.HomeCardSignInOrder));
        }

        Cards = result.SortBy(r => r.Order);
    }
}