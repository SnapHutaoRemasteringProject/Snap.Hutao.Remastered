// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using ProtoBuf;
using ProtoBuf.Meta;
using Snap.Hutao.Remastered.Service.Metadata.Provider.Proto;
using System.Collections.Immutable;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;

namespace Snap.Hutao.Remastered.Test.Metadata;

[ProtoContract]
public sealed class CodecTestBundle
{
    [ProtoMember(1)]
    public CodecTestVersion? Version { get; set; }

    [ProtoMember(2)]
    public CodecTestInfo? Info { get; set; }

    [ProtoMember(3)]
    public ImmutableArray<CodecTestLanguage> Languages { get; set; }
}

[ProtoContract]
public sealed class CodecTestVersion
{
    [ProtoMember(1)]
    public int Major { get; set; }

    [ProtoMember(2)]
    public int Minor { get; set; }
}

[ProtoContract]
public sealed class CodecTestInfo
{
    [ProtoMember(1)]
    public string Publisher { get; set; } = string.Empty;

    [ProtoMember(2)]
    public string GeneratedAt { get; set; } = string.Empty;
}

[ProtoContract]
public sealed class CodecTestLanguage
{
    [ProtoMember(1)]
    public string Language { get; set; } = string.Empty;

    [ProtoMember(2)]
    public ImmutableArray<CodecTestAchievement> Achievements { get; set; }

    [ProtoMember(26)]
    public ImmutableArray<CodecTestTowerLevel> TowerLevels { get; set; }
}

[ProtoContract]
public sealed class CodecTestAchievement
{
    [ProtoMember(1)]
    public uint Id { get; set; }

    [ProtoMember(5)]
    public string Title { get; set; } = string.Empty;

    [ProtoMember(11)]
    public string Version { get; set; } = string.Empty;
}

[ProtoContract]
public sealed class CodecTestTowerLevel
{
    [ProtoMember(1)]
    public uint Id { get; set; }

    [ProtoMember(2)]
    public uint GroupId { get; set; }

    [ProtoMember(3)]
    public uint Index { get; set; }

    [ProtoMember(4)]
    public uint MonsterLevel { get; set; }
}

[ProtoModel]
[ProtoSerializable(typeof(CodecTestBundle))]
public partial class CodecTestModel : TypeModel
{
}

[TestClass]
public sealed class MetadataBundleCodecTest
{
    [TestMethod]
    public void ReadRoundTripsEncryptedBundle()
    {
        CodecTestBundle bundle = new()
        {
            Version = new CodecTestVersion { Major = 7, Minor = 2 },
            Info = new CodecTestInfo { Publisher = "test", GeneratedAt = "2026-11-21T15:00:24Z" },
            Languages =
            [
                new CodecTestLanguage
                {
                    Language = "CHS",
                    Achievements = [new CodecTestAchievement { Id = 1, Title = "title", Version = "1.0" }],
                    TowerLevels = [new CodecTestTowerLevel { Id = 4, GroupId = 5, Index = 6, MonsterLevel = 7 }],
                },
            ],
        };

        using MemoryStream serialized = new();
        CodecTestModel.Instance.Serialize(serialized, bundle);
        byte[] payload = Encrypt(serialized.ToArray());

        using MemoryStream stream = new(payload);
        CodecTestBundle parsed = MetadataBundleCodec.Read<CodecTestBundle>(stream, CodecTestModel.Instance);

        Assert.AreEqual(7, parsed.Version!.Major);
        Assert.AreEqual(2, parsed.Version.Minor);
        Assert.AreEqual("test", parsed.Info!.Publisher);
        Assert.AreEqual(1, parsed.Languages.Length);
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
            MetadataBundleCodec.Read<CodecTestBundle>(stream, CodecTestModel.Instance);
        });
    }

    [TestMethod]
    public void ReadRejectsCorruptCiphertext()
    {
        Assert.ThrowsExactly<Exception>(() =>
        {
            using MemoryStream stream = new(new byte[64]);
            MetadataBundleCodec.Read<CodecTestBundle>(stream, CodecTestModel.Instance);
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
