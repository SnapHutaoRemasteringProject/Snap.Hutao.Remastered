// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.ViewModel.RoleCombat;
using Snap.Hutao.Remastered.ViewModel.User;
using System.Collections.ObjectModel;

namespace Snap.Hutao.Remastered.Service.RoleCombat;

internal interface IRoleCombatService
{
    ValueTask<ObservableCollection<RoleCombatView>> GetRoleCombatViewCollectionAsync(RoleCombatMetadataContext context, UserAndUid userAndUid);

    ValueTask RefreshRoleCombatAsync(RoleCombatMetadataContext context, UserAndUid userAndUid);
}