// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Metadata;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;

namespace Snap.Hutao.Remastered.Service.Metadata.Provider.Proto;

internal static class MetadataBundleCodec
{
    internal const string AesKeyHex = "f2ca5fcad3cffec1bf94a817366ddf076a88b40f324cebd71e4c2d58ef5786e1";

    internal const int IvLength = 16;

    /// <summary>
    /// 256 MiB. Far above any real bundle, low enough that a decompression bomb cannot exhaust memory.
    /// </summary>
    internal const int MaxDecompressedBytes = 256 * 1024 * 1024;

    private const int MinimumEncryptedLength = IvLength + IvLength;

    internal static byte[] AesKey { get; } = Convert.FromHexString(AesKeyHex);

    public static MetadataBundle Read(string path, CancellationToken token = default)
    {
        using FileStream stream = File.OpenRead(path);
        return Read(stream, token);
    }

    public static MetadataBundle Read(Stream stream, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        if (!stream.CanRead)
        {
            throw new InvalidDataException("Metadata bundle stream is not readable.");
        }

        if (stream.CanSeek && stream.Length is < MinimumEncryptedLength or > MetadataBundleConstants.MaxEncryptedBytes)
        {
            throw new InvalidDataException($"Metadata bundle has an invalid length of {stream.Length} bytes.");
        }

        Span<byte> iv = stackalloc byte[IvLength];
        stream.ReadExactly(iv);

        using Aes aes = Aes.Create();
        aes.Key = AesKey;
        aes.IV = iv.ToArray();
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using CryptoStream crypto = new(stream, aes.CreateDecryptor(), CryptoStreamMode.Read, leaveOpen: true);
        using ZLibStream zlib = new(crypto, CompressionMode.Decompress, leaveOpen: true);
        using BoundedReadStream bounded = new(zlib, MaxDecompressedBytes);

        return MetadataBundle.Parser.ParseFrom(bounded);
    }
}
