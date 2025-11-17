// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using System.IO;

namespace Snap.Hutao.Extension;

internal static class DirectoryExtension
{
    extension(Directory)
    {
        public static void SetReadOnly(string path, bool isReadOnly)
        {
            DirectoryInfo dirInfo = new(path);
            dirInfo.Attributes = isReadOnly
                ? dirInfo.Attributes | FileAttributes.ReadOnly
                : dirInfo.Attributes & ~FileAttributes.ReadOnly;

            foreach (FileInfo fileInfo in dirInfo.GetFiles())
            {
                fileInfo.Attributes = isReadOnly
                    ? fileInfo.Attributes | FileAttributes.ReadOnly
                    : fileInfo.Attributes & ~FileAttributes.ReadOnly;
            }

            foreach (DirectoryInfo subDirInfo in dirInfo.GetDirectories())
            {
                SetReadOnly(subDirInfo.FullName, isReadOnly);
            }
        }
    }
}