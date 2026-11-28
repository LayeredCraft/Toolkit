namespace LayeredCraft.Toolkit.Functional;

/// <summary>
/// Helpers for collections of <see cref="Maybe{T}"/> values. Each helper keeps the first absent
/// value as the short-circuit point and never boxes a payload.
/// </summary>
public static class MaybeSequenceExtensions
{
    /// <summary>Yields the present values and drops the absent ones.</summary>
    /// <typeparam name="T">The present value type.</typeparam>
    /// <param name="source">The optional values.</param>
    public static IEnumerable<T> Choose<T>(this IEnumerable<Maybe<T>> source) where T : notnull
    {
        ArgumentNullException.ThrowIfNull(source);

        foreach (var maybe in source)
            if (maybe.TryGetValue(out var value))
                yield return value;
    }

    /// <summary>Keeps every value only when all values are present.</summary>
    /// <typeparam name="T">The present value type.</typeparam>
    /// <param name="source">The optional values.</param>
    /// <returns>The present values, or the empty case when any value is absent.</returns>
    public static Maybe<IReadOnlyList<T>> Sequence<T>(this IEnumerable<Maybe<T>> source)
        where T : notnull
    {
        ArgumentNullException.ThrowIfNull(source);

        var values = new List<T>();

        foreach (var maybe in source)
        {
            if (!maybe.TryGetValue(out var value))
                return Maybe<IReadOnlyList<T>>.None;

            values.Add(value);
        }

        return Maybe<IReadOnlyList<T>>.Some(values);
    }

    /// <summary>Maps each item and keeps every value only when all projections are present.</summary>
    /// <typeparam name="T">The source item type.</typeparam>
    /// <typeparam name="TResult">The projected value type.</typeparam>
    /// <param name="source">The source items.</param>
    /// <param name="selector">The optional projection.</param>
    /// <returns>The projected values, or the empty case when any projection is absent.</returns>
    public static Maybe<IReadOnlyList<TResult>> Traverse<T, TResult>(
        this IEnumerable<T> source,
        Func<T, Maybe<TResult>> selector) where TResult : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(selector);

        var values = new List<TResult>();

        foreach (var item in source)
        {
            if (!selector(item).TryGetValue(out var value))
                return Maybe<IReadOnlyList<TResult>>.None;

            values.Add(value);
        }

        return Maybe<IReadOnlyList<TResult>>.Some(values);
    }

    /// <summary>Returns the first item, or the empty case when the sequence is empty.</summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <param name="source">The source items.</param>
    public static Maybe<T> FirstOrNone<T>(this IEnumerable<T> source) where T : notnull
    {
        ArgumentNullException.ThrowIfNull(source);

        foreach (var item in source)
            return Maybe<T>.Some(item);

        return Maybe<T>.None;
    }

    /// <summary>Returns the first item that satisfies the predicate, or the empty case.</summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <param name="source">The source items.</param>
    /// <param name="predicate">The predicate to satisfy.</param>
    public static Maybe<T> FirstOrNone<T>(this IEnumerable<T> source, Func<T, bool> predicate)
        where T : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(predicate);

        foreach (var item in source)
            if (predicate(item))
                return Maybe<T>.Some(item);

        return Maybe<T>.None;
    }
}

/// <summary>
/// Helpers for collections of <see cref="Result{TValue,TError}"/> values. Each helper keeps the
/// first failure as the short-circuit point and never boxes a payload.
/// </summary>
public static class ResultSequenceExtensions
{
    /// <summary>Yields the success values and drops the failures.</summary>
    /// <typeparam name="TValue">The success value type.</typeparam>
    /// <typeparam name="TError">The failure error type.</typeparam>
    /// <param name="source">The results.</param>
    public static IEnumerable<TValue> Successes<TValue, TError>(
        this IEnumerable<Result<TValue, TError>> source)
        where TValue : notnull where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(source);

        foreach (var result in source)
            if (result.TryGetValue(out var value))
                yield return value;
    }

    /// <summary>Yields the failure errors and drops the successes.</summary>
    /// <typeparam name="TValue">The success value type.</typeparam>
    /// <typeparam name="TError">The failure error type.</typeparam>
    /// <param name="source">The results.</param>
    public static IEnumerable<TError> Failures<TValue, TError>(
        this IEnumerable<Result<TValue, TError>> source)
        where TValue : notnull where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(source);

        foreach (var result in source)
            if (result.TryGetError(out var error))
                yield return error;
    }

    /// <summary>Splits the results into their success values and failure errors.</summary>
    /// <typeparam name="TValue">The success value type.</typeparam>
    /// <typeparam name="TError">The failure error type.</typeparam>
    /// <param name="source">The results.</param>
    public static (IReadOnlyList<TValue> Successes, IReadOnlyList<TError> Failures)
        Partition<TValue, TError>(this IEnumerable<Result<TValue, TError>> source)
        where TValue : notnull where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(source);

        var successes = new List<TValue>();
        var failures = new List<TError>();

        foreach (var result in source)
            if (result.TryGetValue(out var value))
                successes.Add(value);
            else if (result.TryGetError(out var error))
                failures.Add(error);

        return (successes, failures);
    }

    /// <summary>Keeps every value only when all results are successes.</summary>
    /// <typeparam name="TValue">The success value type.</typeparam>
    /// <typeparam name="TError">The failure error type.</typeparam>
    /// <param name="source">The results.</param>
    /// <returns>The success values, or the first failure.</returns>
    public static Result<IReadOnlyList<TValue>, TError> Sequence<TValue, TError>(
        this IEnumerable<Result<TValue, TError>> source)
        where TValue : notnull where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(source);

        var values = new List<TValue>();

        foreach (var result in source)
        {
            if (result.TryGetValue(out var value))
            {
                values.Add(value);
                continue;
            }

            if (!result.IsInitialized)
                return default;

            if (result.TryGetError(out var error))
                return Result<IReadOnlyList<TValue>, TError>.Fail(error);
        }

        return Result<IReadOnlyList<TValue>, TError>.Ok(values);
    }

    /// <summary>Maps each item and keeps every value only when all projections are successes.</summary>
    /// <typeparam name="T">The source item type.</typeparam>
    /// <typeparam name="TValue">The projected value type.</typeparam>
    /// <typeparam name="TError">The failure error type.</typeparam>
    /// <param name="source">The source items.</param>
    /// <param name="selector">The result projection.</param>
    /// <returns>The projected values, or the first failure.</returns>
    public static Result<IReadOnlyList<TValue>, TError> Traverse<T, TValue, TError>(
        this IEnumerable<T> source,
        Func<T, Result<TValue, TError>> selector) where TValue : notnull where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(selector);

        var values = new List<TValue>();

        foreach (var item in source)
        {
            var result = selector(item);

            if (result.TryGetValue(out var value))
            {
                values.Add(value);
                continue;
            }

            if (!result.IsInitialized)
                return default;

            if (result.TryGetError(out var error))
                return Result<IReadOnlyList<TValue>, TError>.Fail(error);
        }

        return Result<IReadOnlyList<TValue>, TError>.Ok(values);
    }
}

/// <summary>
/// Helpers for collections of <see cref="Result{TValue}"/> values. Each helper keeps the first
/// failure as the short-circuit case, including a default-initialized
/// <see cref="Error.Uninitialized"/> failure.
/// </summary>
public static class ResultOfTSequenceExtensions
{
    /// <summary>Yields the success values and drops the failures.</summary>
    /// <typeparam name="TValue">The success value type.</typeparam>
    /// <param name="source">The results.</param>
    public static IEnumerable<TValue> Successes<TValue>(this IEnumerable<Result<TValue>> source)
        where TValue : notnull
    {
        ArgumentNullException.ThrowIfNull(source);

        foreach (var result in source)
            if (result.IsSuccess)
                yield return result.Value;
    }

    /// <summary>Yields the failure errors and drops the successes.</summary>
    /// <typeparam name="TValue">The success value type.</typeparam>
    /// <param name="source">The results.</param>
    public static IEnumerable<Error> Failures<TValue>(this IEnumerable<Result<TValue>> source)
        where TValue : notnull
    {
        ArgumentNullException.ThrowIfNull(source);

        foreach (var result in source)
            if (result.IsFailure)
                yield return result.Error;
    }

    /// <summary>Partitions the results into success values and failure errors.</summary>
    /// <typeparam name="TValue">The success value type.</typeparam>
    /// <param name="source">The results.</param>
    public static (IReadOnlyList<TValue> Successes, IReadOnlyList<Error> Failures)
        Partition<TValue>(this IEnumerable<Result<TValue>> source) where TValue : notnull
    {
        ArgumentNullException.ThrowIfNull(source);

        var successes = new List<TValue>();
        var failures = new List<Error>();

        foreach (var result in source)
            if (result.IsSuccess)
                successes.Add(result.Value);
            else
                failures.Add(result.Error);

        return (successes, failures);
    }

    /// <summary>Keeps the success values or returns the first failure.</summary>
    /// <typeparam name="TValue">The success value type.</typeparam>
    /// <param name="source">The results.</param>
    public static Result<IReadOnlyList<TValue>> Sequence<TValue>(
        this IEnumerable<Result<TValue>> source) where TValue : notnull
    {
        ArgumentNullException.ThrowIfNull(source);

        var values = new List<TValue>();

        foreach (var result in source)
        {
            if (result.IsUninitialized)
                return default;

            if (result.IsSuccess)
            {
                values.Add(result.Value);
                continue;
            }

            return Result<IReadOnlyList<TValue>>.Fail(result.Error);
        }

        return Result<IReadOnlyList<TValue>>.Ok(values);
    }

    /// <summary>Projects each item and keeps the successes or returns the first failure.</summary>
    /// <typeparam name="T">The source item type.</typeparam>
    /// <typeparam name="TValue">The projected value type.</typeparam>
    /// <param name="source">The source items.</param>
    /// <param name="selector">The result projection.</param>
    public static Result<IReadOnlyList<TValue>> Traverse<T, TValue>(
        this IEnumerable<T> source,
        Func<T, Result<TValue>> selector) where TValue : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(selector);

        var values = new List<TValue>();

        foreach (var item in source)
        {
            var result = selector(item);

            if (result.IsUninitialized)
                return default;

            if (result.IsSuccess)
            {
                values.Add(result.Value);
                continue;
            }

            return Result<IReadOnlyList<TValue>>.Fail(result.Error);
        }

        return Result<IReadOnlyList<TValue>>.Ok(values);
    }
}
