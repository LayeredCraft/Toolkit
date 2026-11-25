namespace LayeredCraft.Toolkit.Functional;

/// <summary>
/// Describes why a <see cref="Result{TValue,TError}"/> failed.
/// </summary>
/// <param name="Message">A human-readable description of the failure.</param>
/// <param name="Exception">The exception that caused the failure, when one exists.</param>
public readonly record struct Error(string Message, Exception? Exception = null)
{
    /// <summary>Creates an <see cref="Error"/> from an exception, using the exception message.</summary>
    /// <param name="exception">The exception to describe.</param>
    /// <returns>An error carrying the exception and its message.</returns>
    public static Error FromException(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return new Error(exception.Message, exception);
    }

    /// <summary>Converts a message into an <see cref="Error"/>.</summary>
    /// <param name="message">A human-readable description of the failure.</param>
    public static implicit operator Error(string message) => new(message);
}
