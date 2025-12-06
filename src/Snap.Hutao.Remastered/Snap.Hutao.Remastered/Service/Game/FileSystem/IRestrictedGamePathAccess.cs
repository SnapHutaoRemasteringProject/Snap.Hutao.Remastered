// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core.Property;
using Snap.Hutao.Remastered.Service.Game.PathAbstraction;
using System.Collections.Immutable;

namespace Snap.Hutao.Remastered.Service.Game.FileSystem;

internal interface IRestrictedGamePathAccess
{
    IObservableProperty<GamePathEntry?> GamePathEntry { get; }

    IObservableProperty<ImmutableArray<GamePathEntry>> GamePathEntries { get; }

    AsyncReaderWriterLock GamePathLock { get; }
}