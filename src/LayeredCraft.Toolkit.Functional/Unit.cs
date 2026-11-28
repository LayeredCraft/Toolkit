namespace LayeredCraft.Toolkit.Functional;

/// <summary>
/// Represents the absence of a meaningful value. Use it as the success value of a
/// <see cref="Result{TValue,TError}"/> when an operation only needs to report success or failure.
/// </summary>
public readonly struct Unit : IEquatable<Unit>
{
    /// <summary>Gets the single <see cref="Unit"/> value.</summary>
    public static Unit Value => default;

    /// <inheritdoc />
    public bool Equals(Unit other) => true;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is Unit;

    /// <inheritdoc />
    public override int GetHashCode() => 0;

    /// <inheritdoc />
    public override string ToString() => "()";

    /// <summary>Returns <see langword="true"/>; every <see cref="Unit"/> value is equal.</summary>
    public static bool operator ==(Unit left, Unit right) => true;

    /// <summary>Returns <see langword="false"/>; every <see cref="Unit"/> value is equal.</summary>
    public static bool operator !=(Unit left, Unit right) => false;
}
