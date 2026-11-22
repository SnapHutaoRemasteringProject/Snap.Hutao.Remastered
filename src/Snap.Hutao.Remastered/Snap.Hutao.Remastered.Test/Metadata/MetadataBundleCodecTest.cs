// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Google.Protobuf;
using Snap.Hutao.Remastered.Metadata;
using Snap.Hutao.Remastered.Service.Metadata.Provider.Proto;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;

namespace Snap.Hutao.Remastered.Test.Metadata;

[TestClass]
public sealed class MetadataBundleCodecTest
{
    [TestMethod]
    public void ReadRoundTripsEncryptedBundle()
    {
        MetadataBundle bundle = new()
        {
            Version = new GameVersion { Major = 7, Minor = 2 },
            Info = new MetadataInfo { Publisher = "test", GeneratedAt = "2026-11-21T15:00:24Z" },
        };

        LanguageMetadata language = new() { Language = "CHS" };
        language.Achievements.Add(new Achievement
        {
            Id = 1,
            Goal = 2,
            Order = 3,
            Title = "title",
            Version = "1.0",
        });
        language.TowerLevels.Add(new TowerLevel
        {
            Id = 4,
            GroupId = 5,
            Index = 6,
            MonsterLevel = 7,
        });
        bundle.Languages.Add(language);

        byte[] payload = Encrypt(bundle.ToByteArray());

        using MemoryStream stream = new(payload);
        MetadataBundle parsed = MetadataBundleCodec.Read(stream);

        Assert.AreEqual(7, parsed.Version.Major);
        Assert.AreEqual(2, parsed.Version.Minor);
        Assert.AreEqual("test", parsed.Info.Publisher);
        Assert.AreEqual(1, parsed.Languages.Count);
        Assert.AreEqual("CHS", parsed.Languages[0].Language);
        Assert.AreEqual(1U, parsed.Languages[0].Achievements[0].Id);
        Assert.AreEqual("title", parsed.Languages[0].Achievements[0].Title);
        Assert.AreEqual(4U, parsed.Languages[0].TowerLevels[0].Id);
        Assert.AreEqual(7U, parsed.Languages[0].TowerLevels[0].MonsterLevel);
    }

    [TestMethod]
    public void ReadRejectsBundleShorterThanAnIv()
    {
        Assert.ThrowsExactly<InvalidDataException>(() =>
        {
            using MemoryStream stream = new(new byte[8]);
            MetadataBundleCodec.Read(stream);
        });
    }

    [TestMethod]
    public void ReadRejectsCorruptCiphertext()
    {
        Assert.Throws<Exception>(() =>
        {
            using MemoryStream stream = new(new byte[64]);
            MetadataBundleCodec.Read(stream);
        });
    }

    [TestMethod]
    public void BoundedReadStreamRejectsOversizedPayload()
    {
        Assert.ThrowsExactly<InvalidDataException>(() =>
        {
            using MemoryStream inner = new(new byte[32]);
            using BoundedReadStream bounded = new(inner, 16);
            byte[] buffer = new byte[32];
            bounded.ReadExactly(buffer);
        });
    }

    private static byte[] Encrypt(byte[] protobuf)
    {
        byte[] compressed;
        using (MemoryStream buffer = new())
        {
            using (ZLibStream zlib = new(buffer, CompressionLevel.SmallestSize, leaveOpen: true))
            {
                zlib.Write(protobuf);
            }

            compressed = buffer.ToArray();
        }

        byte[] iv = RandomNumberGenerator.GetBytes(MetadataBundleCodec.IvLength);
        using Aes aes = Aes.Create();
        aes.Key = Convert.FromHexString(MetadataBundleCodec.AesKeyHex);
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using MemoryStream output = new();
        output.Write(iv);
        using (CryptoStream crypto = new(output, aes.CreateEncryptor(), CryptoStreamMode.Write, leaveOpen: true))
        {
            crypto.Write(compressed);
        }

        return output.ToArray();
    }
}
