// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;

namespace Snap.Hutao.Remastered.UI.Xaml.Markup;

[MarkupExtensionReturnType(ReturnType = typeof(FontIcon))]
public sealed partial class FontIconExtension : MarkupExtension
{
    private const string SymbolFontFamilyName = "Segoe Fluent Icons,Segoe MDL2 Assets";

    public string Glyph { get; set; } = default!;

    public double FontSize { get; set; } = 12;

    protected override object ProvideValue()
    {
        // A FontIcon built in code carries no style until the framework applies the
        // implicit one, so its FontFamily is unset in between. XAML diagnostics treat
        // that unset value as invalid, so resolve the symbol family eagerly.
        return new FontIcon
        {
            Glyph = Glyph,
            FontSize = FontSize,
            FontFamily = ResolveSymbolFontFamily(),
        };
    }

    private static FontFamily ResolveSymbolFontFamily()
    {
        if (Application.Current is { } application &&
            application.Resources.TryGetValue("SymbolThemeFontFamily", out object? value) &&
            value is FontFamily family)
        {
            return family;
        }

        return new FontFamily(SymbolFontFamilyName);
    }
}