// Copyright (c) Snap Hutao RP. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.Core.AI.Threading;

internal sealed class AgentThreadingContext
{
    public static AgentThreadingContext Current { get; private set; } = null!;

    private AgentThreadingContext()
    {
        SynchronizationContext = System.Threading.SynchronizationContext.Current ?? throw new InvalidOperationException("An agent workspace must be initialized on the UI thread.");
        TaskScheduler = System.Threading.Tasks.TaskScheduler.FromCurrentSynchronizationContext();
    }

    public SynchronizationContext SynchronizationContext { get; }

    public TaskScheduler TaskScheduler { get; }

    public static void Initialize()
    {
        Current ??= new();
    }
}
