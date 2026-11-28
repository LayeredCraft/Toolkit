using System.Runtime.CompilerServices;
using ErrorType = LayeredCraft.Toolkit.Functional.Error;

namespace LayeredCraft.Toolkit.Functional;

/// <summary>
/// An operation outcome: either a success value or an <see cref="Error"/>. This is the ergonomic
/// default; use <see cref="Result{TValue,TError}"/> when the failure is a closed set of domain
/// cases.
/// </summary>
/// <typeparam name="TValue">The type of the success value. It cannot be <see langword="null"/>.</typeparam>
/// <remarks>
/// <para>
/// <see cref="Result{TValue}"/> is a C# 15 union type with two case types, so an exhaustive
/// <c>switch</c> tests both cases:
/// </para>
/// <code>
/// Result&lt;int&gt; result = Load();
/// var text = result switch
/// {
///     int value =&gt; $"ok: {value}",
///     Error error =&gt; $"failed: {error.Message}",
/// };
/// </code>
/// <para>
/// The default value is a failure with <see cref="Error.Uninitialized"/>, not a null payload. That
/// keeps <c>default(Result&lt;T&gt;)</c> a valid case, so a <c>switch</c> never needs a
/// <see langword="null"/> arm. Check <see cref="IsUninitialized"/> when a forgotten factory call
/// must be distinguishable from a real failure.
/// </para>
/// <para>
/// Unlike <see cref="Result{TValue,TError}"/>, whose default is uninitialized and throws on access,
/// this type folds the default into the failure case. That is possible only because the error type
/// is fixed: a generic <c>TError</c> has no meaningful default to synthesize.
/// </para>
/// </remarks>
[Union]
public readonly struct Result<TValue> : IUnion, Result<TValue>.IUnionMembers, IEquatable<Result<TValue>>
    where TValue : notnull
{
    private readonly Result<TValue, Error> _inner;

    private Result(Result<TValue, Error> inner) => _inner = inner;

    /// <summary>
    /// Declares the union members for <see cref="Result{TValue}"/>. The provider keeps the strongly
    /// typed <see cref="Value"/> and <see cref="Error"/> properties on the struct while the compiler
    /// still treats the type as a union.
    /// </summary>
    public interface IUnionMembers
    {
        /// <summary>Creates a successful result.</summary>
        /// <param name="value">The success value.</param>
        /// <returns>A successful <see cref="Result{TValue}"/>.</returns>
        static Result<TValue> Create(TValue value) => Ok(value);

        /// <summary>Creates a failed result.</summary>
        /// <param name="error">The failure error.</param>
        /// <returns>A failed <see cref="Result{TValue}"/>.</returns>
        static Result<TValue> Create(Error error) => Fail(error);

        /// <summary>Gets the union contents.</summary>
        object? Value { get; }

        /// <summary>Gets a value indicating whether the union has contents.</summary>
        bool HasValue { get; }

        /// <summary>Attempts to get the success value without boxing.</summary>
        /// <param name="value">The success value, or the default value when the result is not a success.</param>
        /// <returns><see langword="true"/> when the result is a success; otherwise <see langword="false"/>.</returns>
        bool TryGetValue(out TValue value);

        /// <summary>Attempts to get the failure error without boxing.</summary>
        /// <param name="error">The failure error, or the default value when the result is not a failure.</param>
        /// <returns><see langword="true"/> when the result is a failure; otherwise <see langword="false"/>.</returns>
        bool TryGetValue(out Error error);
    }

    /// <summary>Creates a successful result.</summary>
    /// <param name="value">The success value.</param>
    /// <returns>A successful <see cref="Result{TValue}"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    public static Result<TValue> Ok(TValue value) => new(Result<TValue, Error>.Ok(value));

    /// <summary>Creates a failed result.</summary>
    /// <param name="error">The failure error.</param>
    /// <returns>A failed <see cref="Result{TValue}"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="error"/> is <see langword="null"/>.</exception>
    public static Result<TValue> Fail(Error error) => new(Result<TValue, Error>.Fail(error));

    /// <summary>Creates a result by running an operation and capturing an unexpected exception.</summary>
    /// <param name="operation">The operation to run.</param>
    /// <returns>The operation result, or a failure carrying the exception.</returns>
    public static Result<TValue> Try(Func<TValue> operation)
        => new(Result<TValue, Error>.Try(operation, ErrorType.FromException));

    /// <summary>Creates a result by running an operation and capturing an unexpected exception.</summary>
    /// <param name="operation">The operation to run.</param>
    /// <param name="errorFactory">Maps the captured exception to an error.</param>
    /// <returns>The operation result, or a failure carrying the mapped exception.</returns>
    public static Result<TValue> Try(Func<TValue> operation, Func<Exception, Error> errorFactory)
        => new(Result<TValue, Error>.Try(operation, errorFactory));

    /// <summary>Creates a result by awaiting an operation and capturing an unexpected exception.</summary>
    /// <param name="operation">The asynchronous operation to run.</param>
    /// <returns>The operation result, or a failure carrying the exception.</returns>
    public static async Task<Result<TValue>> TryAsync(Func<Task<TValue>> operation)
        => new(await Result<TValue, Error>
            .TryAsync(operation, ErrorType.FromException)
            .ConfigureAwait(false));

    /// <summary>Creates a result by awaiting an operation and capturing an unexpected exception.</summary>
    /// <param name="operation">The asynchronous operation to run.</param>
    /// <param name="errorFactory">Maps the captured exception to an error.</param>
    /// <returns>The operation result, or a failure carrying the mapped exception.</returns>
    public static async Task<Result<TValue>> TryAsync(
        Func<Task<TValue>> operation,
        Func<Exception, Error> errorFactory)
        => new(await Result<TValue, Error>
            .TryAsync(operation, errorFactory)
            .ConfigureAwait(false));

    /// <summary>Gets a value indicating whether the result is a success.</summary>
    public bool IsSuccess => _inner.IsSuccess;

    /// <summary>
    /// Gets a value indicating whether the result is a failure. A default-initialized result is a
    /// failure carrying <see cref="Error.Uninitialized"/>.
    /// </summary>
    public bool IsFailure => !_inner.IsSuccess;

    /// <summary>Gets a value indicating whether the result is the default-initialized failure.</summary>
    public bool IsUninitialized => !_inner.IsInitialized;

    /// <summary>Gets the success value.</summary>
    /// <exception cref="InvalidOperationException">The result is a failure.</exception>
    public TValue Value
        => _inner.IsSuccess
            ? _inner.Value
            : throw new InvalidOperationException(
                _inner.IsInitialized
                    ? "The result is a failure. Check IsSuccess before accessing Value."
                    : "The result is uninitialized. It is a failure with Error.Uninitialized.");

    /// <summary>Gets the failure error, or <see cref="Error.Uninitialized"/> for a default result.</summary>
    /// <exception cref="InvalidOperationException">The result is a success.</exception>
    public Error Error
        => _inner.IsSuccess
            ? throw new InvalidOperationException(
                "The result is a success. Check IsFailure before accessing Error.")
            : _inner.IsInitialized
                ? _inner.Error
                : ErrorType.Uninitialized;

    /// <summary>Gets the union contents without changing the strongly typed API.</summary>
    object? IUnion.Value => _inner.IsSuccess ? _inner.Value : Error;

    /// <summary>Gets the union contents for pattern matching.</summary>
    object? IUnionMembers.Value => _inner.IsSuccess ? _inner.Value : Error;

    /// <summary>
    /// Gets a value indicating whether the union has contents. Always <see langword="true"/>: the
    /// default value is the <see cref="Error.Uninitialized"/> failure case, not empty contents.
    /// </summary>
    bool IUnionMembers.HasValue => true;

    /// <summary>Attempts to get the success value without throwing.</summary>
    /// <param name="value">The success value, or the default value when the result is not a success.</param>
    /// <returns><see langword="true"/> when the result is a success; otherwise <see langword="false"/>.</returns>
    public bool TryGetValue(out TValue value) => _inner.TryGetValue(out value);

    /// <summary>Attempts to get the failure error without throwing.</summary>
    /// <param name="error">The failure error, <see cref="Error.Uninitialized"/> for a default result, or the default value when the result is a success.</param>
    /// <returns><see langword="true"/> when the result is a failure; otherwise <see langword="false"/>.</returns>
    public bool TryGetError(out Error error)
    {
        if (_inner.IsSuccess)
        {
            error = default;
            return false;
        }

        error = _inner.IsInitialized ? _inner.Error : ErrorType.Uninitialized;
        return true;
    }

    /// <summary>Attempts to get the failure error without boxing for pattern matching.</summary>
    /// <param name="error">The failure error, or <see cref="Error.Uninitialized"/> for a default result.</param>
    /// <returns><see langword="true"/> when the result is a failure; otherwise <see langword="false"/>.</returns>
    bool IUnionMembers.TryGetValue(out Error error) => TryGetError(out error);

    /// <summary>Projects both cases into a single result.</summary>
    /// <typeparam name="TResult">The type of the projected result.</typeparam>
    /// <param name="success">Projects the success value.</param>
    /// <param name="failure">Projects the failure error.</param>
    /// <returns>The projected value.</returns>
    public TResult Match<TResult>(Func<TValue, TResult> success, Func<Error, TResult> failure)
        => Normalized.Match(success, failure);

    /// <summary>Runs one of two actions depending on the case.</summary>
    /// <param name="success">Runs on a success.</param>
    /// <param name="failure">Runs on a failure.</param>
    public void Switch(Action<TValue> success, Action<Error> failure)
        => Normalized.Switch(success, failure);

    /// <summary>Converts the result to <see cref="Result{TValue,TError}"/> with <see cref="Error"/> as the error type.</summary>
    /// <returns>The equivalent generic result.</returns>
    public Result<TValue, Error> ToGeneric() => Normalized;

    /// <summary>Wraps a generic result whose error type is <see cref="Error"/>.</summary>
    /// <param name="result">The generic result.</param>
    /// <returns>The equivalent fixed-error result.</returns>
    public static Result<TValue> From(Result<TValue, Error> result)
        => result.IsInitialized ? new Result<TValue>(result) : default;

    /// <summary>Converts a fixed-error result to the generic result.</summary>
    /// <param name="result">The fixed-error result.</param>
    public static implicit operator Result<TValue, Error>(Result<TValue> result) => result.Normalized;

    /// <summary>Converts a generic result to the fixed-error result.</summary>
    /// <param name="result">The generic result.</param>
    public static implicit operator Result<TValue>(Result<TValue, Error> result) => From(result);

    /// <inheritdoc />
    public bool Equals(Result<TValue> other) => Normalized.Equals(other.Normalized);

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is Result<TValue> other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => Normalized.GetHashCode();

    /// <inheritdoc />
    public override string ToString() => Normalized.ToString();

    /// <summary>Compares two results by case and payload.</summary>
    public static bool operator ==(Result<TValue> left, Result<TValue> right) => left.Equals(right);

    /// <summary>Compares two results by case and payload.</summary>
    public static bool operator !=(Result<TValue> left, Result<TValue> right) => !left.Equals(right);

    /// <summary>
    /// Normalizes the default value to the failure case so the default, equality, and matching all
    /// agree that it is <see cref="Error.Uninitialized"/>.
    /// </summary>
    private Result<TValue, Error> Normalized
        => _inner.IsInitialized ? _inner : Result<TValue, Error>.Fail(ErrorType.Uninitialized);
}
