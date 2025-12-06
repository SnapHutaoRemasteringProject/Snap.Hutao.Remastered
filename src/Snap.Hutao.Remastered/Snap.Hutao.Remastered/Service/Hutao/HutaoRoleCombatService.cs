// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Web.Hutao.RoleCombat;

namespace Snap.Hutao.Remastered.Service.Hutao;

[Service(ServiceLifetime.Scoped, typeof(IHutaoRoleCombatService))]
internal sealed partial class HutaoRoleCombatService : ObjectCacheService, IHutaoRoleCombatService
{
    [GeneratedConstructor(CallBaseConstructor = true)]
    public partial HutaoRoleCombatService(IServiceProvider serviceProvider);

    public override string TypeName { get; } = nameof(HutaoRoleCombatService);

    public async ValueTask<RoleCombatStatisticsItem> GetRoleCombatStatisticsItemAsync()
    {
        using (IServiceScope scope = ServiceProvider.CreateScope())
        {
            HutaoRoleCombatClient homaClient = scope.ServiceProvider.GetRequiredService<HutaoRoleCombatClient>();
            return await FromCacheOrWebAsync(nameof(RoleCombatStatisticsItem), false, homaClient.GetStatisticsAsync).ConfigureAwait(false);
        }
    }
}