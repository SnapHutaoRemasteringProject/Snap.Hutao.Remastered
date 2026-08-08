// Copyright (c) Snap Hutao RP. All rights reserved.
// Licensed under the MIT license.

using System.Runtime.InteropServices;

namespace Snap.Hutao.Remastered.Service.FileUnlocker;

[Service(ServiceLifetime.Singleton, typeof(IFileUnlockerService))]
public sealed partial class FileUnlockerService : IFileUnlockerService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = false,
    };

    public ValueTask<LockUnlockResponse?> UnlockAsync(string directoryPath)
    {
        LockUnlockResponse? result = UnlockCore(directoryPath);
        return ValueTask.FromResult(result);
    }

    private static unsafe LockUnlockResponse? UnlockCore(string path)
    {
        char* pResult = null;
        try
        {
            fixed (char* pPath = path)
            {
                int hr = HutaoFileUnlockerInterop.HutaoFileUnlocker_UnlockFileLocks(pPath, false, &pResult);
                if (pResult == null)
                {
                    return null;
                }

                string json = Marshal.PtrToStringUni((nint)pResult) ?? string.Empty;
                return JsonSerializer.Deserialize<LockUnlockResponse>(json, JsonOptions);
            }
        }
        finally
        {
            if (pResult != null)
            {
                HutaoFileUnlockerInterop.HutaoFileUnlocker_FreeString(pResult);
            }
        }
    }
}
