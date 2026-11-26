using LayeredCraft.Toolkit.Functional;

namespace LayeredCraft.Toolkit.Functional.Tests;

public sealed class ErrorAndUnitTests
{
    [Fact]
    public void Unit_HasOneValue()
    {
        Assert.Equal(Unit.Value, default);
        Assert.True(Unit.Value == default);
        Assert.Equal("()", Unit.Value.ToString());
        Assert.Equal(0, Unit.Value.GetHashCode());
    }

    [Fact]
    public void Error_FromException_KeepsMessageAndException()
    {
        var exception = new InvalidOperationException("boom");

        var error = Error.FromException(exception);

        Assert.Equal("boom", error.Message);
        Assert.Same(exception, error.Exception);
    }

    [Fact]
    public void Error_ConvertsFromString()
    {
        Error error = "boom";

        Assert.Equal("boom", error.Message);
        Assert.Null(error.Exception);
    }

    [Fact]
    public void Error_RejectsNullException()
        => Assert.Throws<ArgumentNullException>(() => Error.FromException(null!));
}
