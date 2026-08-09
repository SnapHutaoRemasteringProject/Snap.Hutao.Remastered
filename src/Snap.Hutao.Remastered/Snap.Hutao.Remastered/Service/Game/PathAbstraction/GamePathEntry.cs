// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.Service.Game.PathAbstraction;

/// <summary>
/// 表示一个游戏路径条目，包含路径和启动方式。
/// </summary>
public sealed class GamePathEntry
{
    /// <summary>
    /// 游戏路径。对于传统启动方式，这是可执行文件（.exe）的完整路径；
    /// 对于 Shell URI 启动方式，这是 shell: 或 ms- 协议 URI。
    /// </summary>
    [JsonPropertyName("Path")]
    public string Path { get; init; } = default!;

    /// <summary>
    /// 该路径对应的启动方式。
    /// </summary>
    [JsonPropertyName("LaunchMethod")]
    public GameLaunchMethod LaunchMethod { get; init; } = GameLaunchMethod.Executable;

    /// <summary>
    /// 创建一个新的游戏路径条目。
    /// </summary>
    /// <param name="path">游戏路径或 Shell URI。</param>
    /// <param name="method">启动方式，默认为 <see cref="GameLaunchMethod.Executable"/>。</param>
    /// <returns>新创建的 <see cref="GamePathEntry"/> 实例。</returns>
    public static GamePathEntry Create(string path, GameLaunchMethod method = GameLaunchMethod.Executable)
    {
        return new()
        {
            Path = path,
            LaunchMethod = method,
        };
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"{{ Path = {Path}, LaunchMethod = {LaunchMethod} }}";
    }
}
