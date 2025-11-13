// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Web.Hoyolab.Hk4e.Event.GachaInfo;

internal static class GachaConfigTypeExtension
{
    extension(GachaType configType)
    {
        public GachaType ToQueryType()
        {
            return configType switch
            {
                GachaType.SpecialActivityAvatar => GachaType.ActivityAvatar,
                _ => configType,
            };
        }
    }
}