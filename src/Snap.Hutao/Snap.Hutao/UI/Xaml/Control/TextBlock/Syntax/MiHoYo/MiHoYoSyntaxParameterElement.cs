// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Core.ExceptionService;
using System.Collections.Immutable;

namespace Snap.Hutao.UI.Xaml.Control.TextBlock.Syntax.MiHoYo;

internal sealed class MiHoYoSyntaxParameterElement : MiHoYoSyntaxElement
{
    public MiHoYoSyntaxParameterElement(TextPosition position, TextPosition idPosition, ImmutableArray<MiHoYoSyntaxElement> children)
        : base(position, children)
    {
        IdPosition = idPosition;
    }

    public TextPosition IdPosition { get; }

    public MiHoYoSyntaxParameterKind GetParameterKind(ReadOnlySpan<char> source)
    {
        return source[IdPosition.Start] switch
        {
            'P' => MiHoYoSyntaxParameterKind.ProudSkill,
            _ => throw HutaoException.Throw($"Unexpected param kind :{source[IdPosition.Start]}"),
        };
    }

    public ReadOnlySpan<char> GetIdSpan(ReadOnlySpan<char> source)
    {
        return source.Slice(IdPosition.Start, IdPosition.Length);
    }
}