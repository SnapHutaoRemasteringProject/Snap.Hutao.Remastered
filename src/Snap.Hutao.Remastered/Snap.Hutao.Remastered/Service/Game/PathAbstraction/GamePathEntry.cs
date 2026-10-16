// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;

namespace Snap.Hutao.Remastered.Service.Game.PathAbstraction;

public sealed partial class GamePathEntry : ObservableObject
{
    [JsonPropertyName("Path")]
    public string Path { get; init => SetProperty(ref field, value); } = default!;

    public static GamePathEntry Create(string path)
    {
        return new()
        {
            Path = path,
        };
    }

    public override string ToString()
    {
        return $"{{ Path = {Path} }}";
    }
}
