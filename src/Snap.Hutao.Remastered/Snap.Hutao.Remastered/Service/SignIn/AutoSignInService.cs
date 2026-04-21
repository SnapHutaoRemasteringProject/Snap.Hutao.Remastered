// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.
// Copyright (c) Snap Hutao RP. All rights reserved.
// Licensed under the MIT license.

using Microsoft.EntityFrameworkCore;
using Snap.Hutao.Remastered.Core.Database;
using Snap.Hutao.Remastered.Core.Setting;
using Snap.Hutao.Remastered.Model.Entity.Database;
using Snap.Hutao.Remastered.ViewModel.User;
using Snap.Hutao.Remastered.Web.Hoyolab;

namespace Snap.Hutao.Remastered.Service.SignIn;

[Service(ServiceLifetime.Singleton, typeof(IAutoSignInService))]
internal sealed partial class AutoSignInService : IAutoSignInService
{
    private static readonly TimeSpan FailureCooldown = TimeSpan.FromMinutes(10);

    private readonly ISignInService signInService;
    private readonly IServiceProvider serviceProvider;

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
        if (!IsEnabled)
        {
            return;
        }

        try
        {
            await TrySignInAsync(userAndUid, token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // ignore
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
        }
    }

    private async ValueTask<bool> TrySignInAsync(UserAndUid userAndUid, CancellationToken token)
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

            bool success = await signInService.ClaimSignInRewardAsync(userAndUid, token).ConfigureAwait(false);
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
