// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core.Database;
using Snap.Hutao.Remastered.Core.Database.Abstraction;
using Snap.Hutao.Remastered.Model;
using Snap.Hutao.Remastered.Model.Entity.Database;
using Snap.Hutao.Remastered.UI.Xaml.Data;
using Snap.Hutao.Remastered.Web.Hoyolab;
using Snap.Hutao.Remastered.Web.Hoyolab.Bbs.User;
using Snap.Hutao.Remastered.Web.Hoyolab.Takumi.Binding;
using EntityUser = Snap.Hutao.Remastered.Model.Entity.User;

namespace Snap.Hutao.Remastered.ViewModel.User;

internal sealed partial class User : IEntityAccess<EntityUser>,
    ISelectable,
    IPropertyValuesProvider
{
    private readonly IServiceProvider serviceProvider;

    private bool isCurrentUserGameRoleChangedMessageSuppressed;

    private User(EntityUser user, IServiceProvider serviceProvider)
    {
        Entity = user;
        this.serviceProvider = serviceProvider;
    }

    public bool IsInitialized { get; set; }

    public UserInfo? UserInfo { get; set; }

#pragma warning disable SA1500
#pragma warning disable SA1513
    public IAdvancedCollectionView<UserGameRole> UserGameRoles
    {
        get;
        set
        {
            AdvancedCollectionViewCurrentChanged.Detach(field, OnCurrentUserGameRoleChanged);
            field = value;
            AdvancedCollectionViewCurrentChanged.Attach(field, OnCurrentUserGameRoleChanged);
        }
    } = default!;
#pragma warning restore SA1513
#pragma warning restore SA1500

    public string? Fingerprint { get => Entity.Fingerprint; }

    public Guid InnerId { get => Entity.InnerId; }

    public bool IsSelected
    {
        get => Entity.IsSelected;
        set => Entity.IsSelected = value;
    }

    public Cookie? CookieToken
    {
        get => Entity.CookieToken;
        set => Entity.CookieToken = value;
    }

    public Cookie? LToken
    {
        get => Entity.LToken;
        set => Entity.LToken = value;
    }

    public Cookie? SToken
    {
        get => Entity.SToken;
        set => Entity.SToken = value;
    }

    public bool IsOversea { get => Entity.IsOversea; }

    public EntityUser Entity { get; }

    public bool NeedDbUpdateAfterResume { get; set; }

    public string? PreferredUid { get => Entity.PreferredUid; private set => Entity.PreferredUid = value; }

    public static User From(EntityUser user, IServiceProvider provider)
    {
        return new(user, provider);
    }

    public IDisposable SuppressCurrentUserGameRoleChangedMessage()
    {
        return new CurrentUserGameRoleChangedSuppression(this);
    }

    private void OnCurrentUserGameRoleChanged(object? sender, object? e)
    {
        if (UserGameRoles.CurrentItem is { } item && PreferredUid != item.GameUid)
        {
            PreferredUid = item.GameUid;
            using (IServiceScope scope = serviceProvider.CreateScope())
            {
                scope.ServiceProvider.GetRequiredService<AppDbContext>().Users.UpdateAndSave(Entity);
            }
        }

        if (!isCurrentUserGameRoleChangedMessageSuppressed && IsSelected)
        {
            serviceProvider.GetRequiredService<IMessenger>().Send(new UserAndUidChangedMessage(this));
        }
    }

    private sealed partial class CurrentUserGameRoleChangedSuppression : IDisposable
    {
        private readonly User reference;

        public CurrentUserGameRoleChangedSuppression(User reference)
        {
            this.reference = reference;
            reference.isCurrentUserGameRoleChangedMessageSuppressed = true;
        }

        public void Dispose()
        {
            reference.isCurrentUserGameRoleChangedMessageSuppressed = false;
        }
    }
}