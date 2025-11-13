// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using JetBrains.Annotations;
using Snap.Hutao.Model;
using System.Collections.Immutable;

namespace Snap.Hutao.Core.Property;

internal static class Property
{
    public static IProperty<T> Create<T>(T value)
    {
        return new Property<T>(value);
    }

    public static IObservableProperty<T> CreateObservable<T>(T value)
    {
        return new ObservableProperty<T>(value);
    }

    public static IReadOnlyObservableProperty<T> Observe<TSource, T>(IObservableProperty<TSource> source, Func<TSource, T> converter)
    {
        return new ObservablePropertyObserver<TSource, T>(source, converter);
    }

    extension<T>(IProperty<T> property)
    {
        public T Get()
        {
            return property.Value;
        }

        public T Set(T value)
        {
            return property.Value = value;
        }
    }

    extension(IProperty<bool> source)
    {
        public IProperty<bool> Negate()
        {
            return new BooleanPropertyNegation(source);
        }
    }

    extension<T>(IObservableProperty<T> property)
    {
        public IObservableProperty<T> Debug(string name)
        {
            return new ObservablePropertyDebug<T>(property, name);
        }

        public IReadOnlyObservableProperty<T> AsReadOnly()
        {
            return new ObservablePropertyReadOnlyWrapper<T>(property);
        }

        public IObservableProperty<T> Link<TTarget>(IProperty<TTarget> target, [RequireStaticDelegate] Action<T, IProperty<TTarget>> callback)
        {
            return new ObservablePropertyValueChangedCallbackWrapper<T, IProperty<TTarget>>(property, callback, target);
        }

        public IObservableProperty<T> SetWithCondition<TState>(Func<T, TState, bool> condition, TState state)
        {
            return new ObservablePropertyWithConditionalSetMethod<T, TState>(property, condition, state);
        }

        public IObservableProperty<T> WithValueChangedCallback([RequireStaticDelegate] Action<T> callback)
        {
            return new ObservablePropertyValueChangedCallbackWrapper<T>(property, callback);
        }

        public IObservableProperty<T> WithValueChangedCallback<TState>([RequireStaticDelegate] Action<T, TState> callback, TState state)
        {
            return new ObservablePropertyValueChangedCallbackWrapper<T, TState>(property, callback, state);
        }
    }

    extension<T>(IObservableProperty<T> property)
        where T : notnull
    {
        public IObservableProperty<NameValue<T>?> AsNameValue(ImmutableArray<NameValue<T>> array)
        {
            return new ObservablePropertyNameValueWrapper<T>(property, array);
        }
    }

    extension(IObservableProperty<bool> source)
    {
        public IObservableProperty<bool> AlsoSetFalseWhenFalse(IProperty<bool> target)
        {
            return Link(source, target, static (value, target) =>
            {
                if (!value)
                {
                    target.Value = false;
                }
            });
        }
    }

    extension<T>(IProperty<T> source)
        where T : notnull
    {
        public IObservableProperty<TResult?> AsNullableSelection<TResult>(ImmutableArray<TResult> array, Func<TResult?, T> valueSelector, IEqualityComparer<T> equalityComparer)
            where TResult : class
        {
            return new PropertyNullableSelectionWrapper<TResult, T>(source, array, valueSelector, equalityComparer);
        }
    }
}

[SuppressMessage("", "SA1402")]
internal sealed class Property<T> : IProperty<T>
{
    public Property(T value)
    {
        Value = value;
    }

    public T Value { get; set; }
}