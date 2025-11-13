// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using System.Runtime.CompilerServices;
using System.Text;

namespace Snap.Hutao.Extension;

internal static class StringBuilderExtension
{
    extension(StringBuilder sb)
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public StringBuilder AppendIf(bool condition, char? value)
        {
            return condition ? sb.Append(value) : sb;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public StringBuilder AppendIf(bool condition, string? value)
        {
            return condition ? sb.Append(value) : sb;
        }

        public string ToStringTrimEndNewLine()
        {
            int length = sb.Length;
            int index = length - 1;

            while (index >= 0 && (char.IsWhiteSpace(sb[index]) || sb[index] == '\n' || sb[index] == '\r'))
            {
                index--;
            }

            return index < 0 ? string.Empty : sb.ToString(0, index + 1);
        }
    }
}