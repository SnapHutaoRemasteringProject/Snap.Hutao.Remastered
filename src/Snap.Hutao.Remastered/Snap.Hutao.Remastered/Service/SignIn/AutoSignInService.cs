// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.
// Copyright (c) Snap Hutao RP. All rights reserved.
// Licensed under the MIT license.

using Microsoft.EntityFrameworkCore;
using Snap.Hutao.Remastered.Core.Database;
using Snap.Hutao.Remastered.Core.Setting;
using Snap.Hutao.Remastered.Model.Entity.Database;
using Snap.Hutao.Remastered.Service.User;
using Snap.Hutao.Remastered.ViewModel.User;
using Snap.Hutao.Remastered.Web.Hoyolab;
using Snap.Hutao.Remastered.Web.Hoyolab.Takumi.Binding;
using BindingUser = Snap.Hutao.Remastered.ViewModel.User.User;

namespace Snap.Hutao.Remastered.Service.SignIn;

[Service(ServiceLifetime.Singleton, typeof(IAutoSignInService))]
public sealed partial class AutoSignInService : IAutoSignInService
{
    private static readonly TimeSpan FailureCooldown = TimeSpan.FromMinutes(10);

    private readonly ISignInService signInService;
    private readonly IServiceProvider serviceProvider;
    private readonly ILogger<AutoSignInService> logger;

    [GeneratedConstructor]
    public partial AutoSignInService(IServiceProvider serviceProvider);

    public bool IsEnabled
    {
        get => LocalSetting.Get(SettingKeys.AutoSignInEnabled, true);
        set => LocalSetting.Set(SettingKeys.AutoSignInEnabled, value);
    }

    public async ValueTask InitializeAsync(UserAndUid userAndUid, CancellationToken token = default)
    {
        if (!IsEnabled)
        {
            return;
        }

        await RunOnceAsync(userAndUid, token).ConfigureAwait(false);
    }

    public async ValueTask RunOnceAsync(UserAndUid userAndUid, CancellationToken token = default)
    {
        // The user is interacting with the app, so the risk verification fallback is expected.
        await RunOnceAsync(userAndUid, true, token).ConfigureAwait(false);
    }

    public async ValueTask RunOnceAsync(UserAndUid userAndUid, bool fallbackToWebView2, CancellationToken token = default)
    {
        if (!IsEnabled)
        {
            return;
        }

        try
        {
            await TrySignInAsync(userAndUid, fallbackToWebView2, token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // ignore
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Auto sign-in failed for uid {Uid}", userAndUid.Uid.Value);
            SentrySdk.CaptureException(ex);
        }
    }

    public async ValueTask RunForAllUsersAsync(CancellationToken token = default)
    {
        if (!IsEnabled)
        {
            return;
        }

        using (IServiceScope scope = serviceProvider.CreateScope())
        {
            IUserService userService = scope.ServiceProvider.GetRequiredService<IUserService>();
            AdvancedDbCollectionView<BindingUser, Model.Entity.User> users = await userService.GetUsersAsync().ConfigureAwait(false);

            // Snapshot because the collection can be modified by the user while the job is running.
            List<(BindingUser User, List<UserGameRole> GameRoles)> snapshot = new(users.Source.Count);
            foreach (BindingUser user in users.Source)
            {
                snapshot.Add((user, user.UserGameRoles.Source.ToList()));
            }

            foreach ((BindingUser user, List<UserGameRole> gameRoles) in snapshot)
            {
                if (token.IsCancellationRequested)
                {
                    return;
                }

                // Cookie token is refreshed while resuming a user, do the same for the accounts
                // which are not the current one and may hold an expired cookie token.
                await userService.RefreshCookieTokenAsync(user).ConfigureAwait(false);

                // Sign in every game role of the account, not only the current one.
                foreach (UserGameRole gameRole in gameRoles)
                {
                    UserAndUid userAndUid = UserAndUid.From(user.Entity, gameRole);

                    // This runs without user interaction, opening a WebView2 window for an
                    // unsatisfied risk verification of any account would be disruptive.
                    await RunOnceAsync(userAndUid, false, token).ConfigureAwait(false);
                }
            }
        }
    }

    private async ValueTask<bool> TrySignInAsync(UserAndUid userAndUid, bool fallbackToWebView2, CancellationToken token)
    {
        string completedDayKeySetting = GetCompletedDayKeySetting(userAndUid);
        string lastFailureTicksKey = GetLastFailureTicksKey(userAndUid);
        string lastRegionSettingKey = GetLastRegionSettingKey(userAndUid);
        string serverDayKey = GetServerDayKey(userAndUid.Uid);

        using (IServiceScope scope = serviceProvider.CreateScope())
        {
            AppDbContext appDbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            string currentRegion = userAndUid.Uid.Region.Value;
            string? lastRegion = GetSettingValue(appDbContext, lastRegionSettingKey);
            if (!string.Equals(lastRegion, currentRegion, StringComparison.Ordinal))
            {
                SetSettingValue(appDbContext, completedDayKeySetting, string.Empty);
                SetSettingValue(appDbContext, lastFailureTicksKey, 0L.ToString());
                SetSettingValue(appDbContext, lastRegionSettingKey, currentRegion);
            }

            if (GetSettingValue(appDbContext, completedDayKeySetting) == serverDayKey)
            {
                return false;
            }

            if (long.TryParse(GetSettingValue(appDbContext, lastFailureTicksKey), out long lastFailureTicks) && lastFailureTicks != 0)
            {
                DateTimeOffset lastFailure = new(lastFailureTicks, TimeSpan.Zero);
                if (DateTimeOffset.UtcNow - lastFailure < FailureCooldown)
                {
                    return false;
                }
            }

            bool success = await signInService.ClaimSignInRewardAsync(userAndUid, fallbackToWebView2, token).ConfigureAwait(false);
            if (success)
            {
                SetSettingValue(appDbContext, completedDayKeySetting, serverDayKey);
                SetSettingValue(appDbContext, lastFailureTicksKey, 0L.ToString());
                SetSettingValue(appDbContext, lastRegionSettingKey, currentRegion);
            }
            else
            {
                SetSettingValue(appDbContext, lastFailureTicksKey, DateTimeOffset.UtcNow.Ticks.ToString());
                SetSettingValue(appDbContext, lastRegionSettingKey, currentRegion);
            }

            return success;
        }
    }

    private static string? GetSettingValue(AppDbContext appDbContext, string key)
    {
        return appDbContext.Settings.AsNoTracking().Where(e => e.Key == key).Select(e => e.Value).SingleOrDefault();
    }

    private static void SetSettingValue(AppDbContext appDbContext, string key, string value)
    {
        appDbContext.Settings.Where(e => e.Key == key).ExecuteDelete();
        appDbContext.Settings.AddAndSave(new(key, value));
    }

    private static string GetCompletedDayKeySetting(UserAndUid userAndUid)
    {
        return $"{SettingKeys.AutoSignInEnabled}::CompletedDay::{userAndUid.User.InnerId:N}::{userAndUid.Uid.Value}";
    }

    private static string GetLastFailureTicksKey(UserAndUid userAndUid)
    {
        return $"{SettingKeys.AutoSignInEnabled}::LastFailureTicks::{userAndUid.User.InnerId:N}::{userAndUid.Uid.Value}";
    }

    private static string GetLastRegionSettingKey(UserAndUid userAndUid)
    {
        return $"{SettingKeys.AutoSignInEnabled}::LastRegion::{userAndUid.User.InnerId:N}::{userAndUid.Uid.Value}";
    }

    private static string GetServerDayKey(PlayerUid uid)
    {
        TimeSpan offset = PlayerUid.GetRegionTimeZoneUtcOffsetForRegion(uid.Region);
        DateTimeOffset serverNow = DateTimeOffset.UtcNow.ToOffset(offset);
        return $"{uid.Region.Value}:{serverNow:yyyy-MM-dd}";
    }
}
