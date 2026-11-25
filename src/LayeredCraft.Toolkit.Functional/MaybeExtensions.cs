namespace LayeredCraft.Toolkit.Functional;

/// <summary>
/// Composition helpers for <see cref="Maybe{T}"/>.
/// </summary>
public static class MaybeExtensions
{
    /// <summary>Wraps a nullable value type as a <see cref="Maybe{T}"/>.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="value">The nullable value.</param>
    /// <returns>A present value, or <see cref="Maybe.None"/> when <paramref name="value"/> is null.</returns>
    public static Maybe<T> ToMaybe<T>(this T? value)
        where T : struct
        => value.HasValue ? Maybe<T>.Some(value.Value) : Maybe<T>.None;

    /// <summary>Wraps a nullable reference as a <see cref="Maybe{T}"/>.</summary>
    /// <typeparam name="T">The reference type.</typeparam>
    /// <param name="value">The nullable reference.</param>
    /// <returns>A present value, or <see cref="Maybe.None"/> when <paramref name="value"/> is null.</returns>
    public static Maybe<T> ToMaybe<T>(this T? value)
        where T : class
        => value is not null ? Maybe<T>.Some(value) : Maybe<T>.None;

    /// <summary>Transforms a present value and leaves an absent value untouched.</summary>
    /// <typeparam name="T">The present value type.</typeparam>
    /// <typeparam name="TResult">The transformed value type.</typeparam>
    /// <param name="maybe">The optional value.</param>
    /// <param name="map">The transformation.</param>
    /// <returns>The transformed present value, or the empty case.</returns>
    public static Maybe<TResult> Map<T, TResult>(this Maybe<T> maybe, Func<T, TResult> map)
        where T : notnull
        where TResult : notnull
    {
        ArgumentNullException.ThrowIfNull(map);

        return maybe.HasValue ? Maybe<TResult>.Some(map(maybe.Value)) : Maybe<TResult>.None;
    }

    /// <summary>Chains an operation that can also be empty.</summary>
    /// <typeparam name="T">The present value type.</typeparam>
    /// <typeparam name="TResult">The chained value type.</typeparam>
    /// <param name="maybe">The optional value.</param>
    /// <param name="bind">The operation to run when a value is present.</param>
    /// <returns>The chained result, or the empty case.</returns>
    public static Maybe<TResult> Bind<T, TResult>(
        this Maybe<T> maybe,
        Func<T, Maybe<TResult>> bind)
        where T : notnull
        where TResult : notnull
    {
        ArgumentNullException.ThrowIfNull(bind);

        return maybe.HasValue ? bind(maybe.Value) : Maybe<TResult>.None;
    }

    /// <summary>Keeps a present value only when it satisfies a predicate.</summary>
    /// <typeparam name="T">The present value type.</typeparam>
    /// <param name="maybe">The optional value.</param>
    /// <param name="predicate">The predicate to satisfy.</param>
    /// <returns>The original value when the predicate passes; otherwise the empty case.</returns>
    public static Maybe<T> Filter<T>(this Maybe<T> maybe, Func<T, bool> predicate)
        where T : notnull
    {
        ArgumentNullException.ThrowIfNull(predicate);

        return maybe.HasValue && predicate(maybe.Value) ? maybe : Maybe<T>.None;
    }

    /// <summary>Returns the present value or a fallback.</summary>
    /// <typeparam name="T">The present value type.</typeparam>
    /// <param name="maybe">The optional value.</param>
    /// <param name="fallback">The value to return when empty.</param>
    public static T GetOrElse<T>(this Maybe<T> maybe, T fallback)
        where T : notnull
        => maybe.HasValue ? maybe.Value : fallback;

    /// <summary>Returns the original value or another optional value.</summary>
    /// <typeparam name="T">The present value type.</typeparam>
    /// <param name="maybe">The optional value.</param>
    /// <param name="fallback">The optional value to use when empty.</param>
    public static Maybe<T> OrElse<T>(this Maybe<T> maybe, Maybe<T> fallback)
        where T : notnull
        => maybe.HasValue ? maybe : fallback;

    /// <summary>Reduces both cases to a single value.</summary>
    /// <typeparam name="T">The present value type.</typeparam>
    /// <typeparam name="TResult">The reduced result type.</typeparam>
    /// <param name="maybe">The optional value.</param>
    /// <param name="seed">The absent result.</param>
    /// <param name="folder">Combines the seed with a present value.</param>
    public static TResult Fold<T, TResult>(
        this Maybe<T> maybe,
        TResult seed,
        Func<TResult, T, TResult> folder)
        where T : notnull
    {
        ArgumentNullException.ThrowIfNull(folder);

        return maybe.HasValue ? folder(seed, maybe.Value) : seed;
    }

    /// <summary>Runs an action when a value is present and returns the original value.</summary>
    /// <typeparam name="T">The present value type.</typeparam>
    /// <param name="maybe">The optional value.</param>
    /// <param name="tap">The action to run.</param>
    public static Maybe<T> Tap<T>(this Maybe<T> maybe, Action<T> tap)
        where T : notnull
    {
        ArgumentNullException.ThrowIfNull(tap);

        if (maybe.TryGetValue(out var value))
            tap(value);

        return maybe;
    }

    /// <summary>Runs an action when the value is absent and returns the original value.</summary>
    /// <typeparam name="T">The present value type.</typeparam>
    /// <param name="maybe">The optional value.</param>
    /// <param name="tap">The action to run.</param>
    public static Maybe<T> TapNone<T>(this Maybe<T> maybe, Action tap)
        where T : notnull
    {
        ArgumentNullException.ThrowIfNull(tap);

        if (!maybe.HasValue)
            tap();

        return maybe;
    }

    /// <summary>Combines two optional values into an optional tuple.</summary>
    /// <typeparam name="T">The first value type.</typeparam>
    /// <typeparam name="TOther">The second value type.</typeparam>
    /// <param name="maybe">The first optional value.</param>
    /// <param name="other">The second optional value.</param>
    /// <returns>The tuple when both values are present; otherwise the empty case.</returns>
    public static Maybe<(T, TOther)> Zip<T, TOther>(this Maybe<T> maybe, Maybe<TOther> other)
        where T : notnull
        where TOther : notnull
        => maybe.TryGetValue(out var left) && other.TryGetValue(out var right)
            ? Maybe<(T, TOther)>.Some((left, right))
            : Maybe<(T, TOther)>.None;

    /// <summary>Flattens nested optional values.</summary>
    /// <typeparam name="T">The present value type.</typeparam>
    /// <param name="maybe">The nested optional value.</param>
    public static Maybe<T> Flatten<T>(this Maybe<Maybe<T>> maybe)
        where T : notnull
        => maybe.TryGetValue(out var inner) ? inner : Maybe<T>.None;

    /// <summary>Checks whether a present value equals a candidate.</summary>
    /// <typeparam name="T">The present value type.</typeparam>
    /// <param name="maybe">The optional value.</param>
    /// <param name="candidate">The candidate value.</param>
    /// <returns><see langword="true"/> only when a value is present and equal.</returns>
    public static bool Contains<T>(this Maybe<T> maybe, T candidate)
        where T : notnull
        => maybe.TryGetValue(out var value) && EqualityComparer<T>.Default.Equals(value, candidate);

    /// <summary>Checks whether a present value satisfies a predicate.</summary>
    /// <typeparam name="T">The present value type.</typeparam>
    /// <param name="maybe">The optional value.</param>
    /// <param name="predicate">The predicate to test.</param>
    public static bool Exists<T>(this Maybe<T> maybe, Func<T, bool> predicate)
        where T : notnull
    {
        ArgumentNullException.ThrowIfNull(predicate);

        return maybe.TryGetValue(out var value) && predicate(value);
    }

    /// <summary>Checks whether the empty case is present or the value satisfies a predicate.</summary>
    /// <typeparam name="T">The present value type.</typeparam>
    /// <param name="maybe">The optional value.</param>
    /// <param name="predicate">The predicate to test.</param>
    public static bool ForAll<T>(this Maybe<T> maybe, Func<T, bool> predicate)
        where T : notnull
    {
        ArgumentNullException.ThrowIfNull(predicate);

        return !maybe.TryGetValue(out var value) || predicate(value);
    }

    /// <summary>Converts a present optional value type to a nullable value type.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="maybe">The optional value.</param>
    public static T? ToNullable<T>(this Maybe<T> maybe)
        where T : struct
        => maybe.TryGetValue(out var value) ? value : null;

    /// <summary>Converts a present optional reference to a nullable reference.</summary>
    /// <typeparam name="T">The reference type.</typeparam>
    /// <param name="maybe">The optional value.</param>
    /// <remarks>
    /// This has a distinct name because <c>ToNullable</c> overloads cannot differ only by
    /// <see langword="struct"/> and <see langword="class"/> constraints.
    /// </remarks>
    public static T? ToNullableReference<T>(this Maybe<T> maybe)
        where T : class
        => maybe.TryGetValue(out var value) ? value : null;

    /// <summary>Enumerates zero or one values.</summary>
    /// <typeparam name="T">The present value type.</typeparam>
    /// <param name="maybe">The optional value.</param>
    public static IEnumerable<T> ToEnumerable<T>(this Maybe<T> maybe)
        where T : notnull
    {
        if (maybe.TryGetValue(out var value))
            yield return value;
    }

    /// <summary>Converts the optional value into a result with a fixed error.</summary>
    /// <typeparam name="T">The present value type.</typeparam>
    /// <typeparam name="TError">The failure error type.</typeparam>
    /// <param name="maybe">The optional value.</param>
    /// <param name="error">The error to use when empty.</param>
    public static Result<T, TError> ToResult<T, TError>(this Maybe<T> maybe, TError error)
        where T : notnull
        => maybe.TryGetValue(out var value)
            ? Result<T, TError>.Ok(value)
            : Result<T, TError>.Fail(error);

    /// <summary>Converts the optional value into a result with a lazily created error.</summary>
    /// <typeparam name="T">The present value type.</typeparam>
    /// <typeparam name="TError">The failure error type.</typeparam>
    /// <param name="maybe">The optional value.</param>
    /// <param name="errorFactory">Creates the error when empty.</param>
    public static Result<T, TError> ToResult<T, TError>(
        this Maybe<T> maybe,
        Func<TError> errorFactory)
        where T : notnull
    {
        ArgumentNullException.ThrowIfNull(errorFactory);

        return maybe.TryGetValue(out var value)
            ? Result<T, TError>.Ok(value)
            : Result<T, TError>.Fail(errorFactory());
    }

    /// <summary>Transforms a present value asynchronously.</summary>
    /// <typeparam name="T">The present value type.</typeparam>
    /// <typeparam name="TResult">The transformed value type.</typeparam>
    /// <param name="maybe">The optional value.</param>
    /// <param name="map">The asynchronous transformation.</param>
    public static async Task<Maybe<TResult>> MapAsync<T, TResult>(
        this Maybe<T> maybe,
        Func<T, Task<TResult>> map)
        where T : notnull
        where TResult : notnull
    {
        ArgumentNullException.ThrowIfNull(map);

        return maybe.TryGetValue(out var value)
            ? Maybe<TResult>.Some(await map(value).ConfigureAwait(false))
            : Maybe<TResult>.None;
    }

    /// <summary>Chains an asynchronous operation that can also be empty.</summary>
    /// <typeparam name="T">The present value type.</typeparam>
    /// <typeparam name="TResult">The chained value type.</typeparam>
    /// <param name="maybe">The optional value.</param>
    /// <param name="bind">The asynchronous operation.</param>
    public static async Task<Maybe<TResult>> BindAsync<T, TResult>(
        this Maybe<T> maybe,
        Func<T, Task<Maybe<TResult>>> bind)
        where T : notnull
        where TResult : notnull
    {
        ArgumentNullException.ThrowIfNull(bind);

        return maybe.TryGetValue(out var value)
            ? await bind(value).ConfigureAwait(false)
            : Maybe<TResult>.None;
    }

    /// <summary>Runs an asynchronous action when a value is present.</summary>
    /// <typeparam name="T">The present value type.</typeparam>
    /// <param name="maybe">The optional value.</param>
    /// <param name="tap">The asynchronous action.</param>
    public static async Task<Maybe<T>> TapAsync<T>(this Maybe<T> maybe, Func<T, Task> tap)
        where T : notnull
    {
        ArgumentNullException.ThrowIfNull(tap);

        if (maybe.TryGetValue(out var value))
            await tap(value).ConfigureAwait(false);

        return maybe;
    }

    /// <summary>Transforms the value of an asynchronous optional value.</summary>
    /// <typeparam name="T">The present value type.</typeparam>
    /// <typeparam name="TResult">The transformed value type.</typeparam>
    /// <param name="maybe">The asynchronous optional value.</param>
    /// <param name="map">The asynchronous transformation.</param>
    public static async Task<Maybe<TResult>> MapAsync<T, TResult>(
        this Task<Maybe<T>> maybe,
        Func<T, Task<TResult>> map)
        where T : notnull
        where TResult : notnull
    {
        ArgumentNullException.ThrowIfNull(maybe);
        ArgumentNullException.ThrowIfNull(map);

        var value = await maybe.ConfigureAwait(false);
        return value.TryGetValue(out var present)
            ? Maybe<TResult>.Some(await map(present).ConfigureAwait(false))
            : Maybe<TResult>.None;
    }

    /// <summary>Transforms the value of an asynchronous optional value synchronously.</summary>
    /// <typeparam name="T">The present value type.</typeparam>
    /// <typeparam name="TResult">The transformed value type.</typeparam>
    /// <param name="maybe">The asynchronous optional value.</param>
    /// <param name="map">The transformation.</param>
    public static async Task<Maybe<TResult>> MapAsync<T, TResult>(
        this Task<Maybe<T>> maybe,
        Func<T, TResult> map)
        where T : notnull
        where TResult : notnull
    {
        ArgumentNullException.ThrowIfNull(maybe);
        ArgumentNullException.ThrowIfNull(map);

        var value = await maybe.ConfigureAwait(false);
        return value.TryGetValue(out var present)
            ? Maybe<TResult>.Some(map(present))
            : Maybe<TResult>.None;
    }

    /// <summary>Chains an asynchronous operation from an asynchronous optional value.</summary>
    /// <typeparam name="T">The present value type.</typeparam>
    /// <typeparam name="TResult">The chained value type.</typeparam>
    /// <param name="maybe">The asynchronous optional value.</param>
    /// <param name="bind">The asynchronous operation.</param>
    public static async Task<Maybe<TResult>> BindAsync<T, TResult>(
        this Task<Maybe<T>> maybe,
        Func<T, Task<Maybe<TResult>>> bind)
        where T : notnull
        where TResult : notnull
    {
        ArgumentNullException.ThrowIfNull(maybe);
        ArgumentNullException.ThrowIfNull(bind);

        var value = await maybe.ConfigureAwait(false);
        return value.TryGetValue(out var present)
            ? await bind(present).ConfigureAwait(false)
            : Maybe<TResult>.None;
    }
}
