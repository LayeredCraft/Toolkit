namespace LayeredCraft.Toolkit.Functional;

/// <summary>
/// The empty case of a <see cref="Maybe{T}"/>. It is one of the union's case types, so a
/// <c>switch</c> matches it directly instead of a <see langword="null"/> arm. It also exists so
/// <c>return Maybe.None;</c> reads like a value instead of <c>return default;</c>.
/// </summary>
public readonly struct None : IEquatable<None>
{
    /// <inheritdoc />
    public bool Equals(None other) => true;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is None;

    /// <inheritdoc />
    public override int GetHashCode() => 0;

    /// <inheritdoc />
    public override string ToString() => "None";

    /// <summary>Returns <see langword="true"/>; every <see cref="None"/> value is equal.</summary>
    public static bool operator ==(None left, None right) => true;

    /// <summary>Returns <see langword="false"/>; every <see cref="None"/> value is equal.</summary>
    public static bool operator !=(None left, None right) => false;
}

/// <summary>Creates the empty case for <see cref="Maybe{T}"/> values.</summary>
public static class Maybe
{
    /// <summary>Gets the empty <see cref="Maybe{T}"/> case.</summary>
    /// <example><c>Maybe&lt;int&gt; missing = Maybe.None;</c></example>
    public static None None => default;

    /// <summary>Creates a present value and infers the value type.</summary>
    /// <typeparam name="T">The present value type.</typeparam>
    /// <param name="value">The value to wrap.</param>
    /// <returns>A <see cref="Maybe{T}"/> containing <paramref name="value"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    public static Maybe<T> Some<T>(T value) where T : notnull => Maybe<T>.Some(value);
}
