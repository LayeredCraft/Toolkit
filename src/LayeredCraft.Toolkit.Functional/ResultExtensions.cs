namespace LayeredCraft.Toolkit.Functional;

/// <summary>
/// Composition helpers for <see cref="Result{TValue,TError}"/>. Each helper short-circuits on
/// failure and leaves the failure payload unchanged unless the helper says otherwise.
/// </summary>
public static class ResultExtensions
{
    /// <summary>Transforms the success value and passes failures through.</summary>
    /// <typeparam name="TValue">The success value type.</typeparam>
    /// <typeparam name="TNext">The transformed value type.</typeparam>
    /// <typeparam name="TError">The failure error type.</typeparam>
    /// <param name="result">The result to transform.</param>
    /// <param name="map">The transformation.</param>
    public static Result<TNext, TError> Map<TValue, TNext, TError>(
        this Result<TValue, TError> result,
        Func<TValue, TNext> map)
    {
        ArgumentNullException.ThrowIfNull(map);

        return result.Match(
            value => Result<TNext, TError>.Ok(map(value)),
            error => Result<TNext, TError>.Fail(error));
    }

    /// <summary>Chains an operation that can also fail.</summary>
    /// <typeparam name="TValue">The success value type.</typeparam>
    /// <typeparam name="TNext">The chained value type.</typeparam>
    /// <typeparam name="TError">The failure error type.</typeparam>
    /// <param name="result">The result to chain.</param>
    /// <param name="bind">The operation to run on success.</param>
    public static Result<TNext, TError> Bind<TValue, TNext, TError>(
        this Result<TValue, TError> result,
        Func<TValue, Result<TNext, TError>> bind)
    {
        ArgumentNullException.ThrowIfNull(bind);

        return result.Match(
            bind,
            error => Result<TNext, TError>.Fail(error));
    }

    /// <summary>Transforms the failure error and passes successes through.</summary>
    /// <typeparam name="TValue">The success value type.</typeparam>
    /// <typeparam name="TError">The failure error type.</typeparam>
    /// <typeparam name="TNextError">The transformed error type.</typeparam>
    /// <param name="result">The result to transform.</param>
    /// <param name="mapError">The error transformation.</param>
    public static Result<TValue, TNextError> MapError<TValue, TError, TNextError>(
        this Result<TValue, TError> result,
        Func<TError, TNextError> mapError)
    {
        ArgumentNullException.ThrowIfNull(mapError);

        return result.Match(
            value => Result<TValue, TNextError>.Ok(value),
            error => Result<TValue, TNextError>.Fail(mapError(error)));
    }

    /// <summary>Runs an action on the success value and returns the original result.</summary>
    /// <typeparam name="TValue">The success value type.</typeparam>
    /// <typeparam name="TError">The failure error type.</typeparam>
    /// <param name="result">The result to observe.</param>
    /// <param name="tap">The action to run on success.</param>
    public static Result<TValue, TError> Tap<TValue, TError>(
        this Result<TValue, TError> result,
        Action<TValue> tap)
    {
        ArgumentNullException.ThrowIfNull(tap);

        if (result.TryGetValue(out var value))
            tap(value);

        return result;
    }

    /// <summary>Runs an action on the failure error and returns the original result.</summary>
    /// <typeparam name="TValue">The success value type.</typeparam>
    /// <typeparam name="TError">The failure error type.</typeparam>
    /// <param name="result">The result to observe.</param>
    /// <param name="tap">The action to run on failure.</param>
    public static Result<TValue, TError> TapError<TValue, TError>(
        this Result<TValue, TError> result,
        Action<TError> tap)
    {
        ArgumentNullException.ThrowIfNull(tap);

        if (result.TryGetError(out var error))
            tap(error);

        return result;
    }

    /// <summary>Converts a success into a failure when a predicate does not hold.</summary>
    /// <typeparam name="TValue">The success value type.</typeparam>
    /// <typeparam name="TError">The failure error type.</typeparam>
    /// <param name="result">The result to check.</param>
    /// <param name="predicate">The predicate the success value must satisfy.</param>
    /// <param name="error">The error to return when the predicate fails.</param>
    public static Result<TValue, TError> Ensure<TValue, TError>(
        this Result<TValue, TError> result,
        Func<TValue, bool> predicate,
        TError error)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        return result.TryGetValue(out var value) && !predicate(value)
            ? Result<TValue, TError>.Fail(error)
            : result;
    }

    /// <summary>Converts a success into a failure when a predicate does not hold.</summary>
    /// <typeparam name="TValue">The success value type.</typeparam>
    /// <typeparam name="TError">The failure error type.</typeparam>
    /// <param name="result">The result to check.</param>
    /// <param name="predicate">The predicate the success value must satisfy.</param>
    /// <param name="errorFactory">Creates the error from the rejected value.</param>
    public static Result<TValue, TError> Ensure<TValue, TError>(
        this Result<TValue, TError> result,
        Func<TValue, bool> predicate,
        Func<TValue, TError> errorFactory)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentNullException.ThrowIfNull(errorFactory);

        return result.TryGetValue(out var value) && !predicate(value)
            ? Result<TValue, TError>.Fail(errorFactory(value))
            : result;
    }

    /// <summary>Returns the success value or a fallback.</summary>
    /// <typeparam name="TValue">The success value type.</typeparam>
    /// <typeparam name="TError">The failure error type.</typeparam>
    /// <param name="result">The result to read.</param>
    /// <param name="fallback">The value to return on failure.</param>
    public static TValue GetOrElse<TValue, TError>(
        this Result<TValue, TError> result,
        TValue fallback)
        => result.TryGetValue(out var value) ? value : fallback;

    /// <summary>Returns the success value or derives one from the failure error.</summary>
    /// <typeparam name="TValue">The success value type.</typeparam>
    /// <typeparam name="TError">The failure error type.</typeparam>
    /// <param name="result">The result to read.</param>
    /// <param name="fallback">Derives the value from the failure error.</param>
    public static TValue GetOrElse<TValue, TError>(
        this Result<TValue, TError> result,
        Func<TError, TValue> fallback)
    {
        ArgumentNullException.ThrowIfNull(fallback);

        return result.TryGetValue(out var value)
            ? value
            : fallback(result.Error);
    }

    /// <summary>Returns the original result or another result.</summary>
    /// <typeparam name="TValue">The success value type.</typeparam>
    /// <typeparam name="TError">The failure error type.</typeparam>
    /// <param name="result">The result to read.</param>
    /// <param name="fallback">The result to use on failure.</param>
    public static Result<TValue, TError> OrElse<TValue, TError>(
        this Result<TValue, TError> result,
        Result<TValue, TError> fallback)
        => result.IsSuccess ? result : fallback;

    /// <summary>Returns the original result or derives another result from the failure error.</summary>
    /// <typeparam name="TValue">The success value type.</typeparam>
    /// <typeparam name="TError">The failure error type.</typeparam>
    /// <param name="result">The result to read.</param>
    /// <param name="fallback">Derives the result from the failure error.</param>
    public static Result<TValue, TError> OrElse<TValue, TError>(
        this Result<TValue, TError> result,
        Func<TError, Result<TValue, TError>> fallback)
    {
        ArgumentNullException.ThrowIfNull(fallback);

        return result.IsSuccess ? result : fallback(result.Error);
    }

    /// <summary>Converts a success into a present optional value; failures become empty.</summary>
    /// <typeparam name="TValue">The success value type.</typeparam>
    /// <typeparam name="TError">The failure error type.</typeparam>
    /// <param name="result">The result to convert.</param>
    public static Maybe<TValue> ToMaybe<TValue, TError>(this Result<TValue, TError> result)
        where TValue : notnull
        => result.TryGetValue(out var value) ? Maybe<TValue>.Some(value) : Maybe<TValue>.None;

    /// <summary>Enumerates zero or one success values.</summary>
    /// <typeparam name="TValue">The success value type.</typeparam>
    /// <typeparam name="TError">The failure error type.</typeparam>
    /// <param name="result">The result to enumerate.</param>
    public static IEnumerable<TValue> ToEnumerable<TValue, TError>(
        this Result<TValue, TError> result)
    {
        if (result.TryGetValue(out var value))
            yield return value;
    }

    /// <summary>Flattens nested results that share the same error type.</summary>
    /// <typeparam name="TValue">The success value type.</typeparam>
    /// <typeparam name="TError">The failure error type.</typeparam>
    /// <param name="result">The nested result.</param>
    public static Result<TValue, TError> Flatten<TValue, TError>(
        this Result<Result<TValue, TError>, TError> result)
        => result.Bind(static inner => inner);

    /// <summary>Transforms the success value asynchronously.</summary>
    /// <typeparam name="TValue">The success value type.</typeparam>
    /// <typeparam name="TNext">The transformed value type.</typeparam>
    /// <typeparam name="TError">The failure error type.</typeparam>
    /// <param name="result">The result to transform.</param>
    /// <param name="map">The asynchronous transformation.</param>
    public static async Task<Result<TNext, TError>> MapAsync<TValue, TNext, TError>(
        this Result<TValue, TError> result,
        Func<TValue, Task<TNext>> map)
    {
        ArgumentNullException.ThrowIfNull(map);

        if (result.TryGetError(out var error))
            return Result<TNext, TError>.Fail(error);

        return Result<TNext, TError>.Ok(await map(result.Value).ConfigureAwait(false));
    }

    /// <summary>Chains an asynchronous operation that can also fail.</summary>
    /// <typeparam name="TValue">The success value type.</typeparam>
    /// <typeparam name="TNext">The chained value type.</typeparam>
    /// <typeparam name="TError">The failure error type.</typeparam>
    /// <param name="result">The result to chain.</param>
    /// <param name="bind">The asynchronous operation.</param>
    public static async Task<Result<TNext, TError>> BindAsync<TValue, TNext, TError>(
        this Result<TValue, TError> result,
        Func<TValue, Task<Result<TNext, TError>>> bind)
    {
        ArgumentNullException.ThrowIfNull(bind);

        return result.TryGetValue(out var value)
            ? await bind(value).ConfigureAwait(false)
            : Result<TNext, TError>.Fail(result.Error);
    }

    /// <summary>Runs an asynchronous action on the success value.</summary>
    /// <typeparam name="TValue">The success value type.</typeparam>
    /// <typeparam name="TError">The failure error type.</typeparam>
    /// <param name="result">The result to observe.</param>
    /// <param name="tap">The asynchronous action.</param>
    public static async Task<Result<TValue, TError>> TapAsync<TValue, TError>(
        this Result<TValue, TError> result,
        Func<TValue, Task> tap)
    {
        ArgumentNullException.ThrowIfNull(tap);

        if (result.TryGetValue(out var value))
            await tap(value).ConfigureAwait(false);

        return result;
    }

    /// <summary>Transforms the success value of an asynchronous result.</summary>
    /// <typeparam name="TValue">The success value type.</typeparam>
    /// <typeparam name="TNext">The transformed value type.</typeparam>
    /// <typeparam name="TError">The failure error type.</typeparam>
    /// <param name="result">The asynchronous result.</param>
    /// <param name="map">The asynchronous transformation.</param>
    public static async Task<Result<TNext, TError>> MapAsync<TValue, TNext, TError>(
        this Task<Result<TValue, TError>> result,
        Func<TValue, Task<TNext>> map)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(map);

        var value = await result.ConfigureAwait(false);
        return await value.MapAsync(map).ConfigureAwait(false);
    }

    /// <summary>Transforms the success value of an asynchronous result synchronously.</summary>
    /// <typeparam name="TValue">The success value type.</typeparam>
    /// <typeparam name="TNext">The transformed value type.</typeparam>
    /// <typeparam name="TError">The failure error type.</typeparam>
    /// <param name="result">The asynchronous result.</param>
    /// <param name="map">The transformation.</param>
    public static async Task<Result<TNext, TError>> MapAsync<TValue, TNext, TError>(
        this Task<Result<TValue, TError>> result,
        Func<TValue, TNext> map)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(map);

        var value = await result.ConfigureAwait(false);
        return value.Map(map);
    }

    /// <summary>Chains an asynchronous operation from an asynchronous result.</summary>
    /// <typeparam name="TValue">The success value type.</typeparam>
    /// <typeparam name="TNext">The chained value type.</typeparam>
    /// <typeparam name="TError">The failure error type.</typeparam>
    /// <param name="result">The asynchronous result.</param>
    /// <param name="bind">The asynchronous operation.</param>
    public static async Task<Result<TNext, TError>> BindAsync<TValue, TNext, TError>(
        this Task<Result<TValue, TError>> result,
        Func<TValue, Task<Result<TNext, TError>>> bind)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(bind);

        var value = await result.ConfigureAwait(false);
        return await value.BindAsync(bind).ConfigureAwait(false);
    }

    /// <summary>Runs an asynchronous action on the success value of an asynchronous result.</summary>
    /// <typeparam name="TValue">The success value type.</typeparam>
    /// <typeparam name="TError">The failure error type.</typeparam>
    /// <param name="result">The asynchronous result.</param>
    /// <param name="tap">The asynchronous action.</param>
    public static async Task<Result<TValue, TError>> TapAsync<TValue, TError>(
        this Task<Result<TValue, TError>> result,
        Func<TValue, Task> tap)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(tap);

        var value = await result.ConfigureAwait(false);
        return await value.TapAsync(tap).ConfigureAwait(false);
    }
}
