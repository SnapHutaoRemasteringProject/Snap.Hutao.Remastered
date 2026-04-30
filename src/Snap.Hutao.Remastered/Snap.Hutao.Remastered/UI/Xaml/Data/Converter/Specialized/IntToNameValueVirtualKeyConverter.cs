// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Model;
using Snap.Hutao.Remastered.Service.Game;
using Snap.Hutao.Remastered.Win32.UI.Input.KeyboardAndMouse;

namespace Snap.Hutao.Remastered.UI.Xaml.Data.Converter.Specialized;

/// <summary>
/// Converter between <see cref="NameValue{VIRTUAL_KEY}"/> and <see cref="int"/>.
/// Used for binding selected virtual key item to an integer key code.
/// </summary>
public sealed partial class IntToNameValueVirtualKeyConverter : DependencyValueConverter<int, NameValue<VIRTUAL_KEY>>
{
    public override NameValue<VIRTUAL_KEY> Convert(int from)
    {
        // source -> target: source is int (CombineMenuHotkey.Value), target expects VIRTUAL_KEY or underlying numeric
        if (from is int i)
        {
            return LaunchOptions.VirtualKeys.Where(x => x.Value == (VIRTUAL_KEY)i).First();
        }

        return Convert(255);
    }

    public override int ConvertBack(NameValue<VIRTUAL_KEY> to)
    {
        // target -> source: target may be VIRTUAL_KEY, int, ushort or NameValue<VIRTUAL_KEY>
        if (to is NameValue<VIRTUAL_KEY> vk)
        {
            return (int)vk.Value;
        }

        if (to is null)
        {
            throw new NotSupportedException();
        }

        return (int)VIRTUAL_KEY.VK__none_;
    }
}
