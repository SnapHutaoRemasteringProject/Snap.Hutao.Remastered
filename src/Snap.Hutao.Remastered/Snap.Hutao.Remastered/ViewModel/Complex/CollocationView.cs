// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;
using Snap.Hutao.Remastered.Model.Intrinsic;
using Snap.Hutao.Remastered.Model;

namespace Snap.Hutao.Remastered.ViewModel.Complex;

public abstract partial class CollocationView : RateAndDelta, INameIcon<Uri>
{
    protected CollocationView(double rate, double? lastRate)
        : base(rate, lastRate)
    {
    }

    public abstract string Name { get; }

    public abstract Uri Icon { get; }

    public abstract QualityType Quality { get; }
}
