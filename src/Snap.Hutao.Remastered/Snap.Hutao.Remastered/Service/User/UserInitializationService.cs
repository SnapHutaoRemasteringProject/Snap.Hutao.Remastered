// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.
// Copyright (c) Snap Hutao RP. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core.DependencyInjection.Abstraction;
using Snap.Hutao.Remastered.Model.Entity.Extension;
using Snap.Hutao.Remastered.UI.Xaml.Data;
using Snap.Hutao.Remastered.ViewModel.User;
using Snap.Hutao.Remastered.Web.Hoyolab;
using Snap.Hutao.Remastered.Web.Hoyolab.Bbs.User;
using Snap.Hutao.Remastered.Web.Hoyolab.Passport;
using Snap.Hutao.Remastered.Web.Hoyolab.Takumi.Binding;
using Snap.Hutao.Remastered.Web.Response;

namespace Snap.Hutao.Remastered.Service.User;

[Service(ServiceLifetime.Singleton, typeof(IUserInitializationService))]
public sealed partial class UserInitializationService : IUserInitializationService
{
    private readonly IUserFingerprintService userFingerprintService;
    private readonly IProfilePictureService profilePictureService;
    private readonly IServiceProvider serviceProvider;
    private readonly ITaskContext taskContext;
    private readonly IAutoSignInService autoSignInService;

    [GeneratedConstructor]
    public partial UserInitializationService(IServiceProvider serviceProvider);

    public ValueTask<ViewModel.User.User> ResumeUserAsync(Model.Entity.User entity, CancellationToken token = default)
    {
        ViewModel.User.User user = ViewModel.User.User.From(entity, serviceProvider);
        return ResumeUserAsync(user, token);
    }

    public async ValueTask<ViewModel.User.User> ResumeUserAsync(ViewModel.User.User user, CancellationToken token = default)
    {
        if (!await InitializeUserAsync(user, token).ConfigureAwait(false))
        {
            await taskContext.SwitchToMainThreadAsync();
            token.ThrowIfCancellationRequested();
            user.UserInfo = new()
            {
                Uid = SH.ModelBindingUserInitializationFailed,
                Nickname = SH.ModelBindingUserInitializationFailed,
            };

            user.UserGameRoles = new AdvancedCollectionView<UserGameRole>([]);
        }

        return user;
    }

    public async ValueTask<ViewModel.User.User?> CreateUserFromInputCookieOrDefaultAsync(InputCookie inputCookie, CancellationToken token = default)
    {
        // 这里只负责创建实体用户，稍后在用户服务中保存到数据库
        (Cookie cookie, bool isOversea, string? deviceFp) = inputCookie;
        Model.Entity.User entity = Model.Entity.User.From(cookie, isOversea);
        entity.TryUpdateFingerprint(deviceFp);

        if (entity.Aid is not null && entity.Mid is not null)
        {
            ViewModel.User.User user = ViewModel.User.User.From(entity, serviceProvider);
            bool initialized = await InitializeUserAsync(user, token).ConfigureAwait(false);

            return initialized ? user : null;
        }

        return null;
    }

    private static async ValueTask<bool> TryRefreshCredentialsAsync(IServiceProvider serviceProvider, ViewModel.User.User user, CancellationToken token)
    {
        bool isOversea = user.IsOversea;
        Cookie? lToken = user.LToken;
        Cookie? cookieToken = user.CookieToken;
        bool refreshLToken = lToken is null || (!isOversea && !lToken.TryGetLToken(false, out _));
        bool refreshCookieToken = cookieToken is null || (!isOversea && !cookieToken.TryGetCookieToken(false, out _))
            || user.Entity.CookieTokenLastUpdateTime <= DateTimeOffset.UtcNow - TimeSpan.FromDays(1);
        if (!refreshLToken && !refreshCookieToken)
        {
            return true;
        }

        string? accountId = user.Entity.Aid;
        string? mid = user.Entity.Mid;
        if (string.IsNullOrEmpty(accountId) || string.IsNullOrEmpty(mid))
        {
            user.IsCookieInvalid = true;
            return false;
        }

        IPassportClient passportClient = serviceProvider
            .GetRequiredService<IOverseaSupportFactory<IPassportClient>>()
            .Create(isOversea);

        if (refreshLToken)
        {
            Response<LTokenWrapper> response = await passportClient.GetLTokenBySTokenAsync(user.Entity, token).ConfigureAwait(false);
            if (!ResponseValidator.TryValidate(response, serviceProvider, out LTokenWrapper? result))
            {
                user.IsCookieInvalid |= ((KnownReturnCode)response.ReturnCode).IsAuthenticationFailure();
                return false;
            }

            lToken = Cookie.FromLToken(accountId, mid, result.LToken, isOversea);
        }

        DateTimeOffset? refreshedAt = null;
        if (refreshCookieToken)
        {
            Response<UidCookieToken> response = await passportClient.GetCookieAccountInfoBySTokenAsync(user.Entity, token).ConfigureAwait(false);
            if (!ResponseValidator.TryValidate(response, serviceProvider, out UidCookieToken? result))
            {
                user.IsCookieInvalid |= ((KnownReturnCode)response.ReturnCode).IsAuthenticationFailure();
                return false;
            }

            cookieToken = Cookie.FromCookieToken(accountId, mid, result.CookieToken, isOversea);
            refreshedAt = DateTimeOffset.UtcNow;
        }

        token.ThrowIfCancellationRequested();
        user.LToken = lToken;
        user.CookieToken = cookieToken;
        if (refreshedAt is { } timestamp)
        {
            user.Entity.CookieTokenLastUpdateTime = timestamp;
        }

        user.NeedDbUpdateAfterResume = true;
        return true;
    }

    private async ValueTask<bool> TrySetUserUserInfoAsync(IServiceProvider serviceProvider, ViewModel.User.User user, CancellationToken token)
    {
        IUserClient userClient = serviceProvider
            .GetRequiredService<IOverseaSupportFactory<IUserClient>>()
            .Create(user.IsOversea);

        Response<UserFullInfoWrapper> response = await userClient
            .GetUserFullInfoAsync(user.Entity, token)
            .ConfigureAwait(false);

        // An existing account is already bound to UserView. Its PropertyChanged
        // handlers update XAML controls synchronously on the publishing thread.
        await taskContext.SwitchToMainThreadAsync();
        token.ThrowIfCancellationRequested();
        if (ResponseValidator.TryValidate(response, serviceProvider, out UserFullInfoWrapper? wrapper))
        {
            user.UserInfo = wrapper.UserInfo;
            return true;
        }

        user.IsCookieInvalid |= ((KnownReturnCode)response.ReturnCode).IsAuthenticationFailure();

        user.UserInfo = new()
        {
            Uid = SH.ModelBindingUserInitializationFailed,
            Nickname = SH.ModelBindingUserInitializationFailed,
        };

        // Oversea accounts keep going to the game role request for a transient failure,
        // but a rejected cookie has no chance to produce a role list at all.
        return user.IsOversea && !user.IsCookieInvalid;
    }

    private async ValueTask<bool> TrySetUserUserGameRolesAsync(IServiceProvider serviceProvider, ViewModel.User.User user, CancellationToken token)
    {
        BindingClient bindingClient = serviceProvider.GetRequiredService<BindingClient>();

        Response<ListWrapper<UserGameRole>> userGameRolesResponse = await bindingClient
            .GetUserGameRolesOverseaAwareAsync(user.Entity, token)
            .ConfigureAwait(false);

        if (ResponseValidator.TryValidate(userGameRolesResponse, serviceProvider, out ListWrapper<UserGameRole>? wrapper))
        {
            await taskContext.SwitchToMainThreadAsync();
            token.ThrowIfCancellationRequested();
            user.UserGameRoles = wrapper.List.AsAdvancedCollectionView();
            return true;
        }

        user.IsCookieInvalid |= ((KnownReturnCode)userGameRolesResponse.ReturnCode).IsAuthenticationFailure();
        return false;
    }

    private async ValueTask<bool> InitializeUserAsync(ViewModel.User.User user, CancellationToken token = default)
    {
        if (user.IsInitialized)
        {
            // Prevent multiple initialization.
            return true;
        }

        if (user.IsCookieInvalid)
        {
            // The stored cookie was already rejected. Only a newly supplied cookie — which
            // clears this flag — can make another request meaningful.
            return false;
        }

        if (user.SToken is null)
        {
            // Without a login token the account can never be resumed by retrying.
            user.IsCookieInvalid = true;
            return false;
        }

        using (IServiceScope scope = serviceProvider.CreateScope())
        {
            IServiceProvider serviceProvider = scope.ServiceProvider;

            if (!await TryRefreshCredentialsAsync(serviceProvider, user, token).ConfigureAwait(false))
            {
                return false;
            }

            if (!await TrySetUserUserInfoAsync(serviceProvider, user, token).ConfigureAwait(false)
                || !await TrySetUserUserGameRolesAsync(serviceProvider, user, token).ConfigureAwait(false))
            {
                return false;
            }
        }

        await userFingerprintService.TryInitializeAsync(user, token).ConfigureAwait(false);
        await profilePictureService.TryInitializeAsync(user, token).ConfigureAwait(false);

        // Snapshot the UI-bound view before any sign-in await can leave its thread.
        await taskContext.SwitchToMainThreadAsync();
        token.ThrowIfCancellationRequested();
        UserGameRole[] gameRoles = [.. user.UserGameRoles];
        await taskContext.SwitchToBackgroundAsync();
        foreach (UserGameRole gameRole in gameRoles)
        {
            UserAndUid userAndUid = UserAndUid.From(user.Entity, gameRole.GameUid);
            await autoSignInService.InitializeAsync(userAndUid, token).ConfigureAwait(false);
        }

        await taskContext.SwitchToMainThreadAsync();
        token.ThrowIfCancellationRequested();
        user.IsCookieInvalid = false;
        return user.IsInitialized = true;
    }
}
