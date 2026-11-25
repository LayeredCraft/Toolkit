using System.Runtime.CompilerServices;

namespace LayeredCraft.Toolkit.Functional;

/// <summary>
/// An operation outcome: either a success value or a failure error. The payloads live in the struct
/// and the union exposes the non-boxing access pattern, so matching and helpers do not box either
/// payload.
/// </summary>
/// <typeparam name="TValue">The type of the success value. It cannot be <see langword="null"/>.</typeparam>
/// <typeparam name="TError">The type of the failure error. It cannot be <see langword="null"/>.</typeparam>
/// <remarks>
/// <para>
/// <see cref="Result{TValue,TError}"/> is a C# 15 union type with two case types, so an exhaustive
/// <c>switch</c> tests both payloads:
/// </para>
/// <code>
/// Result&lt;int, Error&gt; result = Load();
/// var text = result switch
/// {
///     int value =&gt; $"ok: {value}",
///     Error error =&gt; $"failed: {error.Message}",
///     null =&gt; "uninitialized",
/// };
/// </code>
/// <para>
/// A default-initialized result is neither success nor failure. Accessing <see cref="Value"/>,
/// <see cref="Error"/>, or <see cref="Match{TResult}"/> on it throws. Create results with
/// <see cref="Ok"/> or <see cref="Fail"/>; the default state exists only because this is a struct,
/// and it is the <see langword="null"/> contents of the union.
/// </para>
/// </remarks>
[Union]
public readonly struct Result<TValue, TError>
    : IUnion, Result<TValue, TError>.IUnionMembers, IEquatable<Result<TValue, TError>>
    where TValue : notnull
    where TError : notnull
{
    private const byte Uninitialized = 0;
    private const byte Success = 1;
    private const byte Failure = 2;

    private readonly TValue _value;
    private readonly TError _error;
    private readonly byte _state;

    private Result(TValue value, TError error, byte state)
    {
        _value = value;
        _error = error;
        _state = state;
    }

    /// <summary>
    /// Declares the union members for <see cref="Result{TValue,TError}"/>. The provider keeps the
    /// strongly typed <see cref="Value"/> and <see cref="Error"/> properties on the struct while the
    /// compiler still treats the type as a union.
    /// </summary>
    public interface IUnionMembers
    {
        /// <summary>Creates a successful result.</summary>
        /// <param name="value">The success value.</param>
        /// <returns>A successful <see cref="Result{TValue,TError}"/>.</returns>
        static Result<TValue, TError> Create(TValue value)
        {
            ArgumentNullException.ThrowIfNull(value);

            return new(value, default!, Success);
        }

        /// <summary>Creates a failed result.</summary>
        /// <param name="error">The failure error.</param>
        /// <returns>A failed <see cref="Result{TValue,TError}"/>.</returns>
        static Result<TValue, TError> Create(TError error)
        {
            ArgumentNullException.ThrowIfNull(error);

            return new(default!, error, Failure);
        }

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
        bool TryGetValue(out TError error);
    }

    /// <summary>Creates a successful result.</summary>
    /// <param name="value">The success value.</param>
    /// <returns>A successful <see cref="Result{TValue,TError}"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    public static Result<TValue, TError> Ok(TValue value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return new(value, default!, Success);
    }

    /// <summary>Creates a failed result.</summary>
    /// <param name="error">The failure error.</param>
    /// <returns>A failed <see cref="Result{TValue,TError}"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="error"/> is <see langword="null"/>.</exception>
    public static Result<TValue, TError> Fail(TError error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new(default!, error, Failure);
    }

    /// <summary>Creates a result by running an operation and capturing an unexpected exception.</summary>
    /// <param name="operation">The operation to run.</param>
    /// <param name="errorFactory">Maps the captured exception to an error.</param>
    /// <returns>The operation result, or a failure carrying the mapped exception.</returns>
    public static Result<TValue, TError> Try(
        Func<TValue> operation,
        Func<Exception, TError> errorFactory)
    {
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentNullException.ThrowIfNull(errorFactory);

        try
        {
            return Ok(operation());
        }
        catch (Exception exception)
        {
            return Fail(errorFactory(exception));
        }
    }

    /// <summary>Creates a result by awaiting an operation and capturing an unexpected exception.</summary>
    /// <param name="operation">The asynchronous operation to run.</param>
    /// <param name="errorFactory">Maps the captured exception to an error.</param>
    /// <returns>The operation result, or a failure carrying the mapped exception.</returns>
    public static async Task<Result<TValue, TError>> TryAsync(
        Func<Task<TValue>> operation,
        Func<Exception, TError> errorFactory)
    {
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentNullException.ThrowIfNull(errorFactory);

        try
        {
            return Ok(await operation().ConfigureAwait(false));
        }
        catch (Exception exception)
        {
            return Fail(errorFactory(exception));
        }
    }

    /// <summary>Gets a value indicating whether the result is a success.</summary>
    public bool IsSuccess => _state == Success;

    /// <summary>Gets a value indicating whether the result is a failure.</summary>
    public bool IsFailure => _state == Failure;

    /// <summary>Gets a value indicating whether this is neither a success nor a failure.</summary>
    public bool IsInitialized => _state != Uninitialized;

    /// <summary>Gets the success value.</summary>
    /// <exception cref="InvalidOperationException">The result is a failure or is uninitialized.</exception>
    public TValue Value => _state == Success
        ? _value
        : throw new InvalidOperationException(_state == Failure
            ? "The result is a failure. Check IsSuccess before accessing Value."
            : "The result is uninitialized. Create one with Result<TValue, TError>.Ok or Fail.");

    /// <summary>Gets the failure error.</summary>
    /// <exception cref="InvalidOperationException">The result is a success or is uninitialized.</exception>
    public TError Error => _state == Failure
        ? _error
        : throw new InvalidOperationException(_state == Success
            ? "The result is a success. Check IsFailure before accessing Error."
            : "The result is uninitialized. Create one with Result<TValue, TError>.Ok or Fail.");

    /// <summary>Gets the union contents without changing the strongly typed API.</summary>
    object? IUnion.Value => _state switch
    {
        Success => _value,
        Failure => _error,
        _ => null,
    };

    /// <summary>Gets the union contents for pattern matching.</summary>
    object? IUnionMembers.Value => _state switch
    {
        Success => _value,
        Failure => _error,
        _ => null,
    };

    /// <summary>Gets a value indicating whether the union has contents.</summary>
    bool IUnionMembers.HasValue => _state != Uninitialized;

    /// <summary>Attempts to get the success value without throwing.</summary>
    /// <param name="value">The success value, or the default value when the result is not a success.</param>
    /// <returns><see langword="true"/> when the result is a success; otherwise <see langword="false"/>.</returns>
    public bool TryGetValue(out TValue value)
    {
        if (_state == Success)
        {
            value = _value;
            return true;
        }

        value = default!;
        return false;
    }

    /// <summary>Attempts to get the failure error without throwing.</summary>
    /// <param name="error">The failure error, or the default value when the result is not a failure.</param>
    /// <returns><see langword="true"/> when the result is a failure; otherwise <see langword="false"/>.</returns>
    public bool TryGetError(out TError error)
    {
        if (_state == Failure)
        {
            error = _error;
            return true;
        }

        error = default!;
        return false;
    }

    /// <summary>Attempts to get the failure error without boxing for pattern matching.</summary>
    /// <param name="error">The failure error, or the default value when the result is not a failure.</param>
    /// <returns><see langword="true"/> when the result is a failure; otherwise <see langword="false"/>.</returns>
    bool IUnionMembers.TryGetValue(out TError error) => TryGetError(out error);

    /// <summary>Projects both cases into a single result.</summary>
    /// <typeparam name="TResult">The type of the projected result.</typeparam>
    /// <param name="success">Projects the success value.</param>
    /// <param name="failure">Projects the failure error.</param>
    /// <returns>The projected value.</returns>
    /// <exception cref="InvalidOperationException">The result is uninitialized.</exception>
    public TResult Match<TResult>(Func<TValue, TResult> success, Func<TError, TResult> failure)
    {
        ArgumentNullException.ThrowIfNull(success);
        ArgumentNullException.ThrowIfNull(failure);

        return _state switch
        {
            Success => success(_value),
            Failure => failure(_error),
            _ => throw new InvalidOperationException(
                "The result is uninitialized. Create one with Result<TValue, TError>.Ok or Fail."),
        };
    }

    /// <inheritdoc />
    public bool Equals(Result<TValue, TError> other)
    {
        if (_state != other._state)
            return false;

        return _state switch
        {
            Success => EqualityComparer<TValue>.Default.Equals(_value, other._value),
            Failure => EqualityComparer<TError>.Default.Equals(_error, other._error),
            _ => true,
        };
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
        => obj is Result<TValue, TError> other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => _state switch
    {
        Success => HashCode.Combine(Success, _value),
        Failure => HashCode.Combine(Failure, _error),
        _ => 0,
    };

    /// <inheritdoc />
    public override string ToString() => _state switch
    {
        Success => $"Ok({_value})",
        Failure => $"Fail({_error})",
        _ => "Uninitialized",
    };

    /// <summary>Runs one of two actions depending on the case.</summary>
    /// <param name="success">Runs on a success.</param>
    /// <param name="failure">Runs on a failure.</param>
    /// <exception cref="InvalidOperationException">The result is uninitialized.</exception>
    public void Switch(Action<TValue> success, Action<TError> failure)
    {
        ArgumentNullException.ThrowIfNull(success);
        ArgumentNullException.ThrowIfNull(failure);

        switch (_state)
        {
            case Success:
                success(_value);
                break;
            case Failure:
                failure(_error);
                break;
            default:
                throw new InvalidOperationException(
                    "The result is uninitialized. Create one with Result<TValue, TError>.Ok or Fail.");
        }
    }

    /// <summary>Compares two results by case and payload.</summary>
    public static bool operator ==(Result<TValue, TError> left, Result<TValue, TError> right)
        => left.Equals(right);

    /// <summary>Compares two results by case and payload.</summary>
    public static bool operator !=(Result<TValue, TError> left, Result<TValue, TError> right)
        => !left.Equals(right);
}
