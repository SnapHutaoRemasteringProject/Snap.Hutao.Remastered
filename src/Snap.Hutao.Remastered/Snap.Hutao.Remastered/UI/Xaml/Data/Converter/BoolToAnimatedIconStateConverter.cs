// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.WinUI.Converters;

namespace Snap.Hutao.Remastered.UI.Xaml.Data.Converter;

public sealed partial class BoolToAnimatedIconStateConverter : BoolToObjectConverter
{
    public BoolToAnimatedIconStateConverter()
    {
        TrueValue = "NormalOn";
        FalseValue = "NormalOff";
    }
}
