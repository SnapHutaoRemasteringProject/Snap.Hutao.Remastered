// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

#pragma warning disable CS8618

using Snap.Hutao.Remastered.Model.Intrinsic;
using System.Collections.Immutable;

namespace Snap.Hutao.Remastered.Model.Metadata.Avatar;

[ProtoContract]
public sealed class FetterInfo
{
    [ProtoMember(1)]
    public string Title { get; init; }

    [ProtoMember(2)]
    public string Detail { get; init; }

    [ProtoMember(3)]
    public AssociationType Association { get; init; }

    [ProtoMember(4)]
    public string Native { get; init; }

    [ProtoMember(5)]
    public uint BirthMonth { get; init; }

    [ProtoMember(6)]
    public uint BirthDay { get; init; }

    [JsonIgnore]
    public string BirthFormatted { get => SH.FormatModelMetadataFetterInfoBirthday(BirthMonth, BirthDay); }

    [ProtoMember(7)]
    public string VisionBefore { get; init; }

    [ProtoMember(8)]
    public string? VisionAfter { get; init; }

    public string Vision
    {
        get => string.IsNullOrEmpty(VisionAfter) ? VisionBefore : VisionAfter;
    }

    [ProtoMember(9)]
    public string? VisionOverrideLocked { get; init; }

    [ProtoMember(10)]
    public string? VisionOverrideUnlocked { get; init; }

    [field: MaybeNull]
    public string VisionOverride
    {
        get
        {
            if (!string.IsNullOrEmpty(field))
            {
                return field;
            }

            if (!string.IsNullOrEmpty(VisionOverrideUnlocked))
            {
                return field = VisionOverrideUnlocked;
            }

            if (!string.IsNullOrEmpty(VisionOverrideLocked))
            {
                return field = VisionOverrideLocked;
            }

            return field = SH.ViewPageWiKiAvatarVisionTitle;
        }
        set;
    }

    [ProtoMember(11)]
    public string ConstellationBefore { get; init; }

    [ProtoMember(12)]
    public string? ConstellationAfter { get; init; }

    [JsonIgnore]
    public string Constellation { get => string.IsNullOrEmpty(ConstellationAfter) ? ConstellationBefore : ConstellationAfter; }

    [ProtoMember(13)]
    public string CvChinese { get; init; }

    [ProtoMember(14)]
    public string CvJapanese { get; init; }

    [ProtoMember(15)]
    public string CvEnglish { get; init; }

    [ProtoMember(16)]
    public string CvKorean { get; init; }

    [ProtoMember(17)]
    public CookBonus? CookBonus { get; init; }

    [ProtoMember(18)]
    public ImmutableArray<Fetter> Fetters { get; init; } = [];

    [ProtoMember(19)]
    public ImmutableArray<Fetter> FetterStories { get; init; } = [];
}
