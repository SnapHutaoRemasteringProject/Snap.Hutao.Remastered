// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using BindingUser = Snap.Hutao.Remastered.ViewModel.User.User;

namespace Snap.Hutao.Remastered.Service.User;

internal sealed class UserRemovedMessage
{
    public UserRemovedMessage(BindingUser removedUser)
    {
        RemovedUser = removedUser;
    }

    public BindingUser RemovedUser { get; }
}