// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core.DependencyInjection.Abstraction;
using Snap.Hutao.Remastered.Core.ExceptionService;
using Snap.Hutao.Remastered.Model.Entity.Primitive;
using Snap.Hutao.Remastered.Service.Game.Account;
using Snap.Hutao.Remastered.Service.Game.Launching.Context;
using Snap.Hutao.Remastered.Service.Game.Scheme;
using Snap.Hutao.Remastered.Service.User;
using Snap.Hutao.Remastered.Web.Hoyolab.Passport;
using Snap.Hutao.Remastered.Web.Hoyolab.Takumi.Binding;
using Snap.Hutao.Remastered.Web.Response;

namespace Snap.Hutao.Remastered.Service.Game.Launching.Handler;

internal sealed class LaunchExecutionGameIdentityHandler : AbstractLaunchExecutionHandler
{
    public override async ValueTask BeforeAsync(BeforeLaunchExecutionContext context)
    {
        if (context.LaunchOptions.UsingHoyolabAccount.Value)
        {
            await HandleHoyolabAccountAsync(context).ConfigureAwait(false);
        }
        else if (context.Identity.GameAccount is { } account && !RegistryInterop.Set(account))
        {
            HutaoException.Throw(SH.ViewModelLaunchGameSwitchGameAccountFail);
        }
    }

    public override async ValueTask AfterAsync(AfterLaunchExecutionContext context)
    {
        LaunchStatusOptions options = context.ServiceProvider.GetRequiredService<LaunchStatusOptions>();
        await context.TaskContext.SwitchToMainThreadAsync();
        options.UserGameRole = default;
    }

    private static async ValueTask HandleHoyolabAccountAsync(BeforeLaunchExecutionContext context)
    {
        if (context.TargetScheme.SchemeType is SchemeType.ChineseBilibili)
        {
            return;
        }

        if (context.Identity.UserAndUid is not { } userAndUid)
        {
            return;
        }

        if (userAndUid.IsOversea ^ context.TargetScheme.IsOversea)
        {
            HutaoException.NotSupported(SH.ViewModelLaunchGameAccountAndServerNotMatch);
        }

        using (IServiceScope scope = context.ServiceProvider.CreateScope())
        {
            IHoyoPlayPassportClient client = scope.ServiceProvider
                .GetRequiredService<IOverseaSupportFactory<IHoyoPlayPassportClient>>()
                .CreateFor(userAndUid);
            Response<AuthTicketWrapper> resp = await client
                .CreateAuthTicketAsync(userAndUid.User)
                .ConfigureAwait(false);

            if (ResponseValidator.TryValidate(resp, scope.ServiceProvider, out AuthTicketWrapper? wrapper))
            {
                IUserService userService = scope.ServiceProvider.GetRequiredService<IUserService>();
                UserGameRole? userGameRole = await userService.GetUserGameRoleByUidAsync(userAndUid.Uid.Value).ConfigureAwait(false);

                await context.TaskContext.SwitchToMainThreadAsync();
                scope.ServiceProvider.GetRequiredService<LaunchStatusOptions>().UserGameRole = userGameRole;

                context.SetOption(LaunchExecutionOptionsKey.LoginAuthTicket, wrapper.Ticket);
                return;
            }
        }

        HutaoException.NotSupported(SH.ViewModelLaunchGameCreateAuthTicketFailed);
    }
}