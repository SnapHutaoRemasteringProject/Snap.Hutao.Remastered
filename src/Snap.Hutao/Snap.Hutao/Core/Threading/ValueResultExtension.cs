// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Core.Threading;

internal static class ValueResultExtension
{
    extension<TValue>(in ValueResult<bool, TValue> valueResult)
    {
        public bool TryGetValue([NotNullWhen(true)][MaybeNullWhen(false)] out TValue value)
        {
            value = valueResult.Value;
            return valueResult.IsOk;
        }
    }
}