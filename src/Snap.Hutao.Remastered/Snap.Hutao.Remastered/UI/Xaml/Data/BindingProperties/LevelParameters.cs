// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml.Data;

namespace Snap.Hutao.Remastered.Model.Metadata;

// Only the level label uses runtime lookup; parameter contents use compiled bindings.
public partial class LevelParameters<TLevel, TParam> : IBindableCustomPropertyImplementation
{
    BindableCustomProperty IBindableCustomPropertyImplementation.GetProperty(string name)
    {
        return name == nameof(Level)
            ? new BindableCustomProperty(true, false, nameof(Level), typeof(TLevel),
                static instance => ((LevelParameters<TLevel, TParam>)instance).Level!, null, null, null)
            : default!;
    }

    BindableCustomProperty IBindableCustomPropertyImplementation.GetProperty(Type indexParameterType) => default!;
}
