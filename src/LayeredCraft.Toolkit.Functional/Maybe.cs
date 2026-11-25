using System.Diagnostics.CodeAnalysis;

namespace LayeredCraft.Toolkit.Functional;

/// <summary>
/// A value that is either present or absent. The absent case is unambiguous: there is no
/// <see langword="null"/> representation and no boxing union case.
/// </summary>
/// <typeparam name="T">The type of the present value. It cannot be <see langword="null"/>.</typeparam>
/// <remarks>
/// The default value of <see cref="Maybe{T}"/> is <see cref="None"/>, so
/// <c>default(Maybe&lt;int&gt;)</c> and <c>new Maybe&lt;int&gt;()</c> are empty rather than present.
/// </remarks>
public readonly struct Maybe<T> : IEquatable<Maybe<T>>
    where T : notnull
{
    private readonly T _value;
    private readonly bool _hasValue;

    private Maybe(T value)
    {
        _value = value;
        _hasValue = true;
    }

    /// <summary>Gets the empty case.</summary>
    public static Maybe<T> None => default;

    /// <summary>Creates a present value.</summary>
    /// <param name="value">The value to wrap.</param>
    /// <returns>A <see cref="Maybe{T}"/> containing <paramref name="value"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    public static Maybe<T> Some(T value)
    {
        if (value is null)
            throw new ArgumentNullException(nameof(value));

        return new Maybe<T>(value);
    }

    /// <summary>Gets a value indicating whether a value is present.</summary>
    public bool HasValue => _hasValue;

    /// <summary>Gets a value indicating whether the empty case is present.</summary>
    public bool IsNone => !_hasValue;

    /// <summary>Gets the present value.</summary>
    /// <exception cref="InvalidOperationException">The value is absent.</exception>
    public T Value => _hasValue
        ? _value
        : throw new InvalidOperationException(
            "The Maybe<T> is empty. Check HasValue before accessing Value.");

    /// <summary>Attempts to get the present value without throwing.</summary>
    /// <param name="value">The present value, or the default value when absent.</param>
    /// <returns><see langword="true"/> when a value is present; otherwise <see langword="false"/>.</returns>
    public bool TryGetValue([MaybeNullWhen(false)] out T value)
    {
        value = _value;
        return _hasValue;
    }

    /// <summary>Projects both cases into a single result.</summary>
    /// <typeparam name="TResult">The type of the projected result.</typeparam>
    /// <param name="some">Projects a present value.</param>
    /// <param name="none">Produces the absent result.</param>
    /// <returns>The projected value.</returns>
    public TResult Match<TResult>(Func<T, TResult> some, Func<TResult> none)
    {
        ArgumentNullException.ThrowIfNull(some);
        ArgumentNullException.ThrowIfNull(none);

        return _hasValue ? some(_value) : none();
    }

    /// <summary>Wraps a value without an explicit <see cref="Some"/> call.</summary>
    /// <param name="value">The value to wrap.</param>
    public static implicit operator Maybe<T>(T value) => Some(value);

    /// <summary>Creates an empty <see cref="Maybe{T}"/> from <see cref="Maybe.None"/>.</summary>
    /// <param name="none">The empty case.</param>
    public static implicit operator Maybe<T>(None none) => default;

    /// <inheritdoc />
    public bool Equals(Maybe<T> other)
        => _hasValue == other._hasValue
            && (!_hasValue || EqualityComparer<T>.Default.Equals(_value, other._value));

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is Maybe<T> other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => _hasValue ? HashCode.Combine(true, _value) : 0;

    /// <inheritdoc />
    public override string ToString() => _hasValue ? $"Some({_value})" : "None";

    /// <summary>Compares two optional values.</summary>
    public static bool operator ==(Maybe<T> left, Maybe<T> right) => left.Equals(right);

    /// <summary>Compares two optional values.</summary>
    public static bool operator !=(Maybe<T> left, Maybe<T> right) => !left.Equals(right);
}
