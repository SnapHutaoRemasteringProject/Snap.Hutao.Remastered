// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml.Controls;
using Snap.Hutao.Remastered.Core.Database;
using Snap.Hutao.Remastered.Core.DependencyInjection.Abstraction;
using Snap.Hutao.Remastered.Core.Diagnostics;
using Snap.Hutao.Remastered.Factory.ContentDialog;
using Snap.Hutao.Remastered.Service.Abstraction;
using Snap.Hutao.Remastered.UI.Xaml.Data;
using Snap.Hutao.Remastered.ViewModel.User;
using Snap.Hutao.Remastered.Web.Hoyolab;
using Snap.Hutao.Remastered.Web.Hoyolab.Bbs.User;
using Snap.Hutao.Remastered.Web.Hoyolab.Passport;
using Snap.Hutao.Remastered.Web.Hoyolab.Takumi.Binding;
using Snap.Hutao.Remastered.Web.Response;
using BindingUser = Snap.Hutao.Remastered.ViewModel.User.User;
using EntityUser = Snap.Hutao.Remastered.Model.Entity.User;

namespace Snap.Hutao.Remastered.Service.User;

[Service(ServiceLifetime.Singleton, typeof(IUserService))]
public sealed partial class UserService : IUserService
{
    private readonly IUserInitializationService userInitializationService;
    private readonly IProfilePictureService profilePictureService;
    private readonly IUserCollectionService userCollectionService;
    private readonly IContentDialogFactory contentDialogFactory;
    private readonly IServiceProvider serviceProvider;
    private readonly IUserRepository userRepository;

    [GeneratedConstructor]
    public partial UserService(IServiceProvider serviceProvider);

    public partial ITaskContext TaskContext { get; }

    public ValueTask RemoveUserAsync(BindingUser user)
    {
        return userCollectionService.RemoveUserAsync(user);
    }

    public ValueTask<AdvancedDbCollectionView<BindingUser, EntityUser>> GetUsersAsync()
    {
        return userCollectionService.GetUsersAsync();
    }

    public ValueTask<AdvancedDbCollectionView<BindingUser, EntityUser>> GetUsersAsync(CancellationToken token) => userCollectionService.GetUsersAsync(token);

    public async ValueTask<ValueResult<UserOptionResultKind, string?>> ProcessInputCookieAsync(InputCookie inputCookie)
    {
        ContentDialog dialog = await contentDialogFactory
            .CreateForIndeterminateProgressAsync(SH.ServiceUserProcessInputCookieDialogTitle)
            .ConfigureAwait(false);

        using (await contentDialogFactory.BlockAsync(dialog).ConfigureAwait(false))
        {
            await TaskContext.SwitchToBackgroundAsync();
            (Cookie cookie, bool isOversea, string? deviceFp) = inputCookie;

            string? mid = cookie.GetAccountMid();

            if (string.IsNullOrEmpty(mid))
            {
                return new(UserOptionResultKind.CookieInvalid, SH.ServiceUserProcessCookieNoMid);
            }

            if (await this.GetUserByMidAsync(mid).ConfigureAwait(false) is not { } user)
            {
                return await userCollectionService.TryCreateAndAddUserFromInputCookieAsync(inputCookie).ConfigureAwait(false);
            }

            if (user.IsOversea != isOversea || !cookie.TryGetSToken(out Cookie? sToken)
                || !string.Equals(user.Entity.Aid, sToken.GetValueOrDefault(Cookie.STUID), StringComparison.Ordinal))
            {
                return new(UserOptionResultKind.CookieInvalid, SH.ServiceUserProcessCookieNoSToken);
            }

            // Re-adding an account updates the same instance displayed by UserView.
            await TaskContext.SwitchToMainThreadAsync();
            (Cookie?, Cookie?, Cookie?, DateTimeOffset) previousCredentials = (user.SToken, user.LToken, user.CookieToken, user.Entity.CookieTokenLastUpdateTime);
            (UserInfo?, IAdvancedCollectionView<UserGameRole>, string?, DateTimeOffset) previousProfile = (user.UserInfo, user.UserGameRoles, user.Entity.Fingerprint, user.Entity.FingerprintLastUpdateTime);
            (bool, bool, bool) previousInitialization = (user.IsInitialized, user.NeedDbUpdateAfterResume, user.IsCookieInvalid);
            OptimisticVersionToken credentialVersion = default;
            bool credentialsReplaced = false;
            bool saved = false;
            try
            {
                user.IsInitialized = false;
                // A freshly supplied cookie deserves a new attempt even if the previous one was rejected.
                user.IsCookieInvalid = false;
                user.SToken = sToken;
                user.LToken = cookie.TryGetLToken(isOversea, out Cookie? lToken) ? lToken : (isOversea ? user.LToken : null);
                user.CookieToken = cookie.TryGetCookieToken(isOversea, out Cookie? cookieToken) ? cookieToken : (isOversea ? user.CookieToken : null);
                if (!isOversea)
                {
                    user.Entity.CookieTokenLastUpdateTime = user.CookieToken is null ? default : DateTimeOffset.UtcNow;
                }

                user.TryUpdateFingerprint(deviceFp);

                // Requests that are still running with the previous cookie must not apply
                // their responses now that this cookie replaces it.
                credentialVersion = user.MarkCredentialsReplaced();
                credentialsReplaced = true;

                await TaskContext.SwitchToBackgroundAsync();
                await userInitializationService.ResumeUserAsync(user).ConfigureAwait(false);
                await TaskContext.SwitchToBackgroundAsync();
                if (!user.IsInitialized)
                {
                    return new(UserOptionResultKind.CookieInvalid, SH.ServiceUserProcessCookieRequestUserInfoFailed);
                }

                userRepository.UpdateUser(user.Entity);
                user.NeedDbUpdateAfterResume = false;
                saved = true;
                return new(UserOptionResultKind.CookieUpdated, mid);
            }
            finally
            {
                if (!saved)
                {
                    await TaskContext.SwitchToMainThreadAsync();

                    // A newer cookie may have replaced this one while the request was running;
                    // restoring the previous credentials would discard the cookie the user
                    // supplied in the meantime.
                    if (!credentialsReplaced || credentialVersion.IsCurrent)
                    {
                        (user.SToken, user.LToken, user.CookieToken, user.Entity.CookieTokenLastUpdateTime) = previousCredentials;
                        (user.UserInfo, user.UserGameRoles, user.Entity.Fingerprint, user.Entity.FingerprintLastUpdateTime) = previousProfile;
                        (user.IsInitialized, user.NeedDbUpdateAfterResume, user.IsCookieInvalid) = previousInitialization;

                        if (credentialsReplaced)
                        {
                            // Requests started with the failed cookie must not apply either.
                            user.MarkCredentialsReplaced();
                        }
                    }
                }
            }
        }
    }

    public async ValueTask<bool> RefreshCookieTokenAsync(EntityUser user)
    {
        string? aid = user.Aid;
        string? mid = user.Mid;
        Cookie? sToken = user.SToken;
        if (string.IsNullOrEmpty(aid) || string.IsNullOrEmpty(mid) || sToken is null)
        {
            return false;
        }

        using SentrySpanScope span = SentrySpanScope.StartOperation("user.refresh_cookie", "user");
        span.SetData("user.is_oversea", user.IsOversea);

        UidCookieToken? uidCookieToken;
        using (IServiceScope scope = serviceProvider.CreateScope())
        {
            IPassportClient passportClient = scope.ServiceProvider
                .GetRequiredService<IOverseaSupportFactory<IPassportClient>>()
                .Create(user.IsOversea);

            Response<UidCookieToken> cookieTokenResponse = await passportClient
                .GetCookieAccountInfoBySTokenAsync(user)
                .ConfigureAwait(false);

            if (!ResponseValidator.TryValidate(cookieTokenResponse, scope.ServiceProvider, out uidCookieToken))
            {
                return false;
            }
        }

        if (!ReferenceEquals(sToken, user.SToken) || !userRepository.WithDatabase(context => AppDbQueries.UserContainsMid(context, mid)))
        {
            return false;
        }

        user.CookieToken = Cookie.FromCookieToken(aid, mid, uidCookieToken.CookieToken, user.IsOversea);
        user.CookieTokenLastUpdateTime = DateTimeOffset.UtcNow;
        userRepository.UpdateUser(user);
        return true;
    }

    public async ValueTask RefreshProfilePictureAsync(UserGameRole userGameRole)
    {
        await profilePictureService.RefreshUserGameRoleAsync(userGameRole).ConfigureAwait(false);
    }

    public ValueTask<bool> RetryResumeUninitializedUsersAsync(CancellationToken token = default)
    {
        return userCollectionService.RetryResumeUninitializedUsersAsync(token);
    }
}
