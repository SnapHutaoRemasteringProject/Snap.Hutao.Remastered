// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using System.Collections.Immutable;

namespace Snap.Hutao.Remastered.Model.Metadata;

// The level selector is a ComboBox whose items cross the XAML boundary. A
// closed generic item type cannot provide a runtime property provider for a
// DisplayMemberPath lookup in the native build, so the display label lives on
// this non-generic type and is shown through ToString.
public sealed class ParameterLevel
{
    public ParameterLevel(string level, ImmutableArray<ParameterDescription> parameters)
    {
        Level = level;
        Parameters = parameters;
    }

    public string Level { get; }

    public ImmutableArray<ParameterDescription> Parameters { get; }

    public override string ToString()
    {
        return Level;
    }
}
