namespace LayeredCraft.Toolkit.Functional;

/// <summary>
/// Asynchronous composition helpers for <see cref="Maybe{T}"/>.
/// </summary>
public static class MaybeAsyncExtensions
{
    /// <summary>Keeps a present value only when an asynchronous predicate passes.</summary>
    /// <typeparam name="T">The present value type.</typeparam>
    /// <param name="maybe">The optional value.</param>
    /// <param name="predicate">The asynchronous predicate to satisfy.</param>
    /// <returns>The original value when the predicate passes; otherwise the empty case.</returns>
    public static async Task<Maybe<T>> FilterAsync<T>(
        this Maybe<T> maybe,
        Func<T, Task<bool>> predicate) where T : notnull
    {
        ArgumentNullException.ThrowIfNull(predicate);

        if (!maybe.TryGetValue(out var value))
            return Maybe<T>.None;

        return await predicate(value).ConfigureAwait(false) ? maybe : Maybe<T>.None;
    }

    /// <summary>Keeps a present value only when an asynchronous predicate passes.</summary>
    /// <typeparam name="T">The present value type.</typeparam>
    /// <param name="maybe">The asynchronous optional value.</param>
    /// <param name="predicate">The asynchronous predicate to satisfy.</param>
    /// <returns>The original value when the predicate passes; otherwise the empty case.</returns>
    public static async Task<Maybe<T>> FilterAsync<T>(
        this Task<Maybe<T>> maybe,
        Func<T, Task<bool>> predicate) where T : notnull
    {
        ArgumentNullException.ThrowIfNull(maybe);
        ArgumentNullException.ThrowIfNull(predicate);

        var value = await maybe.ConfigureAwait(false);
        return await value.FilterAsync(predicate).ConfigureAwait(false);
    }

    /// <summary>Runs an asynchronous action when the value is absent.</summary>
    /// <typeparam name="T">The present value type.</typeparam>
    /// <param name="maybe">The optional value.</param>
    /// <param name="tap">The asynchronous action to run.</param>
    /// <returns>The original optional value.</returns>
    public static async Task<Maybe<T>> TapNoneAsync<T>(
        this Maybe<T> maybe,
        Func<Task> tap) where T : notnull
    {
        ArgumentNullException.ThrowIfNull(tap);

        if (!maybe.HasValue)
            await tap().ConfigureAwait(false);

        return maybe;
    }

    /// <summary>Runs an asynchronous action when the value is absent.</summary>
    /// <typeparam name="T">The present value type.</typeparam>
    /// <param name="maybe">The asynchronous optional value.</param>
    /// <param name="tap">The asynchronous action to run.</param>
    /// <returns>The original optional value.</returns>
    public static async Task<Maybe<T>> TapNoneAsync<T>(
        this Task<Maybe<T>> maybe,
        Func<Task> tap) where T : notnull
    {
        ArgumentNullException.ThrowIfNull(maybe);
        ArgumentNullException.ThrowIfNull(tap);

        var value = await maybe.ConfigureAwait(false);
        return await value.TapNoneAsync(tap).ConfigureAwait(false);
    }

    /// <summary>Returns the present value or an asynchronously created fallback.</summary>
    /// <typeparam name="T">The present value type.</typeparam>
    /// <param name="maybe">The optional value.</param>
    /// <param name="fallback">Creates the value to return when empty.</param>
    public static async Task<T> GetOrElseAsync<T>(
        this Maybe<T> maybe,
        Func<Task<T>> fallback) where T : notnull
    {
        ArgumentNullException.ThrowIfNull(fallback);

        return maybe.TryGetValue(out var value)
            ? value
            : await fallback().ConfigureAwait(false);
    }

    /// <summary>Returns the present value or an asynchronously created fallback.</summary>
    /// <typeparam name="T">The present value type.</typeparam>
    /// <param name="maybe">The asynchronous optional value.</param>
    /// <param name="fallback">Creates the value to return when empty.</param>
    public static async Task<T> GetOrElseAsync<T>(
        this Task<Maybe<T>> maybe,
        Func<Task<T>> fallback) where T : notnull
    {
        ArgumentNullException.ThrowIfNull(maybe);
        ArgumentNullException.ThrowIfNull(fallback);

        var value = await maybe.ConfigureAwait(false);
        return await value.GetOrElseAsync(fallback).ConfigureAwait(false);
    }

    /// <summary>Returns the original value or an asynchronously created optional value.</summary>
    /// <typeparam name="T">The present value type.</typeparam>
    /// <param name="maybe">The optional value.</param>
    /// <param name="fallback">Creates the optional value to use when empty.</param>
    public static async Task<Maybe<T>> OrElseAsync<T>(
        this Maybe<T> maybe,
        Func<Task<Maybe<T>>> fallback) where T : notnull
    {
        ArgumentNullException.ThrowIfNull(fallback);

        return maybe.HasValue ? maybe : await fallback().ConfigureAwait(false);
    }

    /// <summary>Returns the original value or an asynchronously created optional value.</summary>
    /// <typeparam name="T">The present value type.</typeparam>
    /// <param name="maybe">The asynchronous optional value.</param>
    /// <param name="fallback">Creates the optional value to use when empty.</param>
    public static async Task<Maybe<T>> OrElseAsync<T>(
        this Task<Maybe<T>> maybe,
        Func<Task<Maybe<T>>> fallback) where T : notnull
    {
        ArgumentNullException.ThrowIfNull(maybe);
        ArgumentNullException.ThrowIfNull(fallback);

        var value = await maybe.ConfigureAwait(false);
        return await value.OrElseAsync(fallback).ConfigureAwait(false);
    }

    /// <summary>Reduces both cases to a single value with an asynchronous folder.</summary>
    /// <typeparam name="T">The present value type.</typeparam>
    /// <typeparam name="TResult">The reduced result type.</typeparam>
    /// <param name="maybe">The optional value.</param>
    /// <param name="seed">The absent result.</param>
    /// <param name="folder">Combines the seed with a present value.</param>
    public static async Task<TResult> FoldAsync<T, TResult>(
        this Maybe<T> maybe,
        TResult seed,
        Func<TResult, T, Task<TResult>> folder) where T : notnull
    {
        ArgumentNullException.ThrowIfNull(folder);

        return maybe.TryGetValue(out var value)
            ? await folder(seed, value).ConfigureAwait(false)
            : seed;
    }

    /// <summary>Reduces both cases to a single value with an asynchronous folder.</summary>
    /// <typeparam name="T">The present value type.</typeparam>
    /// <typeparam name="TResult">The reduced result type.</typeparam>
    /// <param name="maybe">The asynchronous optional value.</param>
    /// <param name="seed">The absent result.</param>
    /// <param name="folder">Combines the seed with a present value.</param>
    public static async Task<TResult> FoldAsync<T, TResult>(
        this Task<Maybe<T>> maybe,
        TResult seed,
        Func<TResult, T, Task<TResult>> folder) where T : notnull
    {
        ArgumentNullException.ThrowIfNull(maybe);
        ArgumentNullException.ThrowIfNull(folder);

        var value = await maybe.ConfigureAwait(false);
        return await value.FoldAsync(seed, folder).ConfigureAwait(false);
    }

    /// <summary>Projects both cases into a single result with asynchronous projections.</summary>
    /// <typeparam name="T">The present value type.</typeparam>
    /// <typeparam name="TResult">The projected result type.</typeparam>
    /// <param name="maybe">The optional value.</param>
    /// <param name="some">Projects a present value.</param>
    /// <param name="none">Produces the absent result.</param>
    public static async Task<TResult> MatchAsync<T, TResult>(
        this Maybe<T> maybe,
        Func<T, Task<TResult>> some,
        Func<Task<TResult>> none) where T : notnull
    {
        ArgumentNullException.ThrowIfNull(some);
        ArgumentNullException.ThrowIfNull(none);

        return maybe.TryGetValue(out var value)
            ? await some(value).ConfigureAwait(false)
            : await none().ConfigureAwait(false);
    }

    /// <summary>Projects both cases into a single result with asynchronous projections.</summary>
    /// <typeparam name="T">The present value type.</typeparam>
    /// <typeparam name="TResult">The projected result type.</typeparam>
    /// <param name="maybe">The asynchronous optional value.</param>
    /// <param name="some">Projects a present value.</param>
    /// <param name="none">Produces the absent result.</param>
    public static async Task<TResult> MatchAsync<T, TResult>(
        this Task<Maybe<T>> maybe,
        Func<T, Task<TResult>> some,
        Func<Task<TResult>> none) where T : notnull
    {
        ArgumentNullException.ThrowIfNull(maybe);
        ArgumentNullException.ThrowIfNull(some);
        ArgumentNullException.ThrowIfNull(none);

        var value = await maybe.ConfigureAwait(false);
        return await value.MatchAsync(some, none).ConfigureAwait(false);
    }

    /// <summary>Runs one of two asynchronous actions depending on the case.</summary>
    /// <typeparam name="T">The present value type.</typeparam>
    /// <param name="maybe">The optional value.</param>
    /// <param name="some">Runs when a value is present.</param>
    /// <param name="none">Runs when the value is absent.</param>
    public static async Task SwitchAsync<T>(
        this Maybe<T> maybe,
        Func<T, Task> some,
        Func<Task> none) where T : notnull
    {
        ArgumentNullException.ThrowIfNull(some);
        ArgumentNullException.ThrowIfNull(none);

        if (maybe.TryGetValue(out var value))
            await some(value).ConfigureAwait(false);
        else
            await none().ConfigureAwait(false);
    }

    /// <summary>Runs one of two asynchronous actions depending on the case.</summary>
    /// <typeparam name="T">The present value type.</typeparam>
    /// <param name="maybe">The asynchronous optional value.</param>
    /// <param name="some">Runs when a value is present.</param>
    /// <param name="none">Runs when the value is absent.</param>
    public static async Task SwitchAsync<T>(
        this Task<Maybe<T>> maybe,
        Func<T, Task> some,
        Func<Task> none) where T : notnull
    {
        ArgumentNullException.ThrowIfNull(maybe);
        ArgumentNullException.ThrowIfNull(some);
        ArgumentNullException.ThrowIfNull(none);

        var value = await maybe.ConfigureAwait(false);
        await value.SwitchAsync(some, none).ConfigureAwait(false);
    }

    /// <summary>Checks whether a present value satisfies an asynchronous predicate.</summary>
    /// <typeparam name="T">The present value type.</typeparam>
    /// <param name="maybe">The optional value.</param>
    /// <param name="predicate">The asynchronous predicate to test.</param>
    public static async Task<bool> ExistsAsync<T>(
        this Maybe<T> maybe,
        Func<T, Task<bool>> predicate) where T : notnull
    {
        ArgumentNullException.ThrowIfNull(predicate);

        return maybe.TryGetValue(out var value)
            && await predicate(value).ConfigureAwait(false);
    }

    /// <summary>Checks whether a present value satisfies an asynchronous predicate.</summary>
    /// <typeparam name="T">The present value type.</typeparam>
    /// <param name="maybe">The asynchronous optional value.</param>
    /// <param name="predicate">The asynchronous predicate to test.</param>
    public static async Task<bool> ExistsAsync<T>(
        this Task<Maybe<T>> maybe,
        Func<T, Task<bool>> predicate) where T : notnull
    {
        ArgumentNullException.ThrowIfNull(maybe);
        ArgumentNullException.ThrowIfNull(predicate);

        var value = await maybe.ConfigureAwait(false);
        return await value.ExistsAsync(predicate).ConfigureAwait(false);
    }

    /// <summary>Checks whether the empty case is present or the value satisfies an asynchronous predicate.</summary>
    /// <typeparam name="T">The present value type.</typeparam>
    /// <param name="maybe">The optional value.</param>
    /// <param name="predicate">The asynchronous predicate to test.</param>
    public static async Task<bool> ForAllAsync<T>(
        this Maybe<T> maybe,
        Func<T, Task<bool>> predicate) where T : notnull
    {
        ArgumentNullException.ThrowIfNull(predicate);

        return !maybe.TryGetValue(out var value)
            || await predicate(value).ConfigureAwait(false);
    }

    /// <summary>Checks whether the empty case is present or the value satisfies an asynchronous predicate.</summary>
    /// <typeparam name="T">The present value type.</typeparam>
    /// <param name="maybe">The asynchronous optional value.</param>
    /// <param name="predicate">The asynchronous predicate to test.</param>
    public static async Task<bool> ForAllAsync<T>(
        this Task<Maybe<T>> maybe,
        Func<T, Task<bool>> predicate) where T : notnull
    {
        ArgumentNullException.ThrowIfNull(maybe);
        ArgumentNullException.ThrowIfNull(predicate);

        var value = await maybe.ConfigureAwait(false);
        return await value.ForAllAsync(predicate).ConfigureAwait(false);
    }

    /// <summary>Checks whether an asynchronous optional value contains a candidate.</summary>
    /// <typeparam name="T">The present value type.</typeparam>
    /// <param name="maybe">The asynchronous optional value.</param>
    /// <param name="candidate">The candidate value.</param>
    public static async Task<bool> ContainsAsync<T>(
        this Task<Maybe<T>> maybe,
        T candidate) where T : notnull
    {
        ArgumentNullException.ThrowIfNull(maybe);

        var value = await maybe.ConfigureAwait(false);
        return value.TryGetValue(out var present)
            && EqualityComparer<T>.Default.Equals(present, candidate);
    }

    /// <summary>Combines two optional values into an optional tuple asynchronously.</summary>
    /// <typeparam name="T">The first value type.</typeparam>
    /// <typeparam name="TOther">The second value type.</typeparam>
    /// <param name="maybe">The first optional value.</param>
    /// <param name="other">The second asynchronous optional value.</param>
    public static async Task<Maybe<(T, TOther)>> ZipAsync<T, TOther>(
        this Maybe<T> maybe,
        Task<Maybe<TOther>> other) where T : notnull where TOther : notnull
    {
        ArgumentNullException.ThrowIfNull(other);

        var right = await other.ConfigureAwait(false);
        return maybe.TryGetValue(out var left) && right.TryGetValue(out var rightValue)
            ? Maybe<(T, TOther)>.Some((left, rightValue))
            : Maybe<(T, TOther)>.None;
    }

    /// <summary>Combines two optional values into an optional tuple asynchronously.</summary>
    /// <typeparam name="T">The first value type.</typeparam>
    /// <typeparam name="TOther">The second value type.</typeparam>
    /// <param name="maybe">The first asynchronous optional value.</param>
    /// <param name="other">The second optional value.</param>
    public static async Task<Maybe<(T, TOther)>> ZipAsync<T, TOther>(
        this Task<Maybe<T>> maybe,
        Maybe<TOther> other) where T : notnull where TOther : notnull
    {
        ArgumentNullException.ThrowIfNull(maybe);

        var left = await maybe.ConfigureAwait(false);
        return left.TryGetValue(out var leftValue) && other.TryGetValue(out var rightValue)
            ? Maybe<(T, TOther)>.Some((leftValue, rightValue))
            : Maybe<(T, TOther)>.None;
    }

    /// <summary>Combines two asynchronous optional values into an optional tuple.</summary>
    /// <typeparam name="T">The first value type.</typeparam>
    /// <typeparam name="TOther">The second value type.</typeparam>
    /// <param name="maybe">The first asynchronous optional value.</param>
    /// <param name="other">The second asynchronous optional value.</param>
    public static async Task<Maybe<(T, TOther)>> ZipAsync<T, TOther>(
        this Task<Maybe<T>> maybe,
        Task<Maybe<TOther>> other) where T : notnull where TOther : notnull
    {
        ArgumentNullException.ThrowIfNull(maybe);
        ArgumentNullException.ThrowIfNull(other);

        var left = await maybe.ConfigureAwait(false);
        var right = await other.ConfigureAwait(false);
        return left.TryGetValue(out var leftValue) && right.TryGetValue(out var rightValue)
            ? Maybe<(T, TOther)>.Some((leftValue, rightValue))
            : Maybe<(T, TOther)>.None;
    }
}
