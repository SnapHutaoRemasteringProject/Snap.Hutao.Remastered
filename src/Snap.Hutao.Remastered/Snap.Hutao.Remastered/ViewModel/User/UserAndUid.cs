// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Web.Hoyolab;
using Snap.Hutao.Remastered.Web.Hoyolab.Takumi.Binding;
using EntityUser = Snap.Hutao.Remastered.Model.Entity.User;

namespace Snap.Hutao.Remastered.ViewModel.User;

public sealed class UserAndUid
{
    public UserAndUid(EntityUser user, in PlayerUid role)
    {
        User = user;
        Uid = role;
    }

    public EntityUser User { get; }

    public PlayerUid Uid { get; }

    public bool IsOversea { get => User.IsOversea; }

    public static UserAndUid From(EntityUser user, PlayerUid role)
    {
        return new(user, role);
    }

    public static bool TryFromUser([NotNullWhen(true)] User? user, [NotNullWhen(true)] out UserAndUid? userAndUid)
    {
        if (user is null)
        {
            userAndUid = null;
            return false;
        }

        if (user.UserGameRoles.CurrentItem is { } currentRole)
        {
            userAndUid = new(user.Entity, currentRole);
            return true;
        }

        PlayerUid? selectedRole = default;
        using (user.SuppressCurrentUserGameRoleChangedMessage())
        {
            foreach (UserGameRole candidate in user.UserGameRoles)
            {
                if (candidate.GameUid == user.PreferredUid)
                {
                    selectedRole = candidate;
                    user.UserGameRoles.MoveCurrentTo(candidate);
                    break;
                }
            }

            if (selectedRole is null)
            {
                UserGameRole? chosenRole = user.UserGameRoles.Source.SingleOrDefault(candidate => candidate.IsChosen)
                    ?? user.UserGameRoles.Source.FirstOrDefault();

                if (chosenRole is not null)
                {
                    selectedRole = chosenRole;
                    user.UserGameRoles.MoveCurrentTo(chosenRole);
                }
            }
        }

        if (selectedRole is null)
        {
            userAndUid = null;
            return false;
        }

        userAndUid = new(user.Entity, selectedRole.Value);
        return true;
    }
}