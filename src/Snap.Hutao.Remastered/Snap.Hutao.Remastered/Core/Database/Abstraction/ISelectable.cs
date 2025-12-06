// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Model.Entity.Abstraction;

namespace Snap.Hutao.Remastered.Core.Database.Abstraction;

internal interface ISelectable : IAppDbEntity
{
    bool IsSelected { get; set; }
}