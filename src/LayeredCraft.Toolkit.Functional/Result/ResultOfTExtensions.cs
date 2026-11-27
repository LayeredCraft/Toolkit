namespace LayeredCraft.Toolkit.Functional;

/// <summary>
/// Composition helpers for <see cref="Result{TValue}"/>. Every helper preserves the default
/// <see cref="Error.Uninitialized"/> failure instead of throwing.
/// </summary>
public static class ResultOfTExtensions
{
    /// <summary>Transforms the success value and passes failures through.</summary>
    /// <typeparam name="TValue">The success value type.</typeparam>
    /// <typeparam name="TNext">The transformed value type.</typeparam>
    /// <param name="result">The result to transform.</param>
    /// <param name="map">The transformation.</param>
    public static Result<TNext> Map<TValue, TNext>(this Result<TValue> result, Func<TValue, TNext> map)
        where TValue : notnull where TNext : notnull
        => Result<TNext>.From(result.ToGeneric().Map(map));

    /// <summary>Chains a result projection.</summary>
    /// <typeparam name="TValue">The success value type.</typeparam>
    /// <typeparam name="TNext">The chained value type.</typeparam>
    /// <param name="result">The result to chain.</param>
    /// <param name="bind">The chained projection.</param>
    public static Result<TNext> Bind<TValue, TNext>(
        this Result<TValue> result,
        Func<TValue, Result<TNext>> bind)
        where TValue : notnull where TNext : notnull
        => Result<TNext>.From(result.ToGeneric().Bind(value => bind(value).ToGeneric()));

    /// <summary>Projects the success value; the LINQ query syntax form of <see cref="Map{TValue,TNext}"/>.</summary>
    /// <typeparam name="TValue">The success value type.</typeparam>
    /// <typeparam name="TNext">The transformed value type.</typeparam>
    /// <param name="result">The result to project.</param>
    /// <param name="selector">The projection.</param>
    public static Result<TNext> Select<TValue, TNext>(
        this Result<TValue> result,
        Func<TValue, TNext> selector)
        where TValue : notnull where TNext : notnull
        => result.Map(selector);

    /// <summary>Chains a result projection; the LINQ query syntax form of <see cref="Bind{TValue,TNext}"/>.</summary>
    /// <typeparam name="TValue">The success value type.</typeparam>
    /// <typeparam name="TNext">The chained value type.</typeparam>
    /// <param name="result">The result to chain.</param>
    /// <param name="selector">The result projection.</param>
    public static Result<TNext> SelectMany<TValue, TNext>(
        this Result<TValue> result,
        Func<TValue, Result<TNext>> selector)
        where TValue : notnull where TNext : notnull
        => result.Bind(selector);

    /// <summary>Chains a second result projection and combines both values.</summary>
    /// <typeparam name="TValue">The first success value type.</typeparam>
    /// <typeparam name="TCollection">The second success value type.</typeparam>
    /// <typeparam name="TNext">The combined value type.</typeparam>
    /// <param name="result">The first result.</param>
    /// <param name="collectionSelector">The second result projection.</param>
    /// <param name="resultSelector">Combines both values.</param>
    public static Result<TNext> SelectMany<TValue, TCollection, TNext>(
        this Result<TValue> result,
        Func<TValue, Result<TCollection>> collectionSelector,
        Func<TValue, TCollection, TNext> resultSelector)
        where TValue : notnull where TCollection : notnull where TNext : notnull
        => result.Bind(value
            => collectionSelector(value).Map(collection => resultSelector(value, collection)));

    /// <summary>Transforms the failure error and passes successes through.</summary>
    /// <typeparam name="TValue">The success value type.</typeparam>
    /// <param name="result">The result to transform.</param>
    /// <param name="mapError">The error transformation.</param>
    public static Result<TValue> MapError<TValue>(
        this Result<TValue> result,
        Func<Error, Error> mapError)
        where TValue : notnull
        => Result<TValue>.From(result.ToGeneric().MapError(mapError));

    /// <summary>Converts a success into a failure when the predicate fails.</summary>
    /// <typeparam name="TValue">The success value type.</typeparam>
    /// <param name="result">The result to test.</param>
    /// <param name="predicate">The predicate the success value must satisfy.</param>
    /// <param name="error">The error to return when the predicate fails.</param>
    public static Result<TValue> Ensure<TValue>(
        this Result<TValue> result,
        Func<TValue, bool> predicate,
        Error error)
        where TValue : notnull
        => Result<TValue>.From(result.ToGeneric().Ensure(predicate, error));

    /// <summary>Converts a success into a failure when the predicate fails.</summary>
    /// <typeparam name="TValue">The success value type.</typeparam>
    /// <param name="result">The result to test.</param>
    /// <param name="predicate">The predicate the success value must satisfy.</param>
    /// <param name="errorFactory">Creates the error from the rejected value.</param>
    public static Result<TValue> Ensure<TValue>(
        this Result<TValue> result,
        Func<TValue, bool> predicate,
        Func<TValue, Error> errorFactory)
        where TValue : notnull
        => Result<TValue>.From(result.ToGeneric().Ensure(predicate, errorFactory));

    /// <summary>Runs an action on the success value and returns the original result.</summary>
    /// <typeparam name="TValue">The success value type.</typeparam>
    /// <param name="result">The result to observe.</param>
    /// <param name="tap">The action to run.</param>
    public static Result<TValue> Tap<TValue>(this Result<TValue> result, Action<TValue> tap)
        where TValue : notnull
    {
        ArgumentNullException.ThrowIfNull(tap);

        if (result.IsSuccess)
            tap(result.Value);

        return result;
    }

    /// <summary>Runs an action on the failure error and returns the original result.</summary>
    /// <typeparam name="TValue">The success value type.</typeparam>
    /// <param name="result">The result to observe.</param>
    /// <param name="tap">The action to run.</param>
    public static Result<TValue> TapError<TValue>(this Result<TValue> result, Action<Error> tap)
        where TValue : notnull
    {
        ArgumentNullException.ThrowIfNull(tap);

        if (result.IsFailure)
            tap(result.Error);

        return result;
    }

    /// <summary>Returns the success value or a fallback.</summary>
    /// <typeparam name="TValue">The success value type.</typeparam>
    /// <param name="result">The result to read.</param>
    /// <param name="fallback">The value to return on failure.</param>
    public static TValue GetOrElse<TValue>(this Result<TValue> result, TValue fallback)
        where TValue : notnull
        => result.IsSuccess ? result.Value : fallback;

    /// <summary>Returns the success value or derives one from the failure error.</summary>
    /// <typeparam name="TValue">The success value type.</typeparam>
    /// <param name="result">The result to read.</param>
    /// <param name="fallback">Derives the value from the failure error.</param>
    public static TValue GetOrElse<TValue>(this Result<TValue> result, Func<Error, TValue> fallback)
        where TValue : notnull
    {
        ArgumentNullException.ThrowIfNull(fallback);

        return result.IsSuccess ? result.Value : fallback(result.Error);
    }

    /// <summary>Returns the original result or another result.</summary>
    /// <typeparam name="TValue">The success value type.</typeparam>
    /// <param name="result">The original result.</param>
    /// <param name="fallback">The result to use on failure.</param>
    public static Result<TValue> OrElse<TValue>(this Result<TValue> result, Result<TValue> fallback)
        where TValue : notnull
        => result.IsSuccess ? result : fallback;

    /// <summary>Returns the original result or derives another result.</summary>
    /// <typeparam name="TValue">The success value type.</typeparam>
    /// <param name="result">The original result.</param>
    /// <param name="fallback">Derives the result from the failure error.</param>
    public static Result<TValue> OrElse<TValue>(
        this Result<TValue> result,
        Func<Error, Result<TValue>> fallback)
        where TValue : notnull
    {
        ArgumentNullException.ThrowIfNull(fallback);

        return result.IsSuccess ? result : fallback(result.Error);
    }

    /// <summary>Returns the success value or throws a built exception.</summary>
    /// <typeparam name="TValue">The success value type.</typeparam>
    /// <param name="result">The result to read.</param>
    /// <param name="exceptionFactory">Builds the exception from the failure error.</param>
    public static TValue GetOrThrow<TValue>(
        this Result<TValue> result,
        Func<Error, Exception> exceptionFactory)
        where TValue : notnull
    {
        ArgumentNullException.ThrowIfNull(exceptionFactory);

        return result.IsSuccess ? result.Value : throw exceptionFactory(result.Error);
    }

    /// <summary>Returns the success value or throws an <see cref="InvalidOperationException"/>.</summary>
    /// <typeparam name="TValue">The success value type.</typeparam>
    /// <param name="result">The result to read.</param>
    public static TValue GetOrThrow<TValue>(this Result<TValue> result)
        where TValue : notnull
        => result.IsSuccess
            ? result.Value
            : throw new InvalidOperationException($"The result is a failure: {result.Error}");

    /// <summary>Converts a success into a present optional value; failures become absent.</summary>
    /// <typeparam name="TValue">The success value type.</typeparam>
    /// <param name="result">The result to convert.</param>
    public static Maybe<TValue> ToMaybe<TValue>(this Result<TValue> result)
        where TValue : notnull
        => result.IsSuccess ? Maybe<TValue>.Some(result.Value) : Maybe<TValue>.None;

    /// <summary>Enumerates zero or one success value.</summary>
    /// <typeparam name="TValue">The success value type.</typeparam>
    /// <param name="result">The result to enumerate.</param>
    public static IEnumerable<TValue> ToEnumerable<TValue>(this Result<TValue> result)
        where TValue : notnull
    {
        if (result.IsSuccess)
            yield return result.Value;
    }

    /// <summary>Unwraps a nested result.</summary>
    /// <typeparam name="TValue">The success value type.</typeparam>
    /// <param name="result">The nested result.</param>
    public static Result<TValue> Flatten<TValue>(this Result<Result<TValue>> result)
        where TValue : notnull
        => result.IsSuccess ? result.Value : Result<TValue>.Fail(result.Error);

    /// <summary>Transforms the success value asynchronously.</summary>
    /// <typeparam name="TValue">The success value type.</typeparam>
    /// <typeparam name="TNext">The transformed value type.</typeparam>
    /// <param name="result">The result to transform.</param>
    /// <param name="map">The asynchronous transformation.</param>
    public static async Task<Result<TNext>> MapAsync<TValue, TNext>(
        this Result<TValue> result,
        Func<TValue, Task<TNext>> map)
        where TValue : notnull where TNext : notnull
    {
        ArgumentNullException.ThrowIfNull(map);

        return result.IsSuccess
            ? Result<TNext>.Ok(await map(result.Value).ConfigureAwait(false))
            : Result<TNext>.Fail(result.Error);
    }

    /// <summary>Chains an asynchronous result projection.</summary>
    /// <typeparam name="TValue">The success value type.</typeparam>
    /// <typeparam name="TNext">The chained value type.</typeparam>
    /// <param name="result">The result to chain.</param>
    /// <param name="bind">The asynchronous projection.</param>
    public static async Task<Result<TNext>> BindAsync<TValue, TNext>(
        this Result<TValue> result,
        Func<TValue, Task<Result<TNext>>> bind)
        where TValue : notnull where TNext : notnull
    {
        ArgumentNullException.ThrowIfNull(bind);

        return result.IsSuccess
            ? await bind(result.Value).ConfigureAwait(false)
            : Result<TNext>.Fail(result.Error);
    }

    /// <summary>Runs an asynchronous action on the success value.</summary>
    /// <typeparam name="TValue">The success value type.</typeparam>
    /// <param name="result">The result to observe.</param>
    /// <param name="tap">The asynchronous action.</param>
    public static async Task<Result<TValue>> TapAsync<TValue>(
        this Result<TValue> result,
        Func<TValue, Task> tap)
        where TValue : notnull
    {
        ArgumentNullException.ThrowIfNull(tap);

        if (result.IsSuccess)
            await tap(result.Value).ConfigureAwait(false);

        return result;
    }

    /// <summary>Transforms the success value of an asynchronous result.</summary>
    /// <typeparam name="TValue">The success value type.</typeparam>
    /// <typeparam name="TNext">The transformed value type.</typeparam>
    /// <param name="result">The asynchronous result.</param>
    /// <param name="map">The asynchronous transformation.</param>
    public static async Task<Result<TNext>> MapAsync<TValue, TNext>(
        this Task<Result<TValue>> result,
        Func<TValue, Task<TNext>> map)
        where TValue : notnull where TNext : notnull
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(map);

        return await (await result.ConfigureAwait(false)).MapAsync(map).ConfigureAwait(false);
    }

    /// <summary>Transforms the success value of an asynchronous result synchronously.</summary>
    /// <typeparam name="TValue">The success value type.</typeparam>
    /// <typeparam name="TNext">The transformed value type.</typeparam>
    /// <param name="result">The asynchronous result.</param>
    /// <param name="map">The transformation.</param>
    public static async Task<Result<TNext>> MapAsync<TValue, TNext>(
        this Task<Result<TValue>> result,
        Func<TValue, TNext> map)
        where TValue : notnull where TNext : notnull
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(map);

        return (await result.ConfigureAwait(false)).Map(map);
    }

    /// <summary>Chains an asynchronous result projection from an asynchronous result.</summary>
    /// <typeparam name="TValue">The success value type.</typeparam>
    /// <typeparam name="TNext">The chained value type.</typeparam>
    /// <param name="result">The asynchronous result.</param>
    /// <param name="bind">The asynchronous projection.</param>
    public static async Task<Result<TNext>> BindAsync<TValue, TNext>(
        this Task<Result<TValue>> result,
        Func<TValue, Task<Result<TNext>>> bind)
        where TValue : notnull where TNext : notnull
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(bind);

        return await (await result.ConfigureAwait(false)).BindAsync(bind).ConfigureAwait(false);
    }

    /// <summary>Runs an asynchronous action on the success value of an asynchronous result.</summary>
    /// <typeparam name="TValue">The success value type.</typeparam>
    /// <param name="result">The asynchronous result.</param>
    /// <param name="tap">The asynchronous action.</param>
    public static async Task<Result<TValue>> TapAsync<TValue>(
        this Task<Result<TValue>> result,
        Func<TValue, Task> tap)
        where TValue : notnull
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(tap);

        return await (await result.ConfigureAwait(false)).TapAsync(tap).ConfigureAwait(false);
    }
}
