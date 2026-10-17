// Copyright (c) Snap Hutao RP. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.Service.Game.Package.Advanced;

public readonly record struct PredownloadProgressSnapshot(bool Finished, int DownloadedBlocks, int TotalBlocks, int Percent)
{
    public static PredownloadProgressSnapshot FromBlocks(bool finished, int downloaded, int total)
    {
        total = Math.Max(total, 0);
        downloaded = Math.Clamp(downloaded, 0, total);
        int percent = total == 0 ? 0 : (int)((long)downloaded * 100 / total);
        return new(finished, downloaded, total, finished ? percent : Math.Min(percent, 99));
    }
}

public sealed class PredownloadProgressTracker
{
    private int totalBlocks;
    private int downloadedBlocks;
    private long totalBytes;
    private long downloadedBytes;
    private bool finished;

    public PredownloadProgressSnapshot Snapshot
    {
        get
        {
            int blocks = totalBlocks == 0 ? 0 : (int)((long)downloadedBlocks * 100 / totalBlocks);
            int bytes = totalBytes == 0 ? 0 : (int)(downloadedBytes / (double)totalBytes * 100);
            return new(finished, downloadedBlocks, totalBlocks, finished ? 100 : Math.Min(Math.Max(blocks, bytes), 99));
        }
    }

    public void Reset(int blocks, long bytes)
    {
        totalBlocks = Math.Max(blocks, 0);
        totalBytes = Math.Max(bytes, 0);
        downloadedBlocks = 0;
        downloadedBytes = 0;
        finished = false;
    }

    public void Download(long bytes, int blocks)
    {
        if (finished) return;
        downloadedBytes += Math.Min(Math.Max(bytes, 0), totalBytes - downloadedBytes);
        downloadedBlocks += Math.Min(Math.Max(blocks, 0), totalBlocks - downloadedBlocks);
    }

    public void Complete()
    {
        downloadedBlocks = totalBlocks;
        downloadedBytes = totalBytes;
        finished = true;
    }
}
