namespace LayeredCraft.Toolkit.Functional;

/// <summary>
/// Asynchronous composition helpers for <see cref="Result{TValue,TError}"/>. Composition helpers
/// preserve the uninitialized default; terminal helpers throw on it.
/// </summary>
public static class ResultAsyncExtensions
{
    /// <summary>Transforms the failure error asynchronously and passes successes through.</summary>
    /// <typeparam name="TValue">The success value type.</typeparam>
    /// <typeparam name="TError">The failure error type.</typeparam>
    /// <typeparam name="TNextError">The transformed error type.</typeparam>
    /// <param name="result">The result to transform.</param>
    /// <param name="mapError">The asynchronous error transformation.</param>
    public static async Task<Result<TValue, TNextError>> MapErrorAsync<TValue, TError, TNextError>(
        this Result<TValue, TError> result,
        Func<TError, Task<TNextError>> mapError)
        where TValue : notnull where TError : notnull where TNextError : notnull
    {
        ArgumentNullException.ThrowIfNull(mapError);

        if (!result.IsInitialized)
            return default;

        if (result.IsSuccess)
            return Result<TValue, TNextError>.Ok(result.Value);

        return Result<TValue, TNextError>.Fail(
            await mapError(result.Error).ConfigureAwait(false));
    }

    /// <summary>Transforms the failure error asynchronously and passes successes through.</summary>
    /// <typeparam name="TValue">The success value type.</typeparam>
    /// <typeparam name="TError">The failure error type.</typeparam>
    /// <typeparam name="TNextError">The transformed error type.</typeparam>
    /// <param name="result">The asynchronous result to transform.</param>
    /// <param name="mapError">The asynchronous error transformation.</param>
    public static async Task<Result<TValue, TNextError>> MapErrorAsync<TValue, TError, TNextError>(
        this Task<Result<TValue, TError>> result,
        Func<TError, Task<TNextError>> mapError)
        where TValue : notnull where TError : notnull where TNextError : notnull
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(mapError);

        var value = await result.ConfigureAwait(false);
        return await value.MapErrorAsync(mapError).ConfigureAwait(false);
    }

    /// <summary>Converts a success into a failure when an asynchronous predicate fails.</summary>
    /// <typeparam name="TValue">The success value type.</typeparam>
    /// <typeparam name="TError">The failure error type.</typeparam>
    /// <param name="result">The result to check.</param>
    /// <param name="predicate">The asynchronous predicate the success value must satisfy.</param>
    /// <param name="error">The error to return when the predicate fails.</param>
    public static async Task<Result<TValue, TError>> EnsureAsync<TValue, TError>(
        this Result<TValue, TError> result,
        Func<TValue, Task<bool>> predicate,
        TError error) where TValue : notnull where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(predicate);

        if (!result.IsInitialized)
            return default;

        if (!result.IsSuccess)
            return result;

        return await predicate(result.Value).ConfigureAwait(false)
            ? result
            : Result<TValue, TError>.Fail(error);
    }

    /// <summary>Converts a success into a failure when an asynchronous predicate fails.</summary>
    /// <typeparam name="TValue">The success value type.</typeparam>
    /// <typeparam name="TError">The failure error type.</typeparam>
    /// <param name="result">The result to check.</param>
    /// <param name="predicate">The asynchronous predicate the success value must satisfy.</param>
    /// <param name="errorFactory">Creates the error from the rejected value.</param>
    public static async Task<Result<TValue, TError>> EnsureAsync<TValue, TError>(
        this Result<TValue, TError> result,
        Func<TValue, Task<bool>> predicate,
        Func<TValue, TError> errorFactory) where TValue : notnull where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentNullException.ThrowIfNull(errorFactory);

        if (!result.IsInitialized)
            return default;

        if (!result.IsSuccess)
            return result;

        return await predicate(result.Value).ConfigureAwait(false)
            ? result
            : Result<TValue, TError>.Fail(errorFactory(result.Value));
    }

    /// <summary>Converts a success into a failure when an asynchronous predicate fails.</summary>
    /// <typeparam name="TValue">The success value type.</typeparam>
    /// <typeparam name="TError">The failure error type.</typeparam>
    /// <param name="result">The asynchronous result to check.</param>
    /// <param name="predicate">The asynchronous predicate the success value must satisfy.</param>
    /// <param name="error">The error to return when the predicate fails.</param>
    public static async Task<Result<TValue, TError>> EnsureAsync<TValue, TError>(
        this Task<Result<TValue, TError>> result,
        Func<TValue, Task<bool>> predicate,
        TError error) where TValue : notnull where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(predicate);

        var value = await result.ConfigureAwait(false);
        return await value.EnsureAsync(predicate, error).ConfigureAwait(false);
    }

    /// <summary>Converts a success into a failure when an asynchronous predicate fails.</summary>
    /// <typeparam name="TValue">The success value type.</typeparam>
    /// <typeparam name="TError">The failure error type.</typeparam>
    /// <param name="result">The asynchronous result to check.</param>
    /// <param name="predicate">The asynchronous predicate the success value must satisfy.</param>
    /// <param name="errorFactory">Creates the error from the rejected value.</param>
    public static async Task<Result<TValue, TError>> EnsureAsync<TValue, TError>(
        this Task<Result<TValue, TError>> result,
        Func<TValue, Task<bool>> predicate,
        Func<TValue, TError> errorFactory) where TValue : notnull where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentNullException.ThrowIfNull(errorFactory);

        var value = await result.ConfigureAwait(false);
        return await value.EnsureAsync(predicate, errorFactory).ConfigureAwait(false);
    }

    /// <summary>Runs an asynchronous action on the failure error and returns the original result.</summary>
    /// <typeparam name="TValue">The success value type.</typeparam>
    /// <typeparam name="TError">The failure error type.</typeparam>
    /// <param name="result">The result to observe.</param>
    /// <param name="tap">The asynchronous action to run.</param>
    public static async Task<Result<TValue, TError>> TapErrorAsync<TValue, TError>(
        this Result<TValue, TError> result,
        Func<TError, Task> tap) where TValue : notnull where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(tap);

        if (result.TryGetError(out var error))
            await tap(error).ConfigureAwait(false);

        return result;
    }

    /// <summary>Runs an asynchronous action on the failure error and returns the original result.</summary>
    /// <typeparam name="TValue">The success value type.</typeparam>
    /// <typeparam name="TError">The failure error type.</typeparam>
    /// <param name="result">The asynchronous result to observe.</param>
    /// <param name="tap">The asynchronous action to run.</param>
    public static async Task<Result<TValue, TError>> TapErrorAsync<TValue, TError>(
        this Task<Result<TValue, TError>> result,
        Func<TError, Task> tap) where TValue : notnull where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(tap);

        var value = await result.ConfigureAwait(false);
        return await value.TapErrorAsync(tap).ConfigureAwait(false);
    }

    /// <summary>Returns the success value or an asynchronously derived fallback.</summary>
    /// <typeparam name="TValue">The success value type.</typeparam>
    /// <typeparam name="TError">The failure error type.</typeparam>
    /// <param name="result">The result to read.</param>
    /// <param name="fallback">Derives the value from the failure error.</param>
    /// <exception cref="InvalidOperationException">The result is uninitialized.</exception>
    public static async Task<TValue> GetOrElseAsync<TValue, TError>(
        this Result<TValue, TError> result,
        Func<TError, Task<TValue>> fallback) where TValue : notnull where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(fallback);

        if (result.IsSuccess)
            return result.Value;

        ThrowIfUninitialized(result);
        return await fallback(result.Error).ConfigureAwait(false);
    }

    /// <summary>Returns the success value or an asynchronously derived fallback.</summary>
    /// <typeparam name="TValue">The success value type.</typeparam>
    /// <typeparam name="TError">The failure error type.</typeparam>
    /// <param name="result">The asynchronous result to read.</param>
    /// <param name="fallback">Derives the value from the failure error.</param>
    /// <exception cref="InvalidOperationException">The result is uninitialized.</exception>
    public static async Task<TValue> GetOrElseAsync<TValue, TError>(
        this Task<Result<TValue, TError>> result,
        Func<TError, Task<TValue>> fallback) where TValue : notnull where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(fallback);

        var value = await result.ConfigureAwait(false);
        return await value.GetOrElseAsync(fallback).ConfigureAwait(false);
    }

    /// <summary>Returns the original result or an asynchronously derived result.</summary>
    /// <typeparam name="TValue">The success value type.</typeparam>
    /// <typeparam name="TError">The failure error type.</typeparam>
    /// <param name="result">The original result.</param>
    /// <param name="fallback">Derives the result from the failure error.</param>
    /// <exception cref="InvalidOperationException">The result is uninitialized.</exception>
    public static async Task<Result<TValue, TError>> OrElseAsync<TValue, TError>(
        this Result<TValue, TError> result,
        Func<TError, Task<Result<TValue, TError>>> fallback)
        where TValue : notnull where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(fallback);

        if (result.IsSuccess)
            return result;

        ThrowIfUninitialized(result);
        return await fallback(result.Error).ConfigureAwait(false);
    }

    /// <summary>Returns the original result or an asynchronously derived result.</summary>
    /// <typeparam name="TValue">The success value type.</typeparam>
    /// <typeparam name="TError">The failure error type.</typeparam>
    /// <param name="result">The asynchronous result.</param>
    /// <param name="fallback">Derives the result from the failure error.</param>
    /// <exception cref="InvalidOperationException">The result is uninitialized.</exception>
    public static async Task<Result<TValue, TError>> OrElseAsync<TValue, TError>(
        this Task<Result<TValue, TError>> result,
        Func<TError, Task<Result<TValue, TError>>> fallback)
        where TValue : notnull where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(fallback);

        var value = await result.ConfigureAwait(false);
        return await value.OrElseAsync(fallback).ConfigureAwait(false);
    }

    /// <summary>Projects both cases into a single result with asynchronous projections.</summary>
    /// <typeparam name="TValue">The success value type.</typeparam>
    /// <typeparam name="TError">The failure error type.</typeparam>
    /// <typeparam name="TResult">The projected result type.</typeparam>
    /// <param name="result">The result to project.</param>
    /// <param name="success">Projects the success value.</param>
    /// <param name="failure">Projects the failure error.</param>
    /// <exception cref="InvalidOperationException">The result is uninitialized.</exception>
    public static async Task<TResult> MatchAsync<TValue, TError, TResult>(
        this Result<TValue, TError> result,
        Func<TValue, Task<TResult>> success,
        Func<TError, Task<TResult>> failure)
        where TValue : notnull where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(success);
        ArgumentNullException.ThrowIfNull(failure);

        if (result.IsSuccess)
            return await success(result.Value).ConfigureAwait(false);

        ThrowIfUninitialized(result);
        return await failure(result.Error).ConfigureAwait(false);
    }

    /// <summary>Projects both cases into a single result with asynchronous projections.</summary>
    /// <typeparam name="TValue">The success value type.</typeparam>
    /// <typeparam name="TError">The failure error type.</typeparam>
    /// <typeparam name="TResult">The projected result type.</typeparam>
    /// <param name="result">The asynchronous result to project.</param>
    /// <param name="success">Projects the success value.</param>
    /// <param name="failure">Projects the failure error.</param>
    /// <exception cref="InvalidOperationException">The result is uninitialized.</exception>
    public static async Task<TResult> MatchAsync<TValue, TError, TResult>(
        this Task<Result<TValue, TError>> result,
        Func<TValue, Task<TResult>> success,
        Func<TError, Task<TResult>> failure)
        where TValue : notnull where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(success);
        ArgumentNullException.ThrowIfNull(failure);

        var value = await result.ConfigureAwait(false);
        return await value.MatchAsync(success, failure).ConfigureAwait(false);
    }

    /// <summary>Runs one of two asynchronous actions depending on the case.</summary>
    /// <typeparam name="TValue">The success value type.</typeparam>
    /// <typeparam name="TError">The failure error type.</typeparam>
    /// <param name="result">The result to observe.</param>
    /// <param name="success">Runs on a success.</param>
    /// <param name="failure">Runs on a failure.</param>
    /// <exception cref="InvalidOperationException">The result is uninitialized.</exception>
    public static async Task SwitchAsync<TValue, TError>(
        this Result<TValue, TError> result,
        Func<TValue, Task> success,
        Func<TError, Task> failure) where TValue : notnull where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(success);
        ArgumentNullException.ThrowIfNull(failure);

        if (result.IsSuccess)
        {
            await success(result.Value).ConfigureAwait(false);
            return;
        }

        ThrowIfUninitialized(result);
        await failure(result.Error).ConfigureAwait(false);
    }

    /// <summary>Runs one of two asynchronous actions depending on the case.</summary>
    /// <typeparam name="TValue">The success value type.</typeparam>
    /// <typeparam name="TError">The failure error type.</typeparam>
    /// <param name="result">The asynchronous result to observe.</param>
    /// <param name="success">Runs on a success.</param>
    /// <param name="failure">Runs on a failure.</param>
    /// <exception cref="InvalidOperationException">The result is uninitialized.</exception>
    public static async Task SwitchAsync<TValue, TError>(
        this Task<Result<TValue, TError>> result,
        Func<TValue, Task> success,
        Func<TError, Task> failure) where TValue : notnull where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(success);
        ArgumentNullException.ThrowIfNull(failure);

        var value = await result.ConfigureAwait(false);
        await value.SwitchAsync(success, failure).ConfigureAwait(false);
    }

    /// <summary>Checks whether a success value satisfies an asynchronous predicate.</summary>
    /// <typeparam name="TValue">The success value type.</typeparam>
    /// <typeparam name="TError">The failure error type.</typeparam>
    /// <param name="result">The result to test.</param>
    /// <param name="predicate">The asynchronous predicate to test.</param>
    /// <exception cref="InvalidOperationException">The result is uninitialized.</exception>
    public static async Task<bool> ExistsAsync<TValue, TError>(
        this Result<TValue, TError> result,
        Func<TValue, Task<bool>> predicate) where TValue : notnull where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(predicate);

        if (result.IsSuccess)
            return await predicate(result.Value).ConfigureAwait(false);

        ThrowIfUninitialized(result);
        return false;
    }

    /// <summary>Checks whether a success value satisfies an asynchronous predicate.</summary>
    /// <typeparam name="TValue">The success value type.</typeparam>
    /// <typeparam name="TError">The failure error type.</typeparam>
    /// <param name="result">The asynchronous result to test.</param>
    /// <param name="predicate">The asynchronous predicate to test.</param>
    /// <exception cref="InvalidOperationException">The result is uninitialized.</exception>
    public static async Task<bool> ExistsAsync<TValue, TError>(
        this Task<Result<TValue, TError>> result,
        Func<TValue, Task<bool>> predicate) where TValue : notnull where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(predicate);

        var value = await result.ConfigureAwait(false);
        return await value.ExistsAsync(predicate).ConfigureAwait(false);
    }

    /// <summary>Checks whether the result is a failure or the success value satisfies an asynchronous predicate.</summary>
    /// <typeparam name="TValue">The success value type.</typeparam>
    /// <typeparam name="TError">The failure error type.</typeparam>
    /// <param name="result">The result to test.</param>
    /// <param name="predicate">The asynchronous predicate to test.</param>
    /// <exception cref="InvalidOperationException">The result is uninitialized.</exception>
    public static async Task<bool> ForAllAsync<TValue, TError>(
        this Result<TValue, TError> result,
        Func<TValue, Task<bool>> predicate) where TValue : notnull where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(predicate);

        if (result.IsSuccess)
            return await predicate(result.Value).ConfigureAwait(false);

        ThrowIfUninitialized(result);
        return true;
    }

    /// <summary>Checks whether the result is a failure or the success value satisfies an asynchronous predicate.</summary>
    /// <typeparam name="TValue">The success value type.</typeparam>
    /// <typeparam name="TError">The failure error type.</typeparam>
    /// <param name="result">The asynchronous result to test.</param>
    /// <param name="predicate">The asynchronous predicate to test.</param>
    /// <exception cref="InvalidOperationException">The result is uninitialized.</exception>
    public static async Task<bool> ForAllAsync<TValue, TError>(
        this Task<Result<TValue, TError>> result,
        Func<TValue, Task<bool>> predicate) where TValue : notnull where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(predicate);

        var value = await result.ConfigureAwait(false);
        return await value.ForAllAsync(predicate).ConfigureAwait(false);
    }

    /// <summary>Checks whether an asynchronous result is a success containing a candidate.</summary>
    /// <typeparam name="TValue">The success value type.</typeparam>
    /// <typeparam name="TError">The failure error type.</typeparam>
    /// <param name="result">The asynchronous result.</param>
    /// <param name="candidate">The candidate value.</param>
    /// <exception cref="InvalidOperationException">The result is uninitialized.</exception>
    public static async Task<bool> ContainsAsync<TValue, TError>(
        this Task<Result<TValue, TError>> result,
        TValue candidate) where TValue : notnull where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(result);

        var value = await result.ConfigureAwait(false);

        if (value.IsSuccess)
            return EqualityComparer<TValue>.Default.Equals(value.Value, candidate);

        ThrowIfUninitialized(value);
        return false;
    }

    private static void ThrowIfUninitialized<TValue, TError>(Result<TValue, TError> result)
        where TValue : notnull where TError : notnull
    {
        if (!result.IsInitialized)
            throw new InvalidOperationException(
                "The result is uninitialized. Create one with Result<TValue, TError>.Ok or Fail.");
    }
}
