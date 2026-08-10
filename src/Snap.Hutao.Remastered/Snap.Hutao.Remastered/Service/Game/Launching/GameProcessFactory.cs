// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core;
using Snap.Hutao.Remastered.Core.Diagnostics;
using Snap.Hutao.Remastered.Factory.Process;
using Snap.Hutao.Remastered.Service.Game.FileSystem;
using Snap.Hutao.Remastered.Service.Game.Launching.Context;
using Snap.Hutao.Remastered.Service.Game.PathAbstraction;

namespace Snap.Hutao.Remastered.Service.Game.Launching;

public sealed class GameProcessFactory
{
    public static IProcess CreateForDefault(BeforeLaunchExecutionContext context)
    {
        LaunchOptions launchOptions = context.LaunchOptions;

        string commandLine = string.Empty;
        if (launchOptions.AreCommandLineArgumentsEnabled.Value)
        {
            string? authTicket = default;
            bool useAuthTicket = launchOptions.UsingHoyolabAccount.Value
                && context.TryGetOption(LaunchExecutionOptionsKey.LoginAuthTicket, out authTicket)
                && !string.IsNullOrEmpty(authTicket);

            // https://docs.unity.cn/cn/current/Manual/PlayerCommandLineArguments.html
            // https://docs.unity3d.com/2017.4/Documentation/Manual/CommandLineArguments.html
            commandLine = new CommandLineBuilder()
                .AppendIf(launchOptions.IsBorderless.Value, "-popupwindow")
                .AppendIf(launchOptions.IsExclusive.Value, "-window-mode", "exclusive")
                .Append("-screen-fullscreen", launchOptions.IsFullScreen.Value ? "1" : "0")
                .AppendIf(launchOptions.IsScreenWidthEnabled.Value, "-screen-width", launchOptions.ScreenWidth.Value)
                .AppendIf(launchOptions.IsScreenHeightEnabled.Value, "-screen-height", launchOptions.ScreenHeight.Value)
                .AppendIf(launchOptions.IsMonitorEnabled.Value, "-monitor", launchOptions.Monitor.Value?.Value ?? 1)
                .AppendIf(launchOptions.IsPlatformTypeEnabled.Value, "-platform_type", $"{launchOptions.PlatformType.Value:G}")
                .AppendIf(useAuthTicket, "login_auth_ticket", authTicket, CommandLineArgumentPrefix.Equal)
                .ToString();

            context.TaskContext.InvokeOnMainThread(() =>
            {
                launchOptions.AspectRatios.Add(new(launchOptions.ScreenWidth.Value, launchOptions.ScreenHeight.Value));
            });
        }

        string gameFilePath = context.FileSystem.GameFilePath;

        // Shell URI launch (packaged apps, ms-windows-store, etc.)
        // Command-line arguments and Island injection are not supported for shell URIs.
        if (IsShellUri(gameFilePath))
        {
            return ProcessFactory.CreateUsingShellExecute(string.Empty, gameFilePath, string.Empty);
        }

        string gameDirectory = context.FileSystem.GameDirectory;

        return launchOptions.IsIslandEnabled.Value
            ? ProcessFactory.CreateUsingFullTrustSuspended(commandLine, gameFilePath, gameDirectory)
            : ProcessFactory.CreateUsingShellExecuteRunAs(commandLine, gameFilePath, gameDirectory);
    }

    public static IProcess CreateForEmbeddedYae(BeforeLaunchExecutionContext context)
    {
        LaunchOptions launchOptions = context.LaunchOptions;

        string? authTicket = default;
        bool useAuthTicket = launchOptions.AreCommandLineArgumentsEnabled.Value
            && launchOptions.UsingHoyolabAccount.Value
            && context.TryGetOption(LaunchExecutionOptionsKey.LoginAuthTicket, out authTicket)
            && !string.IsNullOrEmpty(authTicket);

        string commandLine = new CommandLineBuilder()
            .Append("-screen-fullscreen", 0)
            .Append("-screen-width", 800)
            .Append("-screen-height", 450)
            .AppendIf(useAuthTicket, "login_auth_ticket", authTicket, CommandLineArgumentPrefix.Equal)
            .ToString();

        return ProcessFactory.CreateSuspended(commandLine, context.FileSystem.GameFilePath, context.FileSystem.GameDirectory);
    }

    /// <summary>
    /// 判断指定的路径是否为 Shell URI（用于启动打包应用或通过协议启动）。
    /// </summary>
    /// <param name="path">待判断的路径或 URI。</param>
    /// <returns>如果以 <c>shell:</c> 或 <c>ms-</c> 开头则返回 <c>true</c>；否则返回 <c>false</c>。</returns>
    private static bool IsShellUri(string path)
    {
        return path.StartsWith("shell:", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("ms-", StringComparison.OrdinalIgnoreCase);
    }
}
