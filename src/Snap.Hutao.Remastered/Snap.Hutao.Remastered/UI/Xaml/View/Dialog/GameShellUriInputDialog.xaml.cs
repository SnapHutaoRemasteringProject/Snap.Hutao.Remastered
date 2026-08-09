// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml.Controls;
using Snap.Hutao.Remastered.Factory.ContentDialog;
using Snap.Hutao.Remastered.Service.Game.PathAbstraction;

namespace Snap.Hutao.Remastered.UI.Xaml.View.Dialog;

/// <summary>
/// 用于输入 Shell URI 以选择已安装应用的对话框。
/// </summary>
[DependencyProperty<string>("ShellUri")]
public sealed partial class GameShellUriInputDialog : ContentDialog
{
    private readonly IContentDialogFactory contentDialogFactory;

    /// <summary>
    /// 初始化 <see cref="GameShellUriInputDialog"/> 的新实例。
    /// </summary>
    /// <param name="serviceProvider">服务提供程序。</param>
    [GeneratedConstructor(InitializeComponent = true)]
    public partial GameShellUriInputDialog(IServiceProvider serviceProvider);

    /// <summary>
    /// 异步显示对话框并获取用户输入的 Shell URI。
    /// </summary>
    /// <returns>
    /// 如果用户确认且输入了有效的 Shell URI，则返回 <c>(true, uri)</c>；
    /// 否则返回 <c>(false, default)</c>。
    /// </returns>
    public async ValueTask<ValueResult<bool, string>> GetShellUriAsync()
    {
        ContentDialogResult result = await contentDialogFactory.EnqueueAndShowAsync(this).ShowTask.ConfigureAwait(false);
        await contentDialogFactory.TaskContext.SwitchToMainThreadAsync();

        string? uri = ShellUri;
        if (result is ContentDialogResult.Primary && !string.IsNullOrWhiteSpace(uri))
        {
            string trimmed = uri.Trim();
            if (IsValidShellUri(trimmed))
            {
                return new(true, trimmed);
            }
        }

        return new(false, default!);
    }

    /// <summary>
    /// 判断指定的字符串是否为有效的 Shell URI。
    /// </summary>
    /// <param name="uri">待验证的 URI 字符串。</param>
    /// <returns>如果以 <c>shell:</c> 或 <c>ms-</c> 开头则返回 <c>true</c>；否则返回 <c>false</c>。</returns>
    private static bool IsValidShellUri(string uri)
    {
        return uri.StartsWith("shell:", StringComparison.OrdinalIgnoreCase)
            || uri.StartsWith("ms-", StringComparison.OrdinalIgnoreCase);
    }
}
