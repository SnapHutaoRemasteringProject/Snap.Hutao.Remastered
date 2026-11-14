// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Model.Intrinsic;

namespace Snap.Hutao.Remastered.Model.Calculable;

// Level editors bind through this interface and must observe changes made by other editors.
public interface ICalculable : INameIcon<Uri>, INotifyPropertyChanged
{
    QualityType Quality { get; }

    uint LevelCurrent { get; set; }

    uint LevelTarget { get; set; }
}
