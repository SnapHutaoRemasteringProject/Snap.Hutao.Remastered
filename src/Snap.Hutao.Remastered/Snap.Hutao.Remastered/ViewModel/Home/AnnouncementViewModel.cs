// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.
// Copyright (c) Snap Hutao RP. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.Common;
using CommunityToolkit.Mvvm.ComponentModel;
using Snap.Hutao.Remastered.Core.DataTransfer;
using Snap.Hutao.Remastered.Core.DependencyInjection.Abstraction;
using Snap.Hutao.Remastered.Core.Logging;
using Snap.Hutao.Remastered.Core.Setting;
using Snap.Hutao.Remastered.Service;
using Snap.Hutao.Remastered.Service.Announcement;
using Snap.Hutao.Remastered.Service.Hutao;
using Snap.Hutao.Remastered.Service.Network;
using Snap.Hutao.Remastered.Service.Metadata;
using Snap.Hutao.Remastered.Service.Notification;
using Snap.Hutao.Remastered.Service.User;
using Snap.Hutao.Remastered.UI.Xaml.Control.Card;
using Snap.Hutao.Remastered.UI.Xaml.View.Card;
using Snap.Hutao.Remastered.ViewModel.User;
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
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;

namespace Snap.Hutao.Remastered.ViewModel.Home;

[BindableCustomPropertyProvider]
[Service(ServiceLifetime.Scoped)]
public sealed partial class AnnouncementViewModel : Abstraction.ViewModel, IRecipient<UserAndUidChangedMessage>
{
    private readonly IAnnouncementService announcementService;
    private readonly IServiceProvider serviceProvider;
    private readonly IHutaoAsAService hutaoAsAService;
    private readonly CultureOptions cultureOptions;
    private readonly ITaskContext taskContext;
    private readonly AppOptions appOptions;
    private readonly INetworkRetryCoordinator networkRetryCoordinator;
    private readonly IMetadataService metadataService;

    private const string InGameRetryKey = "AnnouncementViewModel.InGame";
    private const string HutaoRetryKey = "AnnouncementViewModel.HutaoAnnouncements";
    private const string MiyoliveRetryKey = "AnnouncementViewModel.Miyolive";
    private const string CalendarRetryKey = "AnnouncementViewModel.Calendar";
    private IDisposable[]? homeRetryRegistrations;
    private readonly AsyncLoadOperation inGameLoad = new();
    private readonly AsyncLoadOperation hutaoLoad = new();
    private readonly AsyncLoadOperation miyoliveLoad = new();
    private AsyncLoadOperation calendarLoad = new();
    private (Guid User, string Uid)? calendarUser;
    private int calendarUserVersion;

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
    [NotifyPropertyChangedFor(nameof(IsActivityCalendarPresented))]
    public partial ActCalendar ActivityCalendar { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsActivityCalendarPresented))]
    public partial bool IsActivityCalendarLoading { get; set; } = true;

    [ObservableProperty]
    public partial ImmutableArray<Act> DisplayedActivityCards { get; set; } = [];

    public bool IsHomeAnnouncementActivityCalendarPresented
    {
        get => LocalSetting.Get(SettingKeys.IsHomeAnnouncementActivityCalendarPresented, true);
    }

    public bool IsActivityCalendarPresented
    {
        get => LocalSetting.Get(SettingKeys.IsHomeAnnouncementActivityCalendarPresented, true)
            && (ActivityCalendar is not null || IsActivityCalendarLoading);
    }

    public void Receive(UserAndUidChangedMessage message)
    {
        if (XamlApplicationLifetime.Exiting || IsViewUnloaded.Value) return;
        Interlocked.Increment(ref calendarUserVersion);
        if (message.UserAndUid is { IsOversea: false } userAndUid)
        {
            RefreshActivityCalendarOnUserChangedAsync(userAndUid).SafeForget();
        }
        else
        {
            taskContext.InvokeOnMainThread(() =>
            {
                calendarUser = null;
                calendarLoad = new();
                ActivityCalendar = default!;
                DisplayedActivityCards = [];
                IsActivityCalendarLoading = false;
            });
        }
    }

    [GeneratedRegex("act_id=(.*?)&")]
    private static partial Regex ActIdExtractor { get; }

    protected override async ValueTask<bool> LoadOverrideAsync(CancellationToken token)
    {
        homeRetryRegistrations ??=
        [
            networkRetryCoordinator.Register(InGameRetryKey, RetryInGameAsync),
            networkRetryCoordinator.Register(HutaoRetryKey, RetryHutaoAsync),
            networkRetryCoordinator.Register(MiyoliveRetryKey, RetryMiyoliveAsync),
            networkRetryCoordinator.Register(CalendarRetryKey, RetryCalendarAsync),
        ];
        // Navigation must not unlock while the metadata/Git prerequisite is pending.
        await metadataService.InitializeAsync().AsTask().WaitAsync(token).ConfigureAwait(false);
        token.ThrowIfCancellationRequested();
        await taskContext.SwitchToMainThreadAsync();
        token.ThrowIfCancellationRequested();
        RefreshDashboard();
        UpdateGreetingText();

        // Metadata/Git and the dashboard are the navigation prerequisites.
        // Slow optional feeds must not keep the whole NavigationView disabled.
        LoadOptionalContentAsync(token).SafeForget();
        return true;
    }

    private async Task LoadOptionalContentAsync(CancellationToken token)
    {
        await Task.CompletedTask.ConfigureAwait(ConfigureAwaitOptions.ForceYielding);
        token.ThrowIfCancellationRequested();
        RetryInGameAsync(token).AsTask().SafeForget();
        RetryHutaoAsync(token).AsTask().SafeForget();
        RetryMiyoliveAsync(token).AsTask().SafeForget();
        RetryCalendarAsync(token).AsTask().SafeForget();
    }

    protected override void UninitializeOverride()
    {
        if (homeRetryRegistrations is not null)
            foreach (IDisposable registration in homeRetryRegistrations) registration.Dispose();
        homeRetryRegistrations = null;
    }

    [SuppressMessage("", "SH003")]
    private async ValueTask<bool> InitializeInGameAnnouncementAsync(CancellationToken token)
    {
        try
        {
            AnnouncementWrapper? announcementWrapper = await announcementService.GetAnnouncementWrapperAsync(cultureOptions.LanguageCode, appOptions.Region.Value, token).ConfigureAwait(false);
            if (announcementWrapper is null)
            {
                return false;
            }

            token.ThrowIfCancellationRequested();
            await taskContext.SwitchToMainThreadAsync();
            token.ThrowIfCancellationRequested();
            Announcement = announcementWrapper;
            return true;
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (IsNetworkRelatedException(ex))
        {
            return false;
        }
    }

    [SuppressMessage("", "SH003")]
    private async ValueTask<bool> InitializeHutaoAnnouncementAsync(CancellationToken token)
    {
        try
        {
            ObservableCollection<Web.Hutao.HutaoAsAService.Announcement>? hutaoAnnouncements = await hutaoAsAService.GetHutaoAnnouncementCollectionAsync(token).ConfigureAwait(false);
            if (hutaoAnnouncements is null)
            {
                return false;
            }

            token.ThrowIfCancellationRequested();
            await taskContext.SwitchToMainThreadAsync();
            token.ThrowIfCancellationRequested();
            HutaoAnnouncements = hutaoAnnouncements;
            DeferContentLoader?.Load("HutaoAnnouncementControl");
            return true;
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (IsNetworkRelatedException(ex))
        {
            return false;
        }
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

            token.ThrowIfCancellationRequested();
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

                token.ThrowIfCancellationRequested();
                await taskContext.SwitchToMainThreadAsync();
                token.ThrowIfCancellationRequested();
                RedeemCodes = wrappers;
                return true;
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex) when (IsNetworkRelatedException(ex))
            {
                return false;
            }
        }
    }

    private async ValueTask<bool> InitializeActivityCalendarAsync(CancellationToken token)
    {
        int userVersion = Volatile.Read(ref calendarUserVersion);
        if (!LocalSetting.Get(SettingKeys.IsHomeAnnouncementActivityCalendarPresented, true))
        {
            return true;
        }

        try
        {
            using (IServiceScope scope = serviceProvider.CreateScope())
            {
                IUserService userService = scope.ServiceProvider.GetRequiredService<IUserService>();
                if (await userService.GetCurrentUserAndUidAsync().ConfigureAwait(false) is not { IsOversea: false } userAndUid)
                {
                    token.ThrowIfCancellationRequested();
                    await taskContext.SwitchToMainThreadAsync();
                    token.ThrowIfCancellationRequested();
                    IsActivityCalendarLoading = false;
                    return true;
                }

                if (userVersion != Volatile.Read(ref calendarUserVersion)) return true;
                return await RefreshActivityCalendarCoreAsync(userAndUid, token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (IsNetworkRelatedException(ex))
        {
            return false;
        }
    }

    [SuppressMessage("", "SH003")]
    private async ValueTask RefreshActivityCalendarOnUserChangedAsync(UserAndUid userAndUid)
    {
        try
        {
            bool success = await RefreshActivityCalendarCoreAsync(userAndUid, CancellationToken).ConfigureAwait(false);
            if (success) networkRetryCoordinator.ClearPending(CalendarRetryKey);
            else MarkCalendarPendingForRetry();
        }
        catch (OperationCanceledException) when (CancellationToken.IsCancellationRequested || XamlApplicationLifetime.Exiting)
        {
        }
        catch (Exception ex) when (IsNetworkRelatedException(ex))
        {
            MarkCalendarPendingForRetry();
        }
    }

    [SuppressMessage("", "SH003")]
    private async ValueTask<bool> RefreshActivityCalendarCoreAsync(UserAndUid userAndUid, CancellationToken token)
    {
        if (!LocalSetting.Get(SettingKeys.IsHomeAnnouncementActivityCalendarPresented, true))
        {
            return true;
        }

        token.ThrowIfCancellationRequested();
        await taskContext.SwitchToMainThreadAsync();
        token.ThrowIfCancellationRequested();
        (Guid User, string Uid) key = (userAndUid.User.InnerId, userAndUid.Uid.Value);
        if (calendarUser != key)
        {
            calendarUser = key;
            calendarLoad = new();
            ActivityCalendar = default!;
            DisplayedActivityCards = [];
        }

        return await calendarLoad.RunAsync(async requestToken =>
        {
            IsActivityCalendarLoading = true;
            try
            {
                return await FetchActivityCalendarAsync(userAndUid, key, requestToken).ConfigureAwait(false);
            }
            finally
            {
                if (!XamlApplicationLifetime.Exiting && !token.IsCancellationRequested)
                {
                    await taskContext.SwitchToMainThreadAsync();
                    if (calendarUser == key) IsActivityCalendarLoading = false;
                }
            }
        }, token).ConfigureAwait(false);
    }

    private async Task<bool> FetchActivityCalendarAsync(UserAndUid userAndUid, (Guid User, string Uid) key, CancellationToken token)
    {

        IGameRecordClient gameRecordClient = serviceProvider
            .GetRequiredService<IOverseaSupportFactory<IGameRecordClient>>()
            .CreateFor(userAndUid);

        Response<ActCalendar> response = await gameRecordClient.GetActCalendarAsync(userAndUid, token).ConfigureAwait(false);

        if (ResponseValidator.TryValidateWithoutUINotification(response, out ActCalendar? calendar))
        {
            token.ThrowIfCancellationRequested();
            await taskContext.SwitchToMainThreadAsync();
            token.ThrowIfCancellationRequested();
            if (calendarUser != key) return true;
            ActivityCalendar = calendar;
            UpdateDisplayedActivityCards();
            return true;
        }

        return false;
    }

    private ValueTask<bool> RetryInGameAsync(CancellationToken token) =>
        RetryOptionalPartAsync(InGameRetryKey, ct => inGameLoad.RunAsync(loadToken => InitializeInGameAnnouncementAsync(loadToken).AsTask(), ct), token);

    private ValueTask<bool> RetryHutaoAsync(CancellationToken token) =>
        RetryOptionalPartAsync(HutaoRetryKey, ct => hutaoLoad.RunAsync(loadToken => InitializeHutaoAnnouncementAsync(loadToken).AsTask(), ct), token);

    private ValueTask<bool> RetryMiyoliveAsync(CancellationToken token) =>
        RetryOptionalPartAsync(MiyoliveRetryKey, ct => miyoliveLoad.RunAsync(loadToken => InitializeMiyoliveCodeAsync(loadToken).AsTask(), ct), token);

    private ValueTask<bool> RetryCalendarAsync(CancellationToken token) =>
        RetryOptionalPartAsync(CalendarRetryKey, ct => InitializeActivityCalendarAsync(ct).AsTask(), token);

    private async ValueTask<bool> RetryOptionalPartAsync(string key, Func<CancellationToken, Task<bool>> load, CancellationToken token)
    {
        using CancellationTokenSource linked = CancellationTokenSource.CreateLinkedTokenSource(token, CancellationToken, XamlApplicationLifetime.ShutdownToken);
        token = linked.Token;
        try
        {
            await metadataService.InitializeAsync().AsTask().WaitAsync(token).ConfigureAwait(false);
            token.ThrowIfCancellationRequested();
            bool success = await load(token).ConfigureAwait(false);
            token.ThrowIfCancellationRequested();
            if (success) networkRetryCoordinator.ClearPending(key);
            else networkRetryCoordinator.MarkPending(key, SH.ViewModelMainNetworkConnectionFailedWillAutoRetry);
            return success;
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (IsNetworkRelatedException(exception))
        {
            token.ThrowIfCancellationRequested();
            networkRetryCoordinator.MarkPending(key, SH.ViewModelMainNetworkConnectionFailedWillAutoRetry);
            return false;
        }
    }

    private static bool IsNetworkRelatedException(Exception ex)
    {
        return ex switch
        {
            HttpRequestException httpRequestException => HttpRequestExceptionHandling.HttpRequestExceptionToNetworkError(httpRequestException) is not NetworkError.NULL
                || httpRequestException.StatusCode is System.Net.HttpStatusCode.RequestTimeout
                || (httpRequestException.StatusCode.HasValue && (int)httpRequestException.StatusCode.Value >= 500),
            TimeoutException => true,
            IOException => true,
            OperationCanceledException => true,
            _ => false,
        };
    }

    [Command("CopyCodeCommand")]
    private async Task CopyCodeToClipboardAsync(string? code)
    {
        SentrySdk.AddBreadcrumb(BreadcrumbFactory.CreateUI("Copy redeem code to ClipBoard", "AnnouncementPage.Command"));

        if (string.IsNullOrEmpty(code))
        {
            return;
        }

        await serviceProvider.GetRequiredService<IClipboardProvider>().SetTextAsync(code).ConfigureAwait(false);
        serviceProvider.GetRequiredService<IMessenger>().Send(InfoBarMessage.Success(SH.ViewPageAnnouncementRedeemCodeCopySucceed));
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

    private void MarkCalendarPendingForRetry()
    {
        if (XamlApplicationLifetime.Exiting || CancellationToken.IsCancellationRequested) return;
        networkRetryCoordinator.MarkPending(CalendarRetryKey, SH.ViewModelMainNetworkConnectionFailedWillAutoRetry);
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

        if (LocalSetting.Get(SettingKeys.IsHomeCardTravelersDiaryPresented, true))
        {
            result.Add(CardReference.Create(new TravelersDiaryCard(serviceProvider), SettingKeys.HomeCardTravelersDiaryOrder));
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
