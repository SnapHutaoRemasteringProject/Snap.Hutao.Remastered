// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml.Data;

namespace Snap.Hutao.Remastered.Model;

// CsWinRT's property generator does not support generic declarations. Its runtime
// accepts this same interface before considering the reflection-only fallback.
// Keep it in the UI compilation so consumers of the plain model need no WinUI reference.
public partial class NameValue<T> : IBindableCustomPropertyImplementation
{
    BindableCustomProperty IBindableCustomPropertyImplementation.GetProperty(string name)
    {
        return name switch
        {
            nameof(Name) => new(true, false, nameof(Name), typeof(string),
                static instance => ((NameValue<T>)instance).Name, null, null, null),
            nameof(Value) => new(true, false, nameof(Value), typeof(T),
                static instance => ((NameValue<T>)instance).Value!, null, null, null),
            _ => default!,
        };
    }

    BindableCustomProperty IBindableCustomPropertyImplementation.GetProperty(Type indexParameterType) => default!;
}
