// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Service.Hutao;
using Snap.Hutao.Remastered.Web.Hoyolab.Hk4e.Event.GachaInfo;
using Snap.Hutao.Remastered.Web.Hutao.GachaLog;
using Snap.Hutao.Remastered.Web.Hutao.Response;

namespace Snap.Hutao.Remastered.Service.GachaLog.Factory;

public readonly struct TypedWishSummaryBuilderContext
{
    public readonly IServiceProvider ServiceProvider;
    public readonly ITaskContext TaskContext;
    public readonly string Name;
    public readonly int GuaranteeOrangeThreshold;
    public readonly int GuaranteePurpleThreshold;
        public readonly int GuaranteeBlueThreshold;
    public readonly Func<GachaType, bool> TypeEvaluator;
    public readonly GachaDistributionType DistributionType;
    public readonly bool SummarizePurple;
    public readonly bool SummarizeBlue;

    private static readonly Func<GachaType, bool> IsStandardWish = type => type is GachaType.Standard;
    private static readonly Func<GachaType, bool> IsAvatarEventWish = type => type is GachaType.ActivityAvatar or GachaType.SpecialActivityAvatar;
    private static readonly Func<GachaType, bool> IsWeaponEventWish = type => type is GachaType.ActivityWeapon;
    private static readonly Func<GachaType, bool> IsChronicledWish = type => type is GachaType.ActivityCity;
    private static readonly Func<GachaType, bool> IsBeyondStandardWish = type => type is GachaType.UGCStandard;
    private static readonly Func<GachaType, bool> IsBeyondEventWish = type => type is GachaType.UGCAvatarEventWish or GachaType.UGCActivityAvatarMaleOne or GachaType.UGCActivityAvatarMaleTwo or GachaType.UGCActivityAvatarFemaleOne or GachaType.UGCActivityAvatarFemaleTwo;

    public TypedWishSummaryBuilderContext(
        IServiceProvider serviceProvider,
        string name,
        int guaranteeOrangeThreshold,
        int guaranteePurpleThreshold,
        int guaranteeBlueThreshold,
        Func<GachaType, bool> typeEvaluator,
        GachaDistributionType distributionType,
        bool summarizePurple = false,
        bool summarizeBlue = false)
    {
        ServiceProvider = serviceProvider;
        TaskContext = serviceProvider.GetRequiredService<ITaskContext>();
        Name = name;
        GuaranteeOrangeThreshold = guaranteeOrangeThreshold;
        GuaranteePurpleThreshold = guaranteePurpleThreshold;
        GuaranteeBlueThreshold = guaranteeBlueThreshold;
        TypeEvaluator = typeEvaluator;
        DistributionType = distributionType;
        SummarizePurple = summarizePurple;
        SummarizeBlue = summarizeBlue;
    }

    public static TypedWishSummaryBuilderContext StandardWish(GachaStatisticsFactoryContext context)
    {
        return new(context.ServiceProvider, SH.ServiceGachaLogFactoryPermanentWishName, 90, 10, 0, IsStandardWish, GachaDistributionType.Standard);
    }

    public static TypedWishSummaryBuilderContext AvatarEventWish(GachaStatisticsFactoryContext context)
    {
        return new(context.ServiceProvider, SH.ServiceGachaLogFactoryAvatarWishName, 90, 10, 0, IsAvatarEventWish, GachaDistributionType.AvatarEvent);
    }

    public static TypedWishSummaryBuilderContext WeaponEventWish(GachaStatisticsFactoryContext context)
    {
        return new(context.ServiceProvider, SH.ServiceGachaLogFactoryWeaponWishName, 80, 10, 0, IsWeaponEventWish, GachaDistributionType.WeaponEvent);
    }

    public static TypedWishSummaryBuilderContext ChronicledWish(GachaStatisticsFactoryContext context)
    {
        return new(context.ServiceProvider, SH.ServiceGachaLogFactoryChronicledWishName, 90, 10, 0, IsChronicledWish, GachaDistributionType.Chronicled);
    }

    public static TypedWishSummaryBuilderContext BeyondStandardWish(GachaStatisticsFactoryContext context)
    {
        return new(context.ServiceProvider, SH.ServiceGachaLogFactoryBeyondPermanentWishName, int.MaxValue, 90, 10, IsBeyondStandardWish, GachaDistributionType.BeyondStandard, true, true);
    }

    public static TypedWishSummaryBuilderContext BeyondEventWish(GachaStatisticsFactoryContext context)
    {
        return new(context.ServiceProvider, SH.ServiceGachaLogFactoryBeyondEventWishName, 90, 10, 0, IsBeyondEventWish, GachaDistributionType.BeyondEvent, true);
    }

    public TypedWishSummaryBuilder CreateBuilder()
    {
        return new(this);
    }

    public async ValueTask<HutaoResponse<GachaDistribution>?> GetGachaDistributionAsync(CancellationToken token = default)
    {
        try
        {
            using (IServiceScope scope = ServiceProvider.CreateScope())
            {
                HutaoUserOptions hutaoUserOptions = scope.ServiceProvider.GetRequiredService<HutaoUserOptions>();
                if (await hutaoUserOptions.GetIsHutaoCloudAllowedAsync(token).ConfigureAwait(false) is not (true, { } accessToken))
                {
                    return default;
                }

                HomaGachaLogClient client = scope.ServiceProvider.GetRequiredService<HomaGachaLogClient>();
                return await client.GetGachaDistributionAsync(accessToken, DistributionType, token).ConfigureAwait(false);
            }
        }
        catch (ObjectDisposedException)
        {
            // Cannot access a disposed object.
            // Object name: 'IServiceProvider'.
            return default;
        }
    }
}
