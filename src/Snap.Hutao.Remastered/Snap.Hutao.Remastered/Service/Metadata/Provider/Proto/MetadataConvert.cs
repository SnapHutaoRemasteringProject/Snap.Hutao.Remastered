// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Model.Intrinsic;
using Snap.Hutao.Remastered.Model.Metadata;
using Snap.Hutao.Remastered.Model.Metadata.Weapon;
using System.Collections.Concurrent;
using System.Globalization;
using ProtoFightProperty = Snap.Hutao.Remastered.Metadata.FightProperty;
using ProtoGrowCurvePoint = Snap.Hutao.Remastered.Metadata.GrowCurvePoint;
using ProtoGrowCurveValue = Snap.Hutao.Remastered.Metadata.GrowCurveValue;
using ProtoWeaponGrowCurveData = Snap.Hutao.Remastered.Metadata.WeaponGrowCurveData;

namespace Snap.Hutao.Remastered.Service.Metadata.Provider.Proto;

/// <summary>
/// Central conversions from proto fields to application model values, including the failure policy.
/// </summary>
/// <remarks>
/// <para>
/// Undefined enum numbers are <b>preserved</b> and reported once instead of being collapsed to the
/// enum's zero member. The JSON source (System.Text.Json) also preserves the raw number, and the
/// data already relies on it (<c>Chapter.CityId</c> 105, <c>ProfilePicture.UnlockType</c> 5), so
/// collapsing would make the two sources disagree for entries the UI already handles.
/// </para>
/// <para>
/// A value the model cannot represent (an id that overflows <see cref="uint"/>, an unparsable
/// required <see cref="Uri"/>) throws <see cref="MetadataEntrySkippedException"/>: the adapter skips
/// that entry and keeps the category usable.
/// </para>
/// </remarks>
internal sealed class MetadataConvert
{
    private readonly ILogger logger;
    private readonly ConcurrentDictionary<string, byte> reported = new(StringComparer.Ordinal);

    public MetadataConvert(ILogger logger)
    {
        this.logger = logger;
    }

    public string Text(string? value)
    {
        return value is null ? string.Empty : string.Intern(value);
    }

    /// <summary>
    /// proto3 cannot distinguish an absent string from an empty one. Nullable model properties map
    /// empty to null, matching the JSON source where the producer omits null values.
    /// </summary>
    public string? OptionalText(string? value)
    {
        return string.IsNullOrEmpty(value) ? null : string.Intern(value);
    }

    public TEnum ToEnum<TEnum>(int value, string field)
        where TEnum : struct, Enum
    {
        TEnum candidate = (TEnum)System.Enum.ToObject(typeof(TEnum), Convert.ChangeType(value, System.Enum.GetUnderlyingType(typeof(TEnum)), CultureInfo.InvariantCulture));
        if (!System.Enum.IsDefined(candidate))
        {
            WarnOnce($"{typeof(TEnum).Name}.{field}.{value}", $"Undefined {typeof(TEnum).Name} value {value} in '{field}'");
        }

        return candidate;
    }

    public TEnum ToEnum<TProtoEnum, TEnum>(TProtoEnum value, string field)
        where TProtoEnum : struct, Enum
        where TEnum : struct, Enum
    {
        return ToEnum<TEnum>(Convert.ToInt32(value, CultureInfo.InvariantCulture), field);
    }

    /// <summary>
    /// proto uses bare <c>uint32</c> for fields whose application type is an enum; the numeric value is
    /// preserved and checked against the enum definition domain.
    /// </summary>
    public TEnum RawEnum<TEnum>(uint value, string field)
        where TEnum : struct, Enum
    {
        if (value > int.MaxValue)
        {
            throw new MetadataEntrySkippedException(field, $"'{field}' value {value} does not fit in an enum");
        }

        return ToEnum<TEnum>((int)value, field);
    }

    public float Single(double value)
    {
        return (float)value;
    }

    public DateTimeOffset Time(string? value, string field)
    {
        if (string.IsNullOrEmpty(value))
        {
            return default;
        }

        if (DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTimeOffset result))
        {
            return result;
        }

        WarnOnce($"{field}.{value}", $"'{field}' value '{value}' is not ISO 8601; using the default");
        return default;
    }

    public Uri RequiredUri(string? value, string field)
    {
        return Uri.TryCreate(value, UriKind.Absolute, out Uri? uri)
            ? uri
            : throw new MetadataEntrySkippedException(field, $"'{field}' value '{value}' is not an absolute URI");
    }

    public Uri? OptionalUri(string? value, string field)
    {
        if (string.IsNullOrEmpty(value))
        {
            return null;
        }

        if (Uri.TryCreate(value, UriKind.Absolute, out Uri? uri))
        {
            return uri;
        }

        WarnOnce($"{field}.{value}", $"'{field}' value '{value}' is not an absolute URI; dropping it");
        return null;
    }

    public void WarnOnce(string key, string message)
    {
        if (reported.TryAdd(key, 0))
        {
            logger.LogWarning("{Message}", message);
        }
    }

    public TypeValueCollection<FightProperty, GrowCurveType> FightGrowCurveCollection(IEnumerable<(ProtoFightProperty Type, ProtoGrowCurveValue Value)> values, string field)
    {
        TypeValue<FightProperty, GrowCurveType>[] array =
        [
            .. values.Select(value => new TypeValue<FightProperty, GrowCurveType>(
                ToEnum<ProtoFightProperty, FightProperty>(value.Type, field),
                ToEnum<ProtoGrowCurveValue, GrowCurveType>(value.Value, field))),
        ];

        return new([.. array]);
    }

    public TypeValueCollection<FightProperty, float> FightPropertyCollection(IEnumerable<(ProtoFightProperty Type, double Value)> values, string field)
    {
        TypeValue<FightProperty, float>[] array =
        [
            .. values.Select(value => new TypeValue<FightProperty, float>(
                ToEnum<ProtoFightProperty, FightProperty>(value.Type, field),
                Single(value.Value))),
        ];

        return new([.. array]);
    }

    public TypeValueCollection<FightProperty, float> RawFightPropertyCollection(IEnumerable<(uint Type, double Value)> values, string field)
    {
        TypeValue<FightProperty, float>[] array =
        [
            .. values.Select(value => new TypeValue<FightProperty, float>(
                RawEnum<FightProperty>(value.Type, field),
                Single(value.Value))),
        ];

        return new([.. array]);
    }

    public TypeValueCollection<GrowCurveType, float> GrowCurveCollection(IEnumerable<ProtoGrowCurvePoint> points, string field)
    {
        TypeValue<GrowCurveType, float>[] array =
        [
            .. points.Select(point => new TypeValue<GrowCurveType, float>(
                ToEnum<ProtoGrowCurveValue, GrowCurveType>(point.Type, field),
                Single(point.Value))),
        ];

        return new([.. array]);
    }

    public WeaponTypeValueCollection WeaponGrowCurveCollection(IEnumerable<ProtoWeaponGrowCurveData> values, string field)
    {
        WeaponTypeValue[] array =
        [
            .. values.Select(value => new WeaponTypeValue(
                ToEnum<ProtoFightProperty, FightProperty>(value.Type, field),
                ToEnum<ProtoGrowCurveValue, GrowCurveType>(value.Value, field),
                Single(value.InitValue))),
        ];

        return new([.. array]);
    }
}
