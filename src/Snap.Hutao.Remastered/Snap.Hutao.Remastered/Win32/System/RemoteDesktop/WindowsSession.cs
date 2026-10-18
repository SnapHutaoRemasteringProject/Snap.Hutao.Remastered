// Copyright (c) Snap Hutao RP. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Win32.Foundation;
using System.Runtime.InteropServices;

namespace Snap.Hutao.Remastered.Win32.System.RemoteDesktop;

public static partial class WindowsSession
{
    public static BOOL RegisterNotifications(HWND window) => WTSRegisterSessionNotification(window, 0); // NOTIFY_FOR_THIS_SESSION

    public static void UnregisterNotifications(HWND window) => WTSUnRegisterSessionNotification(window);

    public static unsafe bool? IsCurrentSessionLocked()
    {
        // WTS_CURRENT_SERVER_HANDLE, WTS_CURRENT_SESSION, WTSSessionInfoEx.
        if (!WTSQuerySessionInformation(0, uint.MaxValue, 25, out nint buffer, out uint size)) return null;
        try
        {
            if (size < sizeof(SessionInfoEx) || ((SessionInfoEx*)buffer)->Level is not 1) return null;
            return ((SessionInfoEx*)buffer)->Data.SessionFlags switch
            {
                0 => true, // WTS_SESSIONSTATE_LOCK on the supported Windows 10/11 versions.
                1 => false,
                _ => null,
            };
        }
        finally
        {
            WTSFreeMemory(buffer);
        }
    }

    [LibraryImport("wtsapi32.dll", SetLastError = true)]
    private static partial BOOL WTSRegisterSessionNotification(HWND window, uint flags);

    [LibraryImport("wtsapi32.dll", SetLastError = true)]
    private static partial BOOL WTSUnRegisterSessionNotification(HWND window);

    [LibraryImport("wtsapi32.dll", EntryPoint = "WTSQuerySessionInformationW", SetLastError = true)]
    private static partial BOOL WTSQuerySessionInformation(nint server, uint sessionId, int informationClass, out nint buffer, out uint bytesReturned);

    [LibraryImport("wtsapi32.dll")]
    private static partial void WTSFreeMemory(nint buffer);

    // Preserve the SDK layout, including the union's 8-byte alignment. A header
    // containing only SessionId/State/Flags would incorrectly align Data to 4.
#pragma warning disable CS0649 // These fields are populated by WTSQuerySessionInformationW.
    [StructLayout(LayoutKind.Sequential)]
    private struct SessionInfoEx
    {
        public uint Level;
        public SessionInfoExLevel1 Data;
    }

    [StructLayout(LayoutKind.Sequential)]
    private unsafe struct SessionInfoExLevel1
    {
        public uint SessionId;
        public int SessionState;
        public int SessionFlags;
        public fixed char WinStationName[33];
        public fixed char UserName[21];
        public fixed char DomainName[18];
        public long LogonTime;
        public long ConnectTime;
        public long DisconnectTime;
        public long LastInputTime;
        public long CurrentTime;
        public uint IncomingBytes;
        public uint OutgoingBytes;
        public uint IncomingFrames;
        public uint OutgoingFrames;
        public uint IncomingCompressedBytes;
        public uint OutgoingCompressedBytes;
    }
#pragma warning restore CS0649
}
