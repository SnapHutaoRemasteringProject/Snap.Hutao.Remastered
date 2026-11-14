// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.Model;

/// <summary>
/// 封装带有名称描述的值
/// 在绑定枚举变量时非常有用
/// https://github.com/microsoft/microsoft-ui-xaml/issues/4266
/// 直接绑定枚举变量会显示 Windows.Foundation.IReference{T}
/// </summary>
/// <typeparam name="T">包含值的类型</typeparam>
public partial class NameValue<T> : INamedValue, IEquatable<NameValue<T>>
{
    public NameValue(string name, T value)
    {
        Name = name;
        Value = value;
    }

    public string Name { get; }

    public T Value { get; }

    public override string ToString() => Name;

    public bool Equals(NameValue<T>? other)
    {
        return other is not null
            && string.Equals(Name, other.Name)
            && EqualityComparer<T>.Default.Equals(Value, other.Value);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Name, Value);
    }
}
