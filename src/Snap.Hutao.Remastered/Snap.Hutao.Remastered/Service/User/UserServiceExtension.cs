// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core.Database;
using Snap.Hutao.Remastered.ViewModel.User;
using Snap.Hutao.Remastered.Web.Hoyolab.Takumi.Binding;
using BindingUser = Snap.Hutao.Remastered.ViewModel.User.User;
using EntityUser = Snap.Hutao.Remastered.Model.Entity.User;

namespace Snap.Hutao.Remastered.Service.User;

// For performance reason, extension method should avoid using LINQ
public static class UserServiceExtension
{
    extension(IUserService userService)
    {
        public ValueTask<bool> RefreshCookieTokenAsync(BindingUser user)
        {
            return userService.RefreshCookieTokenAsync(user.Entity);
        }

        public async ValueTask<UserGameRole?> GetUserGameRoleByUidAsync(string uid)
        {
            AdvancedDbCollectionView<BindingUser, EntityUser> users = await userService.GetUsersAsync().ConfigureAwait(false);
            foreach (BindingUser bindingUser in users.Source)
            {
                foreach (UserGameRole role in bindingUser.UserGameRoles.Source)
                {
                    if (role.GameUid == uid)
                    {
                        return role;
                    }
                }
            }

            return default;
        }

        public async ValueTask<BindingUser?> GetCurrentUserAsync()
        {
            AdvancedDbCollectionView<BindingUser, EntityUser> users = await userService.GetUsersAsync().ConfigureAwait(false);
            return ResolveCurrentUser(users);
        }

        public async ValueTask<UserGameRole?> GetCurrentUserGameRoleAsync()
        {
            AdvancedDbCollectionView<BindingUser, EntityUser> users = await userService.GetUsersAsync().ConfigureAwait(false);
            return ResolveCurrentUser(users)?.UserGameRoles.CurrentItem;
        }

        public async ValueTask<string?> GetCurrentUidAsync()
        {
            AdvancedDbCollectionView<BindingUser, EntityUser> users = await userService.GetUsersAsync().ConfigureAwait(false);
            return ResolveCurrentUser(users)?.UserGameRoles.CurrentItem?.GameUid;
        }

        public async ValueTask<UserAndUid?> GetCurrentUserAndUidAsync()
        {
            AdvancedDbCollectionView<BindingUser, EntityUser> users = await userService.GetUsersAsync().ConfigureAwait(false);
            UserAndUid.TryFromUser(ResolveCurrentUser(users), out UserAndUid? userAndUid);
            return userAndUid;
        }

        public async ValueTask<UserAndUid?> GetUserByUidAsync(string uid)
        {
            AdvancedDbCollectionView<BindingUser, EntityUser> users = await userService.GetUsersAsync().ConfigureAwait(false);
            BindingUser? user = users.Source.SingleOrDefault(u => u.UserGameRoles.Source.Any(r => r.GameUid == uid));

            if (user is null)
            {
                return null;
            }

            UserAndUid.TryFromUser(user, out UserAndUid? userAndUid);

            return userAndUid;
        }

        public async ValueTask<bool> SetCurrentUserByUidAsync(string uid)
        {
            AdvancedDbCollectionView<BindingUser, EntityUser> users = await userService.GetUsersAsync().ConfigureAwait(false);
            BindingUser? user = users.Source.SingleOrDefault(u => u.UserGameRoles.Source.Any(r => r.GameUid == uid));

            if (user is null)
            {
                return false;
            }

            await userService.TaskContext.SwitchToMainThreadAsync();
            users.MoveCurrentTo(user);

            return true;
        }

        public async ValueTask<BindingUser?> GetUserByMidAsync(string mid)
        {
            AdvancedDbCollectionView<BindingUser, EntityUser> users = await userService.GetUsersAsync().ConfigureAwait(false);
            foreach (BindingUser user in users.Source)
            {
                if (user.Entity.Mid == mid)
                {
                    return user;
                }
            }

            return default;
        }

        private static BindingUser? ResolveCurrentUser(AdvancedDbCollectionView<BindingUser, EntityUser> users)
        {
            if (users.CurrentItem is { } currentUser)
            {
                return currentUser;
            }

            BindingUser? selectedUser = null;
            int count = 0;
            foreach (BindingUser user in users.Source)
            {
                count++;
                if (user.IsSelected)
                {
                    selectedUser = user;
                    break;
                }
            }

            return selectedUser ?? (count is 1 ? users.Source.FirstOrDefault() : null);
        }
    }
}