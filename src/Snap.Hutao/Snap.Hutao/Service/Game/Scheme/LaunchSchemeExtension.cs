// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Model.Entity.Primitive;
using Snap.Hutao.Model.Intrinsic;

namespace Snap.Hutao.Service.Game.Scheme;

internal static class LaunchSchemeExtension
{
    extension(LaunchScheme scheme)
    {
        public SchemeType SchemeType
        {
            get => (scheme.Channel, scheme.IsOversea) switch
            {
                (ChannelType.Bili, false) => SchemeType.ChineseBilibili,
                (_, false) => SchemeType.ChineseOfficial,
                (_, true) => SchemeType.Oversea,
            };
        }
    }
}