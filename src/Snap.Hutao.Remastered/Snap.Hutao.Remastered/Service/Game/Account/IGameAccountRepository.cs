// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core.Database;
using Snap.Hutao.Remastered.Model.Entity;
using Snap.Hutao.Remastered.Service.Abstraction;

namespace Snap.Hutao.Remastered.Service.Game.Account;

internal interface IGameAccountRepository : IRepository<GameAccount>
{
    void AddGameAccount(GameAccount gameAccount);

    void RemoveGameAccountById(Guid id);

    ObservableReorderableDbCollection<GameAccount> GetGameAccountCollection();

    void UpdateGameAccount(GameAccount gameAccount);
}