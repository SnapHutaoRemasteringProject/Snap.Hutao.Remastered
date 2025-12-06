// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core.Database;
using BindingUser = Snap.Hutao.Remastered.ViewModel.User.User;
using EntityUser = Snap.Hutao.Remastered.Model.Entity.User;

namespace Snap.Hutao.Remastered.Service.User;

internal interface IUserCollectionService
{
    ValueTask<AdvancedDbCollectionView<BindingUser, EntityUser>> GetUsersAsync();

    ValueTask RemoveUserAsync(BindingUser user);

    ValueTask<ValueResult<UserOptionResultKind, string?>> TryCreateAndAddUserFromInputCookieAsync(InputCookie inputCookie);
}