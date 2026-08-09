// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.Service.Game.PathAbstraction;

/// <summary>
/// 表示游戏的启动方式。
/// </summary>
public enum GameLaunchMethod
{
    /// <summary>
    /// 通过传统的可执行文件（.exe）路径启动。
    /// </summary>
    Executable,

    /// <summary>
    /// 通过 Shell URI（如 shell:AppsFolder\... 或 ms-windows-store://...）启动打包应用。
    /// </summary>
    ShellUri,
}
