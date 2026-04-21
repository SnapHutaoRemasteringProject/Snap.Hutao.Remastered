// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.
// Copyright (c) Snap Hutao RP. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core.Database;
using Snap.Hutao.Remastered.ViewModel.User;
using Snap.Hutao.Remastered.Web.Hoyolab.Takumi.Binding;
using System.Collections.Immutable;
using BindingUser = Snap.Hutao.Remastered.ViewModel.User.User;
using EntityUser = Snap.Hutao.Remastered.Model.Entity.User;

namespace Snap.Hutao.Remastered.Service.User;

[Service(ServiceLifetime.Singleton, typeof(IUserCollectionService))]
public sealed partial class UserCollectionService : IUserCollectionService, IDisposable
{
    private readonly IUserInitializationService userInitializationService;
    private readonly IServiceProvider serviceProvider;
    private readonly IUserRepository userRepository;
    private readonly ITaskContext taskContext;
    private readonly IMessenger messenger;

    private readonly AsyncLock collectionLocker = new();

    private AdvancedDbCollectionView<BindingUser, EntityUser>? users;

    [GeneratedConstructor]
    public partial UserCollectionService(IServiceProvider serviceProvider);

    public async ValueTask<AdvancedDbCollectionView<BindingUser, EntityUser>> GetUsersAsync()
    {
        // Force run in background thread, otherwise will cause re-entrance
        await Task.CompletedTask.ConfigureAwait(ConfigureAwaitOptions.ForceYielding);
        using (await collectionLocker.LockAsync().ConfigureAwait(false))
        {
            if (users is null)
            {
                ImmutableArray<EntityUser> entityUsers = userRepository.GetUserImmutableArray();
                List<BindingUser> bindingUsers = new(entityUsers.Length);
                foreach (EntityUser entity in entityUsers)
                {
                    BindingUser user = await userInitializationService.ResumeUserAsync(entity).ConfigureAwait(false);
                    if (user.NeedDbUpdateAfterResume)
                    {
                        userRepository.UpdateUser(user.Entity);
                        user.NeedDbUpdateAfterResume = false;
                    }

                    bindingUsers.Add(user);
                }

                users = bindingUsers.ToAdvancedDbCollectionViewWrappedObservableReorderableDbCollection<BindingUser, EntityUser>(serviceProvider);

                // Since this service is singleton, we can safely subscribe to the event
                users.CurrentChanged += OnCurrentUserChanged;

                await taskContext.SwitchToMainThreadAsync();
                users.MoveCurrentTo(users.Source.SelectedOrFirstOrDefault());
            }
            else if (users.CurrentItem is null)
            {
                await taskContext.SwitchToMainThreadAsync();
                users.MoveCurrentTo(users.Source.SelectedOrFirstOrDefault());
            }

            if (users.CurrentItem is null && TryGetSelectedOrOnlyUser(users.Source) is { } currentUser)
            {
                await taskContext.SwitchToMainThreadAsync();
                users.MoveCurrentTo(currentUser);
            }

            return users;
        }
    }

    public async ValueTask RemoveUserAsync(BindingUser user)
    {
        ArgumentNullException.ThrowIfNull(users);

        // Sync database
        await taskContext.SwitchToBackgroundAsync();
        userRepository.DeleteUserById(user.Entity.InnerId);

        // Sync cache
        await taskContext.SwitchToMainThreadAsync();
        users.Remove(user);

        messenger.Send(new UserRemovedMessage(user));
    }

    public async ValueTask<ValueResult<UserOptionResultKind, string?>> TryCreateAndAddUserFromInputCookieAsync(InputCookie inputCookie)
    {
        await taskContext.SwitchToBackgroundAsync();
        BindingUser? newUser = await userInitializationService.CreateUserFromInputCookieOrDefaultAsync(inputCookie).ConfigureAwait(false);

        if (newUser is null)
        {
            return new(UserOptionResultKind.CookieInvalid, SH.ServiceUserProcessCookieRequestUserInfoFailed);
        }

        if (newUser.UserGameRoles.Count is 0)
        {
            return new(UserOptionResultKind.GameRoleNotFound, SH.ServiceUserUserInfoContainsNoGameRole);
        }

        await GetUsersAsync().ConfigureAwait(false);
        ArgumentNullException.ThrowIfNull(users);

        // Sync cache
        await taskContext.SwitchToMainThreadAsync();
        users.Add(newUser); // Database synced in the collection

        ArgumentNullException.ThrowIfNull(newUser.UserInfo);
        return new(UserOptionResultKind.Added, newUser.UserInfo.Uid);
    }

    public async ValueTask<bool> RetryResumeUninitializedUsersAsync(CancellationToken token = default)
    {
        AdvancedDbCollectionView<BindingUser, EntityUser> users = await GetUsersAsync().ConfigureAwait(false);

        bool recovered = false;
        foreach (BindingUser user in users.Source)
        {
            if (user.IsInitialized)
            {
                continue;
            }

            await userInitializationService.ResumeUserAsync(user, token).ConfigureAwait(false);
            if (user.IsInitialized)
            {
                recovered = true;

                if (user.NeedDbUpdateAfterResume)
                {
                    userRepository.UpdateUser(user.Entity);
                    user.NeedDbUpdateAfterResume = false;
                }
            }
        }

        if (recovered)
        {
            await taskContext.SwitchToMainThreadAsync();
            OnCurrentUserChanged(this, default);
        }

        return recovered;
    }

    public void Dispose()
    {
        if (users is not null)
        {
            users.CurrentChanged -= OnCurrentUserChanged;
        }
    }

    private void OnCurrentUserChanged(object? sender, object? args)
    {
        if (users is null)
        {
            return;
        }

        if (users.CurrentItem is null)
        {
            if (TryGetSelectedOrOnlyUser(users.Source) is { } fallbackUser)
            {
                users.MoveCurrentTo(fallbackUser);
            }

            if (users.CurrentItem is null)
            {
                if (users.Source.Count is 0)
                {
                    messenger.Send(UserAndUidChangedMessage.Empty);
                }

                return;
            }
        }

        EnsureCurrentUserGameRoleSelection(users.CurrentItem);

        messenger.Send(new UserAndUidChangedMessage(users.CurrentItem));
    }

    private void EnsureCurrentUserGameRoleSelection(BindingUser currentUser)
    {
        // Suppress the BindingUser itself to raise the message
        // This is to avoid the message being raised in the
        // BindingUser.OnCurrentUserGameRoleChanged.
        using (currentUser.SuppressCurrentUserGameRoleChangedMessage())
        {
            foreach (UserGameRole role in currentUser.UserGameRoles)
            {
                if (role.GameUid == currentUser.PreferredUid)
                {
                    currentUser.UserGameRoles.MoveCurrentTo(role);
                    break;
                }
            }

            if (currentUser.UserGameRoles.CurrentItem is null)
            {
                if (currentUser.UserGameRoles.Source.SingleOrDefault(role => role.IsChosen) is { } chosenRole)
                {
                    currentUser.UserGameRoles.MoveCurrentTo(chosenRole);
                }
                else
                {
                    currentUser.UserGameRoles.MoveCurrentToFirst();
                }
            }
        }
    }

    private static BindingUser? TryGetSelectedOrOnlyUser(IEnumerable<BindingUser> source)
    {
        BindingUser? selectedUser = null;
        int count = 0;

        foreach (BindingUser user in source)
        {
            count++;

            if (user.IsSelected)
            {
                selectedUser = user;
                break;
            }
        }

        if (selectedUser is not null)
        {
            return selectedUser;
        }

        return count is 1 ? source.First() : null;
    }
}