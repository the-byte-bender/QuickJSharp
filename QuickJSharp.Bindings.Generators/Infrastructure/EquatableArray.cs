using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using QuickJSharp.Bindings.Generators.Models;

namespace QuickJSharp.Bindings.Generators.Infrastructure;

public readonly struct EquatableArray<T> : IEquatable<EquatableArray<T>>, IEnumerable<T>
    where T : IEquatable<T>
{
    private readonly T[]? _array;

    internal static EquatableArray<ExposedParameter> Empty { get; set; }

    public EquatableArray(IEnumerable<T> items) => _array = [.. items];

    public bool IsEmpty => _array is null || _array.Length == 0;

    public T[] GetInner() => _array ?? Array.Empty<T>();

    public static implicit operator EquatableArray<T>(T[]? items) => new(items ?? []);

    public bool Equals(EquatableArray<T> other) =>
        (_array is null && other._array is null)
        || (_array is not null && other._array is not null && _array.SequenceEqual(other._array));

    public override bool Equals(object? obj) => obj is EquatableArray<T> other && Equals(other);

    public override int GetHashCode()
    {
        if (_array is null)
            return 0;
        var hash = 17;
        foreach (var item in _array)
        {
            hash = hash * 31 + (item?.GetHashCode() ?? 0);
        }
        return hash;
    }

    public IEnumerator<T> GetEnumerator() => (_array ?? []).AsEnumerable().GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
