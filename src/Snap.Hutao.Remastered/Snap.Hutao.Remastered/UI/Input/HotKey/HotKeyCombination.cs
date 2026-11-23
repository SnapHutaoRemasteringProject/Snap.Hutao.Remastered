// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;
using JetBrains.Annotations;
using Snap.Hutao.Remastered.Core.Setting;
using Snap.Hutao.Remastered.Service.Notification;
using Snap.Hutao.Remastered.Win32;
using Snap.Hutao.Remastered.Win32.Foundation;
using Snap.Hutao.Remastered.Win32.UI.Input.KeyboardAndMouse;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Snap.Hutao.Remastered.UI.Input.HotKey;

public sealed partial class HotKeyCombination : ObservableObject, IDisposable
{
    private readonly IMessenger messenger;
    private readonly HutaoNativeHotKeyActionKind kind;
    private readonly string settingKey;
    private GCHandle<HotKeyCombination> handle;

    private HutaoNativeHotKeyAction? native;
    private HotKeyParameter parameter;
    private bool isEnabled;

    public HotKeyCombination(IServiceProvider serviceProvider, HutaoNativeHotKeyActionKind kind, string settingKey)
    {
        messenger = serviceProvider.GetRequiredService<IMessenger>();
        this.kind = kind;
        this.settingKey = settingKey;
        isEnabled = LocalSetting.Get($"{settingKey}.IsEnabled", true);

        HotKeyParameter fallback = HotKeyParameter.Default;
        long value = LocalSetting.Get(settingKey, Unsafe.As<HotKeyParameter, long>(ref fallback));
        HotKeyParameter stored = Unsafe.As<long, HotKeyParameter>(ref value);

        // Windows reserves the Win modifier, but older versions allowed saving it.
        parameter = new(stored.Modifiers & ~HOT_KEY_MODIFIERS.MOD_WIN, VirtualKeys.First(VirtualKeys.Values, stored.Key).Value);

        handle = new(this);
    }

    public HOT_KEY_MODIFIERS Modifiers => parameter.Modifiers;

    public VIRTUAL_KEY Key => parameter.Key;

    /// <summary>
    /// Can perform the action.
    /// </summary>
    public bool IsEnabled
    {
        get => isEnabled;
        set
        {
            if (isEnabled == value) return;
            if (native is not null) native.IsEnabled = value;
            LocalSetting.Set($"{settingKey}.IsEnabled", value);
            // Two-way bindings can synchronously re-enter this setter. Finish
            // applying this value before publishing it so the newer write wins.
            SetProperty(ref isEnabled, value);
        }
    }

    /// <summary>
    /// Is performing the action.
    /// </summary>
    [ObservableProperty]
    [UsedImplicitly]
    public partial bool IsOn { get; private set; }

    public string DisplayName { get => ToString(); }

    public unsafe void Initialize()
    {
        native = HutaoNative.Instance.MakeHotKeyAction(kind, HutaoNativeHotKeyActionCallback.Create(&OnAction), handle);
        native.IsEnabled = isEnabled;
        OnPropertyChanged(nameof(IsEnabled));
        TryUpdateNative(parameter);
    }

    public void Dispose()
    {
        handle.Dispose();
        native = default;
    }

    public bool TrySetShortcut(HotKeyParameter next)
    {
        if (parameter.Modifiers == next.Modifiers && parameter.Key == next.Key)
        {
            return true;
        }

        if (!TryUpdateNative(next))
        {
            TryUpdateNative(parameter);
            return false;
        }

        try
        {
            LocalSetting.Set(settingKey, Unsafe.As<HotKeyParameter, long>(ref next));
        }
        catch
        {
            TryUpdateNative(parameter);
            throw;
        }

        parameter = next;
        OnPropertyChanged(nameof(Modifiers));
        OnPropertyChanged(nameof(Key));
        OnPropertyChanged(nameof(DisplayName));
        return true;
    }

    public override string ToString() => FormatShortcut(parameter);

    private static string FormatShortcut(HotKeyParameter value)
    {
        StringBuilder stringBuilder = new();

        if (value.Modifiers.HasFlag(HOT_KEY_MODIFIERS.MOD_CONTROL))
        {
            stringBuilder.Append("Ctrl").Append(" + ");
        }

        if (value.Modifiers.HasFlag(HOT_KEY_MODIFIERS.MOD_SHIFT))
        {
            stringBuilder.Append("Shift").Append(" + ");
        }

        if (value.Modifiers.HasFlag(HOT_KEY_MODIFIERS.MOD_ALT))
        {
            stringBuilder.Append("Alt").Append(" + ");
        }

        stringBuilder.Append(VirtualKeys.First(VirtualKeys.Values, value.Key).Name);

        return stringBuilder.ToString();
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static void OnAction(BOOL isOn, GCHandle<HotKeyCombination> data)
    {
        if (data.Target is not { } combination)
        {
            return;
        }

        combination.IsOn = isOn;
    }

    private bool TryUpdateNative(HotKeyParameter value)
    {
        try
        {
            uint key = value.Key == VIRTUAL_KEY.VK__none_ ? 0 : (uint)value.Key;
            native?.Update(value.Modifiers, key);
            return true;
        }
        catch (Exception ex)
        {
            if (HutaoNative.IsWin32(ex.HResult, WIN32_ERROR.ERROR_HOTKEY_ALREADY_REGISTERED))
            {
                messenger.Send(InfoBarMessage.Warning(SH.FormatCoreWindowHotkeyCombinationRegisterFailed(kind, FormatShortcut(value))));
            }
            else
            {
                messenger.Send(InfoBarMessage.Error(ex));
            }

            return false;
        }
    }
}
