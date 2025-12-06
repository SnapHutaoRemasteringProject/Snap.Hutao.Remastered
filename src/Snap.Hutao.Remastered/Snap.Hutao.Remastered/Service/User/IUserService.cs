// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core.Database;
using Snap.Hutao.Remastered.Web.Hoyolab.Takumi.Binding;
using BindingUser = Snap.Hutao.Remastered.ViewModel.User.User;
using EntityUser = Snap.Hutao.Remastered.Model.Entity.User;

namespace Snap.Hutao.Remastered.Service.User;

internal interface IUserService
{
    ITaskContext TaskContext { get; }

    ValueTask<AdvancedDbCollectionView<BindingUser, EntityUser>> GetUsersAsync();

    ValueTask<ValueResult<UserOptionResultKind, string?>> ProcessInputCookieAsync(InputCookie inputCookie);

    ValueTask<bool> RefreshCookieTokenAsync(EntityUser user);

    ValueTask RemoveUserAsync(BindingUser user);

    ValueTask RefreshProfilePictureAsync(UserGameRole userGameRole);
}