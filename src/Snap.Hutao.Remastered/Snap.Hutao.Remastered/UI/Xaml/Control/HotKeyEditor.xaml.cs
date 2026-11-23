// Copyright (c) Snap Hutao RP. All rights reserved.
// Licensed under the MIT license.

using DevWinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Snap.Hutao.Remastered.Model;
using Snap.Hutao.Remastered.UI.Input;
using Snap.Hutao.Remastered.UI.Input.HotKey;
using Snap.Hutao.Remastered.Win32.UI.Input.KeyboardAndMouse;
using Windows.System;

namespace Snap.Hutao.Remastered.UI.Xaml.Control;

[DependencyProperty<HotKeyCombination>("Combination", PropertyChangedCallbackName = nameof(OnShortcutChanged))]
[DependencyProperty<NameValue<VIRTUAL_KEY>>("SelectedKey", PropertyChangedCallbackName = nameof(OnShortcutChanged))]
public sealed partial class HotKeyEditor : UserControl
{
    private HotKeyParameter? draft;
    private bool isRecording;

    public HotKeyEditor()
    {
        InitializeComponent();
        MainShortcut.KeyNameProvider = static key => VirtualKeys.First(VirtualKeys.Values, (VIRTUAL_KEY)key).Name;
        MainShortcut.IsEnabled = false;
    }

    private static void OnShortcutChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        ((HotKeyEditor)sender).RefreshShortcut();
    }

    private void RefreshShortcut()
    {
        HotKeyCombination? combination = Combination;
        HOT_KEY_MODIFIERS modifiers = combination?.Modifiers ?? default;
        VIRTUAL_KEY key = combination?.Key ?? SelectedKey?.Value ?? VIRTUAL_KEY.VK__none_;
        List<object> keys = [];
        if (key != VIRTUAL_KEY.VK__none_)
        {
            if (modifiers.HasFlag(HOT_KEY_MODIFIERS.MOD_CONTROL))
            {
                keys.Add(new KeyVisualInfo { Key = VirtualKey.Control, KeyName = "Ctrl" });
            }

            if (modifiers.HasFlag(HOT_KEY_MODIFIERS.MOD_SHIFT))
            {
                keys.Add(new KeyVisualInfo { Key = VirtualKey.Shift, KeyName = "Shift" });
            }

            if (modifiers.HasFlag(HOT_KEY_MODIFIERS.MOD_ALT))
            {
                keys.Add(new KeyVisualInfo { Key = VirtualKey.Menu, KeyName = "Alt" });
            }

            keys.Add(new KeyVisualInfo { Key = (VirtualKey)key, KeyName = VirtualKeys.First(VirtualKeys.Values, key).Name });
        }

        MainShortcut.Keys = keys;
        MainShortcut.IsEnabled = combination is not null || SelectedKey is not null;
        MainShortcut.Title = combination is null ? SH.ViewShortcutEditorSingleKey : SH.ViewShortcutEditorCombination;
    }

    private void OnOpened(object? sender, ContentDialogOpenedEventArgs args)
    {
        if (!isRecording)
        {
            isRecording = true;
            ShortcutCapture.Begin();
        }
    }

    private void OnClosed(object? sender, ContentDialogClosedEventArgs args) => FinishRecording();

    private void OnUnloaded(object sender, RoutedEventArgs args)
    {
        if (isRecording)
        {
            FinishRecording();
            MainShortcut.CloseContentDialog();
        }
    }

    private void FinishRecording()
    {
        if (isRecording)
        {
            isRecording = false;
            ShortcutCapture.End();
        }

        draft = null;
        MainShortcut.IsError = false;
    }

    private void OnValidateShortcut(object? sender, ShortcutValidationEventArgs args)
    {
        draft = ReadShortcut(args.KeysInfo, Combination is not null);
        args.ContentDialog.IsPrimaryButtonEnabled = draft is not null;
        MainShortcut.ErrorTitle = SH.ViewShortcutEditorInvalid;
        MainShortcut.IsError = draft is null;
    }

    private static HotKeyParameter? ReadShortcut(IReadOnlyList<KeyVisualInfo> keys, bool allowModifiers)
    {
        HOT_KEY_MODIFIERS modifiers = default;
        VIRTUAL_KEY mainKey = VIRTUAL_KEY.VK__none_;
        foreach (KeyVisualInfo item in keys)
        {
            if (item.Key is not { } virtualKey)
            {
                return null;
            }

            VIRTUAL_KEY key = (VIRTUAL_KEY)virtualKey;
            if (key == VIRTUAL_KEY.VK__none_ || VirtualKeys.First(VirtualKeys.Values, key).Value != key)
            {
                return null;
            }

            if (allowModifiers)
            {
                if (key is VIRTUAL_KEY.VK_LWIN or VIRTUAL_KEY.VK_RWIN)
                {
                    return null;
                }

                HOT_KEY_MODIFIERS modifier = key switch
                {
                    VIRTUAL_KEY.VK_CONTROL or VIRTUAL_KEY.VK_LCONTROL or VIRTUAL_KEY.VK_RCONTROL => HOT_KEY_MODIFIERS.MOD_CONTROL,
                    VIRTUAL_KEY.VK_SHIFT or VIRTUAL_KEY.VK_LSHIFT or VIRTUAL_KEY.VK_RSHIFT => HOT_KEY_MODIFIERS.MOD_SHIFT,
                    VIRTUAL_KEY.VK_MENU or VIRTUAL_KEY.VK_LMENU or VIRTUAL_KEY.VK_RMENU => HOT_KEY_MODIFIERS.MOD_ALT,
                    _ => default,
                };
                if (modifier != default)
                {
                    modifiers |= modifier;
                    continue;
                }
            }

            if (mainKey != VIRTUAL_KEY.VK__none_)
            {
                return null;
            }

            mainKey = key;
        }

        return mainKey == VIRTUAL_KEY.VK__none_ ? null : new HotKeyParameter(modifiers, mainKey);
    }

    private void OnMainShortcutPrimaryButtonClick(object? sender, ContentDialogButtonClickEventArgs args)
    {
        bool saved = draft is not { } value || ApplyShortcut(value);
        MainShortcut.CanCloseContentDialog(saved);
        args.Cancel = !saved;
    }

    private void OnMainShortcutSecondaryButtonClick(object? sender, ContentDialogButtonClickEventArgs args)
    {
        bool saved = ApplyShortcut(HotKeyParameter.Default);
        MainShortcut.CanCloseContentDialog(saved);
        args.Cancel = !saved;
    }

    private void OnMainShortcutCloseButtonClick(object? sender, ContentDialogButtonClickEventArgs args)
    {
        draft = null;
    }

    private bool ApplyShortcut(HotKeyParameter value)
    {
        if (Combination is { } combination)
        {
            if (!combination.TrySetShortcut(value))
            {
                return false;
            }
        }
        else if (SelectedKey is not null)
        {
            SelectedKey = VirtualKeys.First(VirtualKeys.Values, value.Key);
        }
        else
        {
            return false;
        }

        RefreshShortcut();
        return true;
    }
}
