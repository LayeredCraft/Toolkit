using System.Runtime.CompilerServices;

namespace LayeredCraft.Toolkit.Functional;

/// <summary>
/// A value that is either present or absent. The absent case is unambiguous: it is the
/// <see langword="null"/> contents of the union, never a present <see langword="null"/> value.
/// </summary>
/// <typeparam name="T">The type of the present value. It cannot be <see langword="null"/>.</typeparam>
/// <remarks>
/// <para>
/// <see cref="Maybe{T}"/> is a C# 15 union type. Its single case type is <typeparamref name="T"/>, so
/// an exhaustive <c>switch</c> tests <typeparamref name="T"/> and <see langword="null"/>:
/// </para>
/// <code>
/// Maybe&lt;int&gt; maybe = 42;
/// var text = maybe switch
/// {
///     int value =&gt; $"value: {value}",
///     null =&gt; "missing",
/// };
/// </code>
/// <para>
/// The union stores the payload and an <see langword="bool"/> in the struct and exposes the
/// non-boxing access pattern, so pattern matching value types does not box.
/// </para>
/// <para>
/// The default value of <see cref="Maybe{T}"/> is <see cref="None"/>, so
/// <c>default(Maybe&lt;int&gt;)</c> and <c>new Maybe&lt;int&gt;()</c> are empty rather than present.
/// </para>
/// </remarks>
[Union]
public readonly struct Maybe<T> : IUnion, Maybe<T>.IUnionMembers, IEquatable<Maybe<T>>
    where T : notnull
{
    private readonly T _value;
    private readonly bool _hasValue;

    /// <summary>Creates a present value.</summary>
    /// <param name="value">The value to wrap.</param>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    public Maybe(T value)
    {
        ArgumentNullException.ThrowIfNull(value);

        _value = value;
        _hasValue = true;
    }

    /// <summary>
    /// Declares the union members for <see cref="Maybe{T}"/>. The provider keeps the strongly typed
    /// <see cref="Value"/> property on the struct while the compiler still treats the type as a union.
    /// </summary>
    public interface IUnionMembers
    {
        /// <summary>Creates a present value.</summary>
        /// <param name="value">The value to wrap.</param>
        /// <returns>A <see cref="Maybe{T}"/> containing <paramref name="value"/>.</returns>
        static Maybe<T> Create(T value)
        {
            ArgumentNullException.ThrowIfNull(value);

            return new Maybe<T>(value);
        }

        /// <summary>Gets the union contents.</summary>
        object? Value { get; }

        /// <summary>Gets a value indicating whether the union has contents.</summary>
        bool HasValue { get; }

        /// <summary>Attempts to get the present value without boxing.</summary>
        /// <param name="value">The present value, or the default value when absent.</param>
        /// <returns><see langword="true"/> when a value is present; otherwise <see langword="false"/>.</returns>
        bool TryGetValue(out T value);
    }

    /// <summary>Gets the empty case.</summary>
    public static Maybe<T> None => default;

    /// <summary>Creates a present value.</summary>
    /// <param name="value">The value to wrap.</param>
    /// <returns>A <see cref="Maybe{T}"/> containing <paramref name="value"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    public static Maybe<T> Some(T value) => new(value);

    /// <summary>Gets a value indicating whether a value is present.</summary>
    public bool HasValue => _hasValue;

    /// <summary>Gets a value indicating whether the empty case is present.</summary>
    public bool IsNone => !_hasValue;

    /// <summary>Gets the present value.</summary>
    /// <exception cref="InvalidOperationException">The value is absent.</exception>
    public T Value
        => _hasValue
            ? _value
            : throw new InvalidOperationException(
                "The Maybe<T> is empty. Check HasValue before accessing Value.");

    /// <summary>Gets the union contents without changing the strongly typed API.</summary>
    object? IUnion.Value => _hasValue ? _value : null;

    /// <summary>Gets the union contents for pattern matching.</summary>
    object? IUnionMembers.Value => _hasValue ? _value : null;

    /// <summary>Gets a value indicating whether the union has contents.</summary>
    bool IUnionMembers.HasValue => _hasValue;

    /// <summary>Attempts to get the present value without throwing.</summary>
    /// <param name="value">The present value, or the default value when absent.</param>
    /// <returns><see langword="true"/> when a value is present; otherwise <see langword="false"/>.</returns>
    public bool TryGetValue(out T value)
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

    /// <summary>Runs one of two actions depending on the case.</summary>
    /// <param name="some">Runs when a value is present.</param>
    /// <param name="none">Runs when the value is absent.</param>
    public void Switch(Action<T> some, Action none)
    {
        ArgumentNullException.ThrowIfNull(some);
        ArgumentNullException.ThrowIfNull(none);

        if (_hasValue)
            some(_value);
        else
            none();
    }

    /// <summary>Wraps a value without an explicit <see cref="Some"/> call.</summary>
    /// <param name="value">The value to wrap.</param>
    public static implicit operator Maybe<T>(T value) => new(value);

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
