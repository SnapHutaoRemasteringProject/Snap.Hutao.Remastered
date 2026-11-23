// Copyright (c) Snap Hutao RP. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.UI.Input;

internal static class ShortcutCapture
{
    private static int count;

    public static bool IsActive => Volatile.Read(ref count) > 0;

    public static void Begin() => Interlocked.Increment(ref count);

    public static void End() => Interlocked.Decrement(ref count);
}
