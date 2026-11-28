namespace LayeredCraft.Toolkit.Functional;

/// <summary>
/// Asynchronous composition helpers for <see cref="Result{TValue}"/>. Every helper preserves the
/// default <see cref="Error.Uninitialized"/> failure unless the helper is explicitly terminal.
/// </summary>
public static class ResultOfTAsyncExtensions
{
    /// <summary>Transforms the failure error asynchronously and passes successes through.</summary>
    /// <typeparam name="TValue">The success value type.</typeparam>
    /// <param name="result">The result to transform.</param>
    /// <param name="mapError">The asynchronous error transformation.</param>
    public static async Task<Result<TValue>> MapErrorAsync<TValue>(
        this Result<TValue> result,
        Func<Error, Task<Error>> mapError) where TValue : notnull
    {
        ArgumentNullException.ThrowIfNull(mapError);

        if (result.IsUninitialized)
            return default;

        if (result.IsSuccess)
            return result;

        return Result<TValue>.Fail(await mapError(result.Error).ConfigureAwait(false));
    }

    /// <summary>Transforms the failure error asynchronously and passes successes through.</summary>
    /// <typeparam name="TValue">The success value type.</typeparam>
    /// <param name="result">The asynchronous result to transform.</param>
    /// <param name="mapError">The asynchronous error transformation.</param>
    public static async Task<Result<TValue>> MapErrorAsync<TValue>(
        this Task<Result<TValue>> result,
        Func<Error, Task<Error>> mapError) where TValue : notnull
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(mapError);

        var value = await result.ConfigureAwait(false);
        return await value.MapErrorAsync(mapError).ConfigureAwait(false);
    }

    /// <summary>Converts a success into a failure when an asynchronous predicate fails.</summary>
    /// <typeparam name="TValue">The success value type.</typeparam>
    /// <param name="result">The result to test.</param>
    /// <param name="predicate">The asynchronous predicate the success value must satisfy.</param>
    /// <param name="error">The error to return when the predicate fails.</param>
    public static async Task<Result<TValue>> EnsureAsync<TValue>(
        this Result<TValue> result,
        Func<TValue, Task<bool>> predicate,
        Error error) where TValue : notnull
    {
        ArgumentNullException.ThrowIfNull(predicate);

        if (result.IsUninitialized)
            return default;

        if (!result.IsSuccess)
            return result;

        return await predicate(result.Value).ConfigureAwait(false)
            ? result
            : Result<TValue>.Fail(error);
    }

    /// <summary>Converts a success into a failure when an asynchronous predicate fails.</summary>
    /// <typeparam name="TValue">The success value type.</typeparam>
    /// <param name="result">The result to test.</param>
    /// <param name="predicate">The asynchronous predicate the success value must satisfy.</param>
    /// <param name="errorFactory">Creates the error from the rejected value.</param>
    public static async Task<Result<TValue>> EnsureAsync<TValue>(
        this Result<TValue> result,
        Func<TValue, Task<bool>> predicate,
        Func<TValue, Error> errorFactory) where TValue : notnull
    {
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentNullException.ThrowIfNull(errorFactory);

        if (result.IsUninitialized)
            return default;

        if (!result.IsSuccess)
            return result;

        return await predicate(result.Value).ConfigureAwait(false)
            ? result
            : Result<TValue>.Fail(errorFactory(result.Value));
    }

    /// <summary>Converts a success into a failure when an asynchronous predicate fails.</summary>
    /// <typeparam name="TValue">The success value type.</typeparam>
    /// <param name="result">The asynchronous result to test.</param>
    /// <param name="predicate">The asynchronous predicate the success value must satisfy.</param>
    /// <param name="error">The error to return when the predicate fails.</param>
    public static async Task<Result<TValue>> EnsureAsync<TValue>(
        this Task<Result<TValue>> result,
        Func<TValue, Task<bool>> predicate,
        Error error) where TValue : notnull
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(predicate);

        var value = await result.ConfigureAwait(false);
        return await value.EnsureAsync(predicate, error).ConfigureAwait(false);
    }

    /// <summary>Converts a success into a failure when an asynchronous predicate fails.</summary>
    /// <typeparam name="TValue">The success value type.</typeparam>
    /// <param name="result">The asynchronous result to test.</param>
    /// <param name="predicate">The asynchronous predicate the success value must satisfy.</param>
    /// <param name="errorFactory">Creates the error from the rejected value.</param>
    public static async Task<Result<TValue>> EnsureAsync<TValue>(
        this Task<Result<TValue>> result,
        Func<TValue, Task<bool>> predicate,
        Func<TValue, Error> errorFactory) where TValue : notnull
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentNullException.ThrowIfNull(errorFactory);

        var value = await result.ConfigureAwait(false);
        return await value.EnsureAsync(predicate, errorFactory).ConfigureAwait(false);
    }

    /// <summary>Runs an asynchronous action on the failure error and returns the original result.</summary>
    /// <typeparam name="TValue">The success value type.</typeparam>
    /// <param name="result">The result to observe.</param>
    /// <param name="tap">The asynchronous action to run.</param>
    public static async Task<Result<TValue>> TapErrorAsync<TValue>(
        this Result<TValue> result,
        Func<Error, Task> tap) where TValue : notnull
    {
        ArgumentNullException.ThrowIfNull(tap);

        if (result.IsFailure)
            await tap(result.Error).ConfigureAwait(false);

        return result;
    }

    /// <summary>Runs an asynchronous action on the failure error and returns the original result.</summary>
    /// <typeparam name="TValue">The success value type.</typeparam>
    /// <param name="result">The asynchronous result to observe.</param>
    /// <param name="tap">The asynchronous action to run.</param>
    public static async Task<Result<TValue>> TapErrorAsync<TValue>(
        this Task<Result<TValue>> result,
        Func<Error, Task> tap) where TValue : notnull
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(tap);

        var value = await result.ConfigureAwait(false);
        return await value.TapErrorAsync(tap).ConfigureAwait(false);
    }

    /// <summary>Returns the success value or an asynchronously derived fallback.</summary>
    /// <typeparam name="TValue">The success value type.</typeparam>
    /// <param name="result">The result to read.</param>
    /// <param name="fallback">Derives the value from the failure error.</param>
    public static async Task<TValue> GetOrElseAsync<TValue>(
        this Result<TValue> result,
        Func<Error, Task<TValue>> fallback) where TValue : notnull
    {
        ArgumentNullException.ThrowIfNull(fallback);

        return result.IsSuccess
            ? result.Value
            : await fallback(result.Error).ConfigureAwait(false);
    }

    /// <summary>Returns the success value or an asynchronously derived fallback.</summary>
    /// <typeparam name="TValue">The success value type.</typeparam>
    /// <param name="result">The asynchronous result to read.</param>
    /// <param name="fallback">Derives the value from the failure error.</param>
    public static async Task<TValue> GetOrElseAsync<TValue>(
        this Task<Result<TValue>> result,
        Func<Error, Task<TValue>> fallback) where TValue : notnull
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(fallback);

        var value = await result.ConfigureAwait(false);
        return await value.GetOrElseAsync(fallback).ConfigureAwait(false);
    }

    /// <summary>Returns the original result or an asynchronously derived result.</summary>
    /// <typeparam name="TValue">The success value type.</typeparam>
    /// <param name="result">The original result.</param>
    /// <param name="fallback">Derives the result from the failure error.</param>
    public static async Task<Result<TValue>> OrElseAsync<TValue>(
        this Result<TValue> result,
        Func<Error, Task<Result<TValue>>> fallback) where TValue : notnull
    {
        ArgumentNullException.ThrowIfNull(fallback);

        return result.IsSuccess
            ? result
            : await fallback(result.Error).ConfigureAwait(false);
    }

    /// <summary>Returns the original result or an asynchronously derived result.</summary>
    /// <typeparam name="TValue">The success value type.</typeparam>
    /// <param name="result">The asynchronous result.</param>
    /// <param name="fallback">Derives the result from the failure error.</param>
    public static async Task<Result<TValue>> OrElseAsync<TValue>(
        this Task<Result<TValue>> result,
        Func<Error, Task<Result<TValue>>> fallback) where TValue : notnull
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(fallback);

        var value = await result.ConfigureAwait(false);
        return await value.OrElseAsync(fallback).ConfigureAwait(false);
    }

    /// <summary>Projects both cases into a single result with asynchronous projections.</summary>
    /// <typeparam name="TValue">The success value type.</typeparam>
    /// <typeparam name="TResult">The projected result type.</typeparam>
    /// <param name="result">The result to project.</param>
    /// <param name="success">Projects the success value.</param>
    /// <param name="failure">Projects the failure error.</param>
    public static async Task<TResult> MatchAsync<TValue, TResult>(
        this Result<TValue> result,
        Func<TValue, Task<TResult>> success,
        Func<Error, Task<TResult>> failure) where TValue : notnull
    {
        ArgumentNullException.ThrowIfNull(success);
        ArgumentNullException.ThrowIfNull(failure);

        return result.IsSuccess
            ? await success(result.Value).ConfigureAwait(false)
            : await failure(result.Error).ConfigureAwait(false);
    }

    /// <summary>Projects both cases into a single result with asynchronous projections.</summary>
    /// <typeparam name="TValue">The success value type.</typeparam>
    /// <typeparam name="TResult">The projected result type.</typeparam>
    /// <param name="result">The asynchronous result to project.</param>
    /// <param name="success">Projects the success value.</param>
    /// <param name="failure">Projects the failure error.</param>
    public static async Task<TResult> MatchAsync<TValue, TResult>(
        this Task<Result<TValue>> result,
        Func<TValue, Task<TResult>> success,
        Func<Error, Task<TResult>> failure) where TValue : notnull
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(success);
        ArgumentNullException.ThrowIfNull(failure);

        var value = await result.ConfigureAwait(false);
        return await value.MatchAsync(success, failure).ConfigureAwait(false);
    }

    /// <summary>Runs one of two asynchronous actions depending on the case.</summary>
    /// <typeparam name="TValue">The success value type.</typeparam>
    /// <param name="result">The result to observe.</param>
    /// <param name="success">Runs on a success.</param>
    /// <param name="failure">Runs on a failure.</param>
    public static async Task SwitchAsync<TValue>(
        this Result<TValue> result,
        Func<TValue, Task> success,
        Func<Error, Task> failure) where TValue : notnull
    {
        ArgumentNullException.ThrowIfNull(success);
        ArgumentNullException.ThrowIfNull(failure);

        if (result.IsSuccess)
            await success(result.Value).ConfigureAwait(false);
        else
            await failure(result.Error).ConfigureAwait(false);
    }

    /// <summary>Runs one of two asynchronous actions depending on the case.</summary>
    /// <typeparam name="TValue">The success value type.</typeparam>
    /// <param name="result">The asynchronous result to observe.</param>
    /// <param name="success">Runs on a success.</param>
    /// <param name="failure">Runs on a failure.</param>
    public static async Task SwitchAsync<TValue>(
        this Task<Result<TValue>> result,
        Func<TValue, Task> success,
        Func<Error, Task> failure) where TValue : notnull
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(success);
        ArgumentNullException.ThrowIfNull(failure);

        var value = await result.ConfigureAwait(false);
        await value.SwitchAsync(success, failure).ConfigureAwait(false);
    }

    /// <summary>Checks whether a success value satisfies an asynchronous predicate.</summary>
    /// <typeparam name="TValue">The success value type.</typeparam>
    /// <param name="result">The result to test.</param>
    /// <param name="predicate">The asynchronous predicate to test.</param>
    public static async Task<bool> ExistsAsync<TValue>(
        this Result<TValue> result,
        Func<TValue, Task<bool>> predicate) where TValue : notnull
    {
        ArgumentNullException.ThrowIfNull(predicate);

        return result.IsSuccess
            && await predicate(result.Value).ConfigureAwait(false);
    }

    /// <summary>Checks whether a success value satisfies an asynchronous predicate.</summary>
    /// <typeparam name="TValue">The success value type.</typeparam>
    /// <param name="result">The asynchronous result to test.</param>
    /// <param name="predicate">The asynchronous predicate to test.</param>
    public static async Task<bool> ExistsAsync<TValue>(
        this Task<Result<TValue>> result,
        Func<TValue, Task<bool>> predicate) where TValue : notnull
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(predicate);

        var value = await result.ConfigureAwait(false);
        return await value.ExistsAsync(predicate).ConfigureAwait(false);
    }

    /// <summary>Checks whether the result is a failure or the success value satisfies an asynchronous predicate.</summary>
    /// <typeparam name="TValue">The success value type.</typeparam>
    /// <param name="result">The result to test.</param>
    /// <param name="predicate">The asynchronous predicate to test.</param>
    public static async Task<bool> ForAllAsync<TValue>(
        this Result<TValue> result,
        Func<TValue, Task<bool>> predicate) where TValue : notnull
    {
        ArgumentNullException.ThrowIfNull(predicate);

        return !result.IsSuccess
            || await predicate(result.Value).ConfigureAwait(false);
    }

    /// <summary>Checks whether the result is a failure or the success value satisfies an asynchronous predicate.</summary>
    /// <typeparam name="TValue">The success value type.</typeparam>
    /// <param name="result">The asynchronous result to test.</param>
    /// <param name="predicate">The asynchronous predicate to test.</param>
    public static async Task<bool> ForAllAsync<TValue>(
        this Task<Result<TValue>> result,
        Func<TValue, Task<bool>> predicate) where TValue : notnull
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(predicate);

        var value = await result.ConfigureAwait(false);
        return await value.ForAllAsync(predicate).ConfigureAwait(false);
    }

    /// <summary>Checks whether an asynchronous result is a success containing a candidate.</summary>
    /// <typeparam name="TValue">The success value type.</typeparam>
    /// <param name="result">The asynchronous result.</param>
    /// <param name="candidate">The candidate value.</param>
    public static async Task<bool> ContainsAsync<TValue>(
        this Task<Result<TValue>> result,
        TValue candidate) where TValue : notnull
    {
        ArgumentNullException.ThrowIfNull(result);

        var value = await result.ConfigureAwait(false);
        return value.IsSuccess
            && EqualityComparer<TValue>.Default.Equals(value.Value, candidate);
    }
}
