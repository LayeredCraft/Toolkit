namespace LayeredCraft.Toolkit.Functional;

/// <summary>
/// Asynchronous helpers for collections of <see cref="Maybe{T}"/> values.
/// </summary>
public static class MaybeSequenceAsyncExtensions
{
    /// <summary>Maps each item asynchronously and keeps every value only when all projections are present.</summary>
    /// <typeparam name="T">The source item type.</typeparam>
    /// <typeparam name="TResult">The projected value type.</typeparam>
    /// <param name="source">The source items.</param>
    /// <param name="selector">The asynchronous optional projection.</param>
    /// <returns>The projected values, or the empty case when any projection is absent.</returns>
    public static async Task<Maybe<IReadOnlyList<TResult>>> TraverseAsync<T, TResult>(
        this IEnumerable<T> source,
        Func<T, Task<Maybe<TResult>>> selector) where TResult : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(selector);

        var values = new List<TResult>();

        foreach (var item in source)
        {
            var maybe = await selector(item).ConfigureAwait(false);

            if (!maybe.TryGetValue(out var value))
                return Maybe<IReadOnlyList<TResult>>.None;

            values.Add(value);
        }

        return Maybe<IReadOnlyList<TResult>>.Some(values);
    }

    /// <summary>Keeps every value only when all asynchronous optional values are present.</summary>
    /// <typeparam name="T">The present value type.</typeparam>
    /// <param name="source">The asynchronous optional values.</param>
    /// <returns>The present values, or the empty case when any value is absent.</returns>
    public static async Task<Maybe<IReadOnlyList<T>>> SequenceAsync<T>(
        this IEnumerable<Task<Maybe<T>>> source) where T : notnull
    {
        ArgumentNullException.ThrowIfNull(source);

        var values = new List<T>();

        foreach (var task in source)
        {
            var maybe = await task.ConfigureAwait(false);

            if (!maybe.TryGetValue(out var value))
                return Maybe<IReadOnlyList<T>>.None;

            values.Add(value);
        }

        return Maybe<IReadOnlyList<T>>.Some(values);
    }
}

/// <summary>
/// Asynchronous helpers for collections of <see cref="Result{TValue,TError}"/> values.
/// </summary>
public static class ResultSequenceAsyncExtensions
{
    /// <summary>Maps each item asynchronously and keeps every value only when all projections are successes.</summary>
    /// <typeparam name="T">The source item type.</typeparam>
    /// <typeparam name="TValue">The projected value type.</typeparam>
    /// <typeparam name="TError">The failure error type.</typeparam>
    /// <param name="source">The source items.</param>
    /// <param name="selector">The asynchronous result projection.</param>
    /// <returns>The projected values, the first failure, or an uninitialized result.</returns>
    public static async Task<Result<IReadOnlyList<TValue>, TError>> TraverseAsync<T, TValue, TError>(
        this IEnumerable<T> source,
        Func<T, Task<Result<TValue, TError>>> selector)
        where TValue : notnull where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(selector);

        var values = new List<TValue>();

        foreach (var item in source)
        {
            var result = await selector(item).ConfigureAwait(false);

            if (!result.IsInitialized)
                return default;

            if (result.TryGetValue(out var value))
            {
                values.Add(value);
                continue;
            }

            if (result.TryGetError(out var error))
                return Result<IReadOnlyList<TValue>, TError>.Fail(error);
        }

        return Result<IReadOnlyList<TValue>, TError>.Ok(values);
    }

    /// <summary>Keeps every value only when all asynchronous results are successes.</summary>
    /// <typeparam name="TValue">The success value type.</typeparam>
    /// <typeparam name="TError">The failure error type.</typeparam>
    /// <param name="source">The asynchronous results.</param>
    /// <returns>The success values, the first failure, or an uninitialized result.</returns>
    public static async Task<Result<IReadOnlyList<TValue>, TError>> SequenceAsync<TValue, TError>(
        this IEnumerable<Task<Result<TValue, TError>>> source)
        where TValue : notnull where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(source);

        var values = new List<TValue>();

        foreach (var task in source)
        {
            var result = await task.ConfigureAwait(false);

            if (!result.IsInitialized)
                return default;

            if (result.TryGetValue(out var value))
            {
                values.Add(value);
                continue;
            }

            if (result.TryGetError(out var error))
                return Result<IReadOnlyList<TValue>, TError>.Fail(error);
        }

        return Result<IReadOnlyList<TValue>, TError>.Ok(values);
    }
}

/// <summary>
/// Asynchronous helpers for collections of <see cref="Result{TValue}"/> values.
/// </summary>
public static class ResultOfTSequenceAsyncExtensions
{
    /// <summary>Projects each item asynchronously and keeps the successes or returns the first failure.</summary>
    /// <typeparam name="T">The source item type.</typeparam>
    /// <typeparam name="TValue">The projected value type.</typeparam>
    /// <param name="source">The source items.</param>
    /// <param name="selector">The asynchronous result projection.</param>
    /// <returns>The projected values, the first failure, or an uninitialized result.</returns>
    public static async Task<Result<IReadOnlyList<TValue>>> TraverseAsync<T, TValue>(
        this IEnumerable<T> source,
        Func<T, Task<Result<TValue>>> selector) where TValue : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(selector);

        var values = new List<TValue>();

        foreach (var item in source)
        {
            var result = await selector(item).ConfigureAwait(false);

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

    /// <summary>Keeps the success values or returns the first failure from asynchronous results.</summary>
    /// <typeparam name="TValue">The success value type.</typeparam>
    /// <param name="source">The asynchronous results.</param>
    /// <returns>The success values, the first failure, or an uninitialized result.</returns>
    public static async Task<Result<IReadOnlyList<TValue>>> SequenceAsync<TValue>(
        this IEnumerable<Task<Result<TValue>>> source) where TValue : notnull
    {
        ArgumentNullException.ThrowIfNull(source);

        var values = new List<TValue>();

        foreach (var task in source)
        {
            var result = await task.ConfigureAwait(false);

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
