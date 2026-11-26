namespace LayeredCraft.Toolkit.Functional;

/// <summary>
/// Describes why a <see cref="Result{TValue}"/> or <see cref="Result{TValue,TError}"/> failed.
/// </summary>
/// <param name="Message">A human-readable description of the failure.</param>
/// <param name="Exception">The exception that caused the failure, when one exists.</param>
/// <param name="Code">
/// A stable, machine-readable code. Defaults to <see langword="null"/>; use it for telemetry,
/// <c>ProblemDetails</c> mapping, or localization keys.
/// </param>
public readonly record struct Error(string Message, Exception? Exception = null, string? Code = null)
{
    /// <summary>
    /// The failure returned by a default-initialized <see cref="Result{TValue}"/>. It is a real
    /// failure case, so a <c>switch</c> over a result never needs a <see langword="null"/> arm.
    /// </summary>
    public static Error Uninitialized =>
        new("The result was not initialized.", Code: "RESULT_UNINITIALIZED");

    /// <summary>Creates an <see cref="Error"/> from an exception, using the exception message.</summary>
    /// <param name="exception">The exception to describe.</param>
    /// <returns>An error carrying the exception, its message, and its type name as the code.</returns>
    public static Error FromException(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return new Error(exception.Message, exception, exception.GetType().Name);
    }

    /// <summary>Gets a value indicating whether this is the uninitialized default.</summary>
    public bool IsUninitialized => this == Uninitialized;

    /// <summary>Converts a message into an <see cref="Error"/>.</summary>
    /// <param name="message">A human-readable description of the failure.</param>
    public static implicit operator Error(string message) => new(message);
}
