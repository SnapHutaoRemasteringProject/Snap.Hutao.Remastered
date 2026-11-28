// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.Model.Metadata;

[ProtoContract]
public class TypeValue<TType, TValue>
{
    public TypeValue()
    {
    }

    public TypeValue(TType type, TValue value)
    {
        Type = type;
        Value = value;
    }

    [ProtoMember(1, IsRequired = true)]
    public TType Type { get; set; } = default!;

    [ProtoMember(2, IsRequired = true)]
    public TValue Value { get; set; } = default!;

    public override bool Equals(object? obj)
    {
        return obj is TypeValue<TType, TValue> value
            && EqualityComparer<TType>.Default.Equals(Type, value.Type)
            && EqualityComparer<TValue>.Default.Equals(Value, value.Value);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Type, Value);
    }
}
