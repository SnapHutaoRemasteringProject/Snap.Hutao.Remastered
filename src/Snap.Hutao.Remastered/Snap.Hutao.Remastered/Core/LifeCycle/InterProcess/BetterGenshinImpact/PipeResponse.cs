// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.Core.LifeCycle.InterProcess.BetterGenshinImpact;

public class PipeResponse
{
    public required PipeResponseKind Kind { get; set; }

    public static PipeResponse CreateNone()
    {
        return new PipeResponse<Void>
        {
            Kind = PipeResponseKind.None,
            Data = default,
        };
    }
}

[SuppressMessage("", "SA1402")]
public sealed class PipeResponse<T> : PipeResponse
{
    public T? Data { get; set; }
}