// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;
using Snap.Hutao.Remastered.Core.Setting;
using Snap.Hutao.Remastered.Model;
using Snap.Hutao.Remastered.Service.Game;
using Snap.Hutao.Remastered.Win32;
using Snap.Hutao.Remastered.Win32.Foundation;
using Snap.Hutao.Remastered.Win32.UI.Input.KeyboardAndMouse;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Snap.Hutao.Remastered.UI.Input.HotKey;

[Service(ServiceLifetime.Singleton)]
public sealed partial class HotKeyOptions : ObservableObject, IDisposable
{
    private readonly IServiceProvider serviceProvider;
    private readonly ITaskContext taskContext;

    private bool isDisposed;

    [GeneratedConstructor]
    public partial HotKeyOptions(IServiceProvider serviceProvider);

    static unsafe HotKeyOptions()
    {
        HutaoNativeHotKeyAction.InitializeBeforeSwitchCallback(HutaoNativeHotKeyBeforeSwitchCallback.Create(&HandleShouldPreventSwitch));
    }

    public static bool IsInGameOnly
    {
        get => LocalSetting.Get(SettingKeys.HotKeyRepeatForeverInGameOnly, false);
        set => LocalSetting.Set(SettingKeys.HotKeyRepeatForeverInGameOnly, value);
    }

    public static ImmutableArray<NameValue<VIRTUAL_KEY>> VirtualKeys { get => Input.VirtualKeys.HotKeyValues; }

    [ObservableProperty]
    public partial HotKeyCombination? MouseClickRepeatForeverKeyCombination { get; set; }

    [ObservableProperty]
    public partial HotKeyCombination? KeyPressRepeatForeverKeyCombination { get; set; }

    public async ValueTask InitializeAsync(CancellationToken token = default)
    {
        await taskContext.SwitchToMainThreadAsync();
        token.ThrowIfCancellationRequested();
        if (isDisposed) return;
        if (MouseClickRepeatForeverKeyCombination is not null && KeyPressRepeatForeverKeyCombination is not null) return;

        // Publish only initialized actions. Bindings must never observe a native
        // placeholder and persist its temporary disabled value as user intent.
        HotKeyCombination mouse = new(serviceProvider, HutaoNativeHotKeyActionKind.MouseClickRepeatForever, SettingKeys.HotKeyMouseClickRepeatForever);
        HotKeyCombination key = new(serviceProvider, HutaoNativeHotKeyActionKind.KeyPressRepeatForever, SettingKeys.HotKeyKeyPressRepeatForever);
        try
        {
            mouse.Initialize();
            key.Initialize();
        }
        catch
        {
            mouse.Dispose();
            key.Dispose();
            throw;
        }
        MouseClickRepeatForeverKeyCombination = mouse;
        KeyPressRepeatForeverKeyCombination = key;
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref isDisposed, true))
        {
            return;
        }

        MouseClickRepeatForeverKeyCombination?.Dispose();
        KeyPressRepeatForeverKeyCombination?.Dispose();
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static BOOL HandleShouldPreventSwitch()
    {
        // This callback should always be called by the public wndproc, so we are on the main thread.
        return IsInGameOnly && !GameLifeCycle.IsGameRunningRequiresMainThread();
    }
}
