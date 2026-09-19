// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Model.Intrinsic;
using Windows.UI;

namespace Snap.Hutao.Remastered.UI.Xaml.Data.Converter.Specialized;

[DependencyProperty<Color>("Effective", NotNull = true)]
[DependencyProperty<Color>("Muted", NotNull = true)]
[DependencyProperty<Color>("Crit", NotNull = true)]
public sealed partial class ReliquarySubStatStateToColorConverter : DependencyValueConverter<ReliquarySubStatState, Color>
{
    public ReliquarySubStatStateToColorConverter()
    {
        Effective = ColorHelper.ToColor(0xE4000000);
        Muted = ColorHelper.ToColor(0xFF9E9E9E);
        Crit = ColorHelper.ToColor(0xFFFFB300);
    }

    public override Color Convert(ReliquarySubStatState from)
    {
        return from switch
        {
            ReliquarySubStatState.Crit => Crit,
            ReliquarySubStatState.Ineffective or ReliquarySubStatState.Inactive => Muted,
            _ => Effective,
        };
    }
}
