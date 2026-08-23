// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Google.Protobuf;
using Snap.Hutao.Remastered.Core.Protobuf;
using System.Collections.Immutable;

namespace Snap.Hutao.Remastered.Service.Yae.AvatarData;

public static class AvatarDataParser
{
    // 只解析 repeated AvatarInfo avatar_list，其余字段一律丢弃。
    // 由于每个版本 avatar_list 的字段号会变，这里不写死字段号，
    // 而是对所有顶层 wire=2 字段按元素"是否像 AvatarInfo"打分，取最高者。
    public static ImmutableArray<AvatarInfo> Parse(ByteString bytes)
    {
        Dictionary<uint, List<ByteString>> candidates = [];
        try
        {
            using (CodedInputStream stream = bytes.CreateCodedInput())
            {
                while (stream.TryReadTag(out uint tag))
                {
                    switch (WireFormat.GetTagWireType(tag))
                    {
                        case WireFormat.WireType.Varint:
                            _ = stream.ReadUInt64();
                            break;
                        case WireFormat.WireType.Fixed64:
                            _ = stream.ReadFixed64();
                            break;
                        case WireFormat.WireType.LengthDelimited:
                            {
                                uint field = (uint)WireFormat.GetTagFieldNumber(tag);
                                ByteString element = stream.ReadBytes();
                                if (!candidates.TryGetValue(field, out List<ByteString>? list))
                                {
                                    candidates[field] = list = [];
                                }

                                list.Add(element);
                                break;
                            }

                        case WireFormat.WireType.Fixed32:
                            _ = stream.ReadFixed32();
                            break;
                        default:
                            return [];
                    }
                }
            }
        }
        catch (InvalidProtocolBufferException)
        {
            return [];
        }

        uint bestField = 0;
        int bestScore = 0;
        foreach ((uint field, List<ByteString> elements) in candidates)
        {
            int score = elements.Count(LooksLikeAvatarInfo);
            if (score > bestScore)
            {
                bestScore = score;
                bestField = field;
            }
        }

        if (bestField is 0)
        {
            return [];
        }

        try
        {
            return [.. candidates[bestField].Select(AvatarInfo.Parser.ParseFrom)];
        }
        catch (InvalidProtocolBufferException)
        {
            return [];
        }
    }

    // AvatarInfo 内部签名：字段 1/2 都是 varint（avatar_id / guid，历代稳定），
    // 且有 >= 4 个去重字段。map 项 / rename 项只有 1~2 个字段，会被排除。
    private static bool LooksLikeAvatarInfo(ByteString element)
    {
        try
        {
            HashSet<uint> seen = [];
            bool hasField1 = false;
            bool hasField2 = false;
            using (CodedInputStream stream = element.CreateCodedInput())
            {
                while (stream.TryReadTag(out uint tag))
                {
                    uint field = (uint)WireFormat.GetTagFieldNumber(tag);
                    _ = seen.Add(field);
                    switch (WireFormat.GetTagWireType(tag))
                    {
                        case WireFormat.WireType.Varint:
                            _ = stream.ReadUInt64();
                            if (field is 1)
                            {
                                hasField1 = true;
                            }
                            else if (field is 2)
                            {
                                hasField2 = true;
                            }

                            break;
                        case WireFormat.WireType.Fixed64:
                            _ = stream.ReadFixed64();
                            break;
                        case WireFormat.WireType.LengthDelimited:
                            _ = stream.ReadLength();
                            break;
                        case WireFormat.WireType.Fixed32:
                            _ = stream.ReadFixed32();
                            break;
                        default:
                            return false;
                    }
                }
            }

            return hasField1 && hasField2 && seen.Count >= 4;
        }
        catch (InvalidProtocolBufferException)
        {
            return false;
        }
    }
}
