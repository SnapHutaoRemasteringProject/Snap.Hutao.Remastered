// Copyright (c) 2026 DGP Studio. All rights reserved.
// Licensed under the MIT license.
// Adapted from Snap.Nicole.

namespace Snap.Hutao.Remastered.Core.AI;

internal interface IIdentifiable<T>
    where T : struct, IEquatable<T>
{
    T Id { get; }
}
