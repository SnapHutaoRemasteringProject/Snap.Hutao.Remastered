// Copyright (c) 2026 DGP Studio. All rights reserved.
// Licensed under the MIT license.
// Adapted from Snap.Nicole.

using System.Text;

namespace Snap.Hutao.Remastered.Core.AI;

internal static class StringBuilderExtensions
{
    extension(StringBuilder stringBuilder)
    {
        public string ToStringAndClear()
        {
            string result = stringBuilder.ToString();
            stringBuilder.Clear();
            return result;
        }
    }
}
