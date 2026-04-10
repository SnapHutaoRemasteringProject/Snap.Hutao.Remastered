// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.Service.Game.Package.Advanced;

public abstract class GamePackageOperationReport
{
    public GamePackageOperationReportKind Kind { get; private set; }

    public abstract class Update : GamePackageOperationReport
    {
        public Update(long bytesRead, int chunks, string fileName)
        {
            BytesRead = bytesRead;
            Chunks = chunks;
            FileName = fileName;
        }

        public long BytesRead { get; }

        public int Chunks { get; }

        public string FileName { get; }
    }

    public sealed class Download : Update
    {
        public Download(long bytesRead, int chunks, string fileName = default!)
            : base(bytesRead, chunks, fileName)
        {
            Kind = GamePackageOperationReportKind.Download;
        }
    }

    public sealed class Install : Update
    {
        public Install(long bytesRead, int chunks, string fileName = default!)
            : base(bytesRead, chunks, fileName)
        {
            Kind = GamePackageOperationReportKind.Install;
        }
    }

    public sealed class Reset : GamePackageOperationReport
    {
        public Reset(string title)
        {
            Kind = GamePackageOperationReportKind.Reset;
            Title = title;
        }

        public Reset(string title, int totalChunks, long contentLength)
        {
            Kind = GamePackageOperationReportKind.Reset;
            Title = title;
            DownloadTotalChunks = InstallTotalChunks = totalChunks;
            DownloadTotalBytes = InstallTotalBytes = contentLength;
        }

        public Reset(string title, int totalChunks, long downloadTotalBytes, long installTotalBytes)
        {
            Kind = GamePackageOperationReportKind.Reset;
            Title = title;
            DownloadTotalChunks = InstallTotalChunks = totalChunks;
            DownloadTotalBytes = downloadTotalBytes;
            InstallTotalBytes = installTotalBytes;
        }

        public Reset(string title, int downloadTotalChunks, int installTotalChunks, long contentLength)
        {
            Kind = GamePackageOperationReportKind.Reset;
            Title = title;
            DownloadTotalChunks = downloadTotalChunks;
            InstallTotalChunks = installTotalChunks;
            DownloadTotalBytes = InstallTotalBytes = contentLength;
        }

        public Reset(string title, int downloadTotalChunks, int installTotalChunks, long downloadTotalBytes, long installTotalBytes)
        {
            Kind = GamePackageOperationReportKind.Reset;
            Title = title;
            DownloadTotalChunks = downloadTotalChunks;
            InstallTotalChunks = installTotalChunks;
            DownloadTotalBytes = downloadTotalBytes;
            InstallTotalBytes = installTotalBytes;
        }

        public string Title { get; }

        public int DownloadTotalChunks { get; }

        public int InstallTotalChunks { get; }

        public long DownloadTotalBytes { get; }

        public long InstallTotalBytes { get; }
    }

    public sealed class Finish : GamePackageOperationReport
    {
        public Finish(GamePackageOperationKind kind, bool repaired = false)
        {
            Kind = GamePackageOperationReportKind.Finish;
            OperationKind = kind;
            Repaired = repaired;
        }

        public GamePackageOperationKind OperationKind { get; }

        public bool Repaired { get; }
    }

    public sealed class Abort : GamePackageOperationReport
    {
        public Abort(string reason)
        {
            Kind = GamePackageOperationReportKind.Abort;
            Reason = reason;
        }

        public string Reason { get; }
    }

    public sealed class RetryableFailure : GamePackageOperationReport
    {
        public RetryableFailure(string reason)
        {
            Kind = GamePackageOperationReportKind.RetryableFailure;
            Reason = reason;
        }

        public string Reason { get; }
    }
}