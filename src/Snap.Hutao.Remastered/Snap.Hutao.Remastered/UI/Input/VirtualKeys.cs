// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Model;
using Snap.Hutao.Remastered.Win32.UI.Input.KeyboardAndMouse;
using System.Collections.Immutable;

namespace Snap.Hutao.Remastered.UI.Input;

public static class VirtualKeys
{
    public static ImmutableArray<NameValue<VIRTUAL_KEY>> Values { get; } = CreateValues();

    public static ImmutableArray<NameValue<VIRTUAL_KEY>> HotKeyValues { get; } = Values;

    public static NameValue<VIRTUAL_KEY> First(ImmutableArray<NameValue<VIRTUAL_KEY>> array, VIRTUAL_KEY value)
    {
        foreach (NameValue<VIRTUAL_KEY> item in array)
        {
            if (item.Value == value)
            {
                return item;
            }
        }

        // VK__none_ is the last item and represents "not set". Falling back to
        // it keeps an outdated persisted value from breaking initialization.
        return array[^1];
    }

    private static ImmutableArray<NameValue<VIRTUAL_KEY>> CreateValues()
    {
        return
        [
            .. Enum.GetValues<VIRTUAL_KEY>()
                .Distinct()
                .Where(static key => IsKeyboardKey(key))
                .Select(static key => new NameValue<VIRTUAL_KEY>(GetDisplayName(key), key)),
        ];
    }

    private static bool IsKeyboardKey(VIRTUAL_KEY key)
    {
        return key switch
        {
            VIRTUAL_KEY.VK_LBUTTON or VIRTUAL_KEY.VK_RBUTTON or VIRTUAL_KEY.VK_MBUTTON or VIRTUAL_KEY.VK_XBUTTON1 or VIRTUAL_KEY.VK_XBUTTON2 => false,
            >= VIRTUAL_KEY.VK_NAVIGATION_VIEW and <= VIRTUAL_KEY.VK_NAVIGATION_CANCEL => false,
            >= VIRTUAL_KEY.VK_GAMEPAD_A and <= VIRTUAL_KEY.VK_GAMEPAD_RIGHT_THUMBSTICK_LEFT => false,
            VIRTUAL_KEY.VK_PACKET => false,
            _ => true,
        };
    }

    private static string GetDisplayName(VIRTUAL_KEY key)
    {
        int code = (int)key;

        if (code is >= (int)VIRTUAL_KEY.VK_0 and <= (int)VIRTUAL_KEY.VK_9)
        {
            return ((char)('0' + (code - (int)VIRTUAL_KEY.VK_0))).ToString();
        }

        if (code is >= (int)VIRTUAL_KEY.VK_A and <= (int)VIRTUAL_KEY.VK_Z)
        {
            return ((char)('A' + (code - (int)VIRTUAL_KEY.VK_A))).ToString();
        }

        if (code is >= (int)VIRTUAL_KEY.VK_F1 and <= (int)VIRTUAL_KEY.VK_F24)
        {
            return $"F{code - (int)VIRTUAL_KEY.VK_F1 + 1}";
        }

        if (code is >= (int)VIRTUAL_KEY.VK_NUMPAD0 and <= (int)VIRTUAL_KEY.VK_NUMPAD9)
        {
            return $"Num {code - (int)VIRTUAL_KEY.VK_NUMPAD0}";
        }

        return key switch
        {
            VIRTUAL_KEY.VK_CANCEL => "Break",
            VIRTUAL_KEY.VK_BACK => "Backspace",
            VIRTUAL_KEY.VK_TAB => "Tab",
            VIRTUAL_KEY.VK_CLEAR => "Clear",
            VIRTUAL_KEY.VK_RETURN => "Enter",
            VIRTUAL_KEY.VK_SHIFT => "Shift",
            VIRTUAL_KEY.VK_CONTROL => "Ctrl",
            VIRTUAL_KEY.VK_MENU => "Alt",
            VIRTUAL_KEY.VK_PAUSE => "Pause",
            VIRTUAL_KEY.VK_CAPITAL => "Caps Lock",
            VIRTUAL_KEY.VK_KANA => "Kana",
            VIRTUAL_KEY.VK_IME_ON => "IME On",
            VIRTUAL_KEY.VK_JUNJA => "Junja",
            VIRTUAL_KEY.VK_FINAL => "Final",
            VIRTUAL_KEY.VK_HANJA => "Hanja",
            VIRTUAL_KEY.VK_IME_OFF => "IME Off",
            VIRTUAL_KEY.VK_ESCAPE => "Esc",
            VIRTUAL_KEY.VK_CONVERT => "Convert",
            VIRTUAL_KEY.VK_NONCONVERT => "Non Convert",
            VIRTUAL_KEY.VK_ACCEPT => "Accept",
            VIRTUAL_KEY.VK_MODECHANGE => "Mode Change",
            VIRTUAL_KEY.VK_SPACE => "Space",
            VIRTUAL_KEY.VK_PRIOR => "Page Up",
            VIRTUAL_KEY.VK_NEXT => "Page Down",
            VIRTUAL_KEY.VK_END => "End",
            VIRTUAL_KEY.VK_HOME => "Home",
            VIRTUAL_KEY.VK_LEFT => "Left",
            VIRTUAL_KEY.VK_UP => "Up",
            VIRTUAL_KEY.VK_RIGHT => "Right",
            VIRTUAL_KEY.VK_DOWN => "Down",
            VIRTUAL_KEY.VK_SELECT => "Select",
            VIRTUAL_KEY.VK_PRINT => "Print",
            VIRTUAL_KEY.VK_EXECUTE => "Execute",
            VIRTUAL_KEY.VK_SNAPSHOT => "Print Screen",
            VIRTUAL_KEY.VK_INSERT => "Insert",
            VIRTUAL_KEY.VK_DELETE => "Delete",
            VIRTUAL_KEY.VK_HELP => "Help",
            VIRTUAL_KEY.VK_LWIN => "Left Win",
            VIRTUAL_KEY.VK_RWIN => "Right Win",
            VIRTUAL_KEY.VK_APPS => "Menu",
            VIRTUAL_KEY.VK_SLEEP => "Sleep",
            VIRTUAL_KEY.VK_MULTIPLY => "Num *",
            VIRTUAL_KEY.VK_ADD => "Num +",
            VIRTUAL_KEY.VK_SEPARATOR => "Num ,",
            VIRTUAL_KEY.VK_SUBTRACT => "Num -",
            VIRTUAL_KEY.VK_DECIMAL => "Num .",
            VIRTUAL_KEY.VK_DIVIDE => "Num /",
            VIRTUAL_KEY.VK_NUMLOCK => "Num Lock",
            VIRTUAL_KEY.VK_SCROLL => "Scroll Lock",
            VIRTUAL_KEY.VK_LSHIFT => "Left Shift",
            VIRTUAL_KEY.VK_RSHIFT => "Right Shift",
            VIRTUAL_KEY.VK_LCONTROL => "Left Ctrl",
            VIRTUAL_KEY.VK_RCONTROL => "Right Ctrl",
            VIRTUAL_KEY.VK_LMENU => "Left Alt",
            VIRTUAL_KEY.VK_RMENU => "Right Alt",
            VIRTUAL_KEY.VK_BROWSER_BACK => "Browser Back",
            VIRTUAL_KEY.VK_BROWSER_FORWARD => "Browser Forward",
            VIRTUAL_KEY.VK_BROWSER_REFRESH => "Browser Refresh",
            VIRTUAL_KEY.VK_BROWSER_STOP => "Browser Stop",
            VIRTUAL_KEY.VK_BROWSER_SEARCH => "Browser Search",
            VIRTUAL_KEY.VK_BROWSER_FAVORITES => "Browser Favorites",
            VIRTUAL_KEY.VK_BROWSER_HOME => "Browser Home",
            VIRTUAL_KEY.VK_VOLUME_MUTE => "Volume Mute",
            VIRTUAL_KEY.VK_VOLUME_DOWN => "Volume Down",
            VIRTUAL_KEY.VK_VOLUME_UP => "Volume Up",
            VIRTUAL_KEY.VK_MEDIA_NEXT_TRACK => "Media Next",
            VIRTUAL_KEY.VK_MEDIA_PREV_TRACK => "Media Previous",
            VIRTUAL_KEY.VK_MEDIA_STOP => "Media Stop",
            VIRTUAL_KEY.VK_MEDIA_PLAY_PAUSE => "Media Play/Pause",
            VIRTUAL_KEY.VK_LAUNCH_MAIL => "Launch Mail",
            VIRTUAL_KEY.VK_LAUNCH_MEDIA_SELECT => "Launch Media",
            VIRTUAL_KEY.VK_LAUNCH_APP1 => "Launch App 1",
            VIRTUAL_KEY.VK_LAUNCH_APP2 => "Launch App 2",
            VIRTUAL_KEY.VK_OEM_1 => ";",
            VIRTUAL_KEY.VK_OEM_PLUS => "+",
            VIRTUAL_KEY.VK_OEM_COMMA => ",",
            VIRTUAL_KEY.VK_OEM_MINUS => "-",
            VIRTUAL_KEY.VK_OEM_PERIOD => ".",
            VIRTUAL_KEY.VK_OEM_2 => "/",
            VIRTUAL_KEY.VK_OEM_3 => "`",
            VIRTUAL_KEY.VK_OEM_4 => "[",
            VIRTUAL_KEY.VK_OEM_5 => "\\",
            VIRTUAL_KEY.VK_OEM_6 => "]",
            VIRTUAL_KEY.VK_OEM_7 => "'",
            VIRTUAL_KEY.VK_OEM_8 => "OEM 8",
            VIRTUAL_KEY.VK_ABNT_C1 => "ABNT C1",
            VIRTUAL_KEY.VK_ABNT_C2 => "ABNT C2",
            VIRTUAL_KEY.VK_OEM_NEC_EQUAL => "Num =",
            VIRTUAL_KEY.VK_OEM_102 => "OEM 102",
            VIRTUAL_KEY.VK_PLAY => "Play",
            VIRTUAL_KEY.VK_ZOOM => "Zoom",
            VIRTUAL_KEY.VK_OEM_CLEAR => "OEM Clear",
            VIRTUAL_KEY.VK__none_ => "None",
            _ => GetFallbackName(key),
        };
    }

    private static string GetFallbackName(VIRTUAL_KEY key)
    {
        string name = key.ToString();
        return name.StartsWith("VK_", StringComparison.Ordinal) ? name[3..].Replace('_', ' ') : name;
    }
}
