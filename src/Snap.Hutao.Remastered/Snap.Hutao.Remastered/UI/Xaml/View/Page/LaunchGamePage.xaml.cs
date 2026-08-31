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
    private bool isInitializing;

    public LaunchGamePage()
    {
        InitializeComponent();
    }

    protected override void LoadingOverride()
    {
        // 树脂物品与 FastSkipTalk 开关是 TwoWay 绑定，页面挂载时绑定会把持久化的值
        // 推入 IsOn 并触发 Toggled，若此时弹确认框就会"进页面就弹窗"。
        // 因此这里在绑定激活前先把 IsOn 置为持久化值：绑定随后推入相同值时，依赖属性
        // 值未变化、不会触发变更回调（这正是弹窗只在 DB 值≠默认时才出现的原因）
        isInitializing = true;
        InitializeDataContext<LaunchGameViewModel>();

        if (DataContext is LaunchGameViewModel { LaunchOptions: { } launchOptions })
        {
            OriginalResinAllowedToggleSwitch.IsOn = launchOptions.ResinListItemId000106Allowed.Value;
            PrimogemAllowedToggleSwitch.IsOn = launchOptions.ResinListItemId000201Allowed.Value;
            FragileResinAllowedToggleSwitch.IsOn = launchOptions.ResinListItemId107009Allowed.Value;
            TransientResinAllowedToggleSwitch.IsOn = launchOptions.ResinListItemId107012Allowed.Value;
            CondensedResinAllowedToggleSwitch.IsOn = launchOptions.ResinListItemId220007Allowed.Value;
            FastSkipTalkToggleSwitch.IsOn = launchOptions.FastSkipTalk.Value;
        }

        isInitializing = false;
    }

    // 为什么用 Code-Behind 而非 ViewModel：
    // ToggleSwitch 的 IsOn 是 TwoWay 绑定到 LaunchOptions.FastSkipTalk.Value，
    // 用户拨动开关时值在 Toggled 事件触发前就已经被写入为 true，ViewModel 没有拦截点。
    // 因此在此处"事后裁决"：未确认则回拨 toggleSwitch.IsOn，使绑定把值回写为 false。
    private async void FastSkipTalkToggleSwitch_Toggled(object sender, RoutedEventArgs e)
    {
        if (isInitializing)
        {
            return;
        }

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
                    InfoBarSeverity.Error)
                .ConfigureAwait(false);

            bool confirmed = await dialog.ConfirmAsync().ConfigureAwait(false);
            if (!confirmed)
            {
                await contentDialogFactory.TaskContext.SwitchToMainThreadAsync();
                toggleSwitch.IsOn = false;
            }
        }
    }

    // 为什么用 Code-Behind 而非 ViewModel：与 FastSkipTalk 同理。
    // 树脂物品的 IsOn 同样是 TwoWay 绑定，Toggled 触发前值已被写入，
    // 因此在此"事后裁决"：用户取消确认则回拨 toggleSwitch.IsOn，使绑定把值回写为开启。
    private async void ResinListItemToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (isInitializing)
        {
            return;
        }

        if (sender is not ToggleSwitch { IsOn: false } toggleSwitch)
        {
            return;
        }

        if (DataContext is not LaunchGameViewModel { LaunchOptions: { } launchOptions })
        {
            return;
        }

        // 5 个树脂物品选项是否已全部关闭
        bool allDisabled = !launchOptions.ResinListItemId000106Allowed.Value
            && !launchOptions.ResinListItemId000201Allowed.Value
            && !launchOptions.ResinListItemId107009Allowed.Value
            && !launchOptions.ResinListItemId107012Allowed.Value
            && !launchOptions.ResinListItemId220007Allowed.Value;

        if (!allDisabled)
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
                    SH.ViewDialogResinListItemAllDisabledTitle,
                    SH.ViewDialogResinListItemAllDisabledHint,
                    InfoBarSeverity.Warning)
                .ConfigureAwait(false);

            bool confirmed = await dialog.ConfirmAsync().ConfigureAwait(false);
            if (!confirmed)
            {
                await contentDialogFactory.TaskContext.SwitchToMainThreadAsync();
                toggleSwitch.IsOn = true;
            }
        }
    }
}