namespace LayeredCraft.Toolkit.Functional;

/// <summary>
/// Type-inference factories for <see cref="Result{TValue}"/>. The static members on the type need
/// the type argument; these members let the compiler infer it from the value or the operation.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Result{TValue}"/> and <see cref="Result{TValue,TError}"/> already expose static
/// <c>Ok</c>, <c>Fail</c>, <c>Try</c>, and <c>TryAsync</c> members. This class adds the same
/// factories without the type argument, so a success value or a lambda return type supplies it:
/// </para>
/// <code>
/// var ok = Result.Ok(42);            // Result&lt;int&gt;
/// var failed = Result.Fail&lt;Order&gt;(error); // Result&lt;Order&gt;; the type argument is required
/// var parsed = Result.Try(() => int.Parse("42")); // Result&lt;int&gt;
/// </code>
/// <para>
/// A failure has no value to infer from, so <c>Fail</c> still needs the type argument. <c>Ok</c>,
/// <c>Try</c>, and <c>TryAsync</c> infer it from the value or the operation's return type.
/// </para>
/// </remarks>
public static class Result
{
    /// <summary>Creates a successful result and infers the value type.</summary>
    /// <typeparam name="TValue">The success value type.</typeparam>
    /// <param name="value">The success value.</param>
    /// <returns>A successful <see cref="Result{TValue}"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    public static Result<TValue> Ok<TValue>(TValue value) where TValue : notnull
        => Result<TValue>.Ok(value);

    /// <summary>Creates a failed result.</summary>
    /// <typeparam name="TValue">The success value type.</typeparam>
    /// <param name="error">The failure error.</param>
    /// <returns>A failed <see cref="Result{TValue}"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="error"/> is <see langword="null"/>.</exception>
    public static Result<TValue> Fail<TValue>(Error error) where TValue : notnull
        => Result<TValue>.Fail(error);

    /// <summary>
    /// Runs an operation and captures an unexpected exception. The value type is inferred from the
    /// operation's return type.
    /// </summary>
    /// <typeparam name="TValue">The success value type.</typeparam>
    /// <param name="operation">The operation to run.</param>
    /// <returns>The operation result, or a failure carrying the exception.</returns>
    public static Result<TValue> Try<TValue>(Func<TValue> operation) where TValue : notnull
        => Result<TValue>.Try(operation);

    /// <summary>
    /// Runs an operation and captures an unexpected exception. The value type is inferred from the
    /// operation's return type.
    /// </summary>
    /// <typeparam name="TValue">The success value type.</typeparam>
    /// <param name="operation">The operation to run.</param>
    /// <param name="errorFactory">Maps the captured exception to an error.</param>
    /// <returns>The operation result, or a failure carrying the mapped exception.</returns>
    public static Result<TValue> Try<TValue>(
        Func<TValue> operation,
        Func<Exception, Error> errorFactory) where TValue : notnull
        => Result<TValue>.Try(operation, errorFactory);

    /// <summary>Runs an asynchronous operation and captures an unexpected exception.</summary>
    /// <typeparam name="TValue">The success value type.</typeparam>
    /// <param name="operation">The asynchronous operation to run.</param>
    /// <returns>The operation result, or a failure carrying the exception.</returns>
    public static Task<Result<TValue>> TryAsync<TValue>(Func<Task<TValue>> operation)
        where TValue : notnull
        => Result<TValue>.TryAsync(operation);

    /// <summary>Runs an asynchronous operation and captures an unexpected exception.</summary>
    /// <typeparam name="TValue">The success value type.</typeparam>
    /// <param name="operation">The asynchronous operation to run.</param>
    /// <param name="errorFactory">Maps the captured exception to an error.</param>
    /// <returns>The operation result, or a failure carrying the mapped exception.</returns>
    public static Task<Result<TValue>> TryAsync<TValue>(
        Func<Task<TValue>> operation,
        Func<Exception, Error> errorFactory) where TValue : notnull
        => Result<TValue>.TryAsync(operation, errorFactory);
}
