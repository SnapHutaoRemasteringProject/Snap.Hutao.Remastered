// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Snap.Hutao.Remastered.Factory.ContentDialog;
using Snap.Hutao.Remastered.UI.Content;
using Snap.Hutao.Remastered.UI.Xaml.Control;
using Snap.Hutao.Remastered.UI.Xaml.View.Dialog;
using Snap.Hutao.Remastered.ViewModel.Game;

namespace Snap.Hutao.Remastered.UI.Xaml.View.Page;

public sealed partial class LaunchGamePage : ScopedPage
{
    public LaunchGamePage()
    {
        InitializeComponent();
    }

    protected override void LoadingOverride()
    {
        InitializeDataContext<LaunchGameViewModel>();
    }

    // 为什么用 Code-Behind 而非 ViewModel：
    // ToggleSwitch 的 IsOn 是 TwoWay 绑定到 LaunchOptions.FastSkipTalk.Value，
    // 用户拨动开关时值在 Toggled 事件触发前就已经被写入为 true，ViewModel 没有拦截点。
    // 因此在此处"事后裁决"：未确认则回拨 toggleSwitch.IsOn，使绑定把值回写为 false。
    private async void FastSkipTalkToggleSwitch_Toggled(object sender, RoutedEventArgs e)
    {
        if (sender is not ToggleSwitch { IsOn: true } toggleSwitch)
        {
            return;
        }

        XamlContext? context = XamlRoot.XamlContext();
        ArgumentNullException.ThrowIfNull(context);

        using (IServiceScope scope = context.ServiceProvider.CreateScope())
        {
            IContentDialogFactory contentDialogFactory = scope.ServiceProvider.GetRequiredService<IContentDialogFactory>();
            CountdownConfirmDialog dialog = await contentDialogFactory
                .CreateInstanceAsync<CountdownConfirmDialog>(
                    scope.ServiceProvider,
                    SH.ViewDialogFastSkipTalkConfirmTitle,
                    SH.ViewDialogFastSkipTalkConfirmHint,
                    SH.ViewDialogFastSkipTalkConfirmPrimaryButtonText)
                .ConfigureAwait(false);

            bool confirmed = await dialog.ConfirmAsync().ConfigureAwait(false);
            if (!confirmed)
            {
                await contentDialogFactory.TaskContext.SwitchToMainThreadAsync();
                toggleSwitch.IsOn = false;
            }
        }
    }
}