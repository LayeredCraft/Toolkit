using LayeredCraft.Toolkit.Functional;

namespace LayeredCraft.Toolkit.Functional.Tests;

public sealed class ResultFactoryTests
{
    [Fact]
    public void Ok_InfersTheValueType()
    {
        Result<int> result = Result.Ok(42);

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void Ok_ThrowsOnNull()
    {
        Assert.Throws<ArgumentNullException>(() => Result.Ok<string>(null!));
    }

    [Fact]
    public void Fail_UsesTheExplicitTypeArgument()
    {
        var result = Result.Fail<int>(new Error("bad", Code: "E_BAD"));

        Assert.True(result.IsFailure);
        Assert.Equal("E_BAD", result.Error.Code);
    }

    [Fact]
    public void Fail_AcceptsAMessageThroughImplicitConversion()
    {
        var result = Result.Fail<int>("bad");

        Assert.True(result.IsFailure);
        Assert.Equal("bad", result.Error.Message);
    }

    [Fact]
    public void Try_InfersTheTypeAndCapturesTheException()
    {
        var result = Result.Try(() => int.Parse("not a number"));

        Assert.True(result.IsFailure);
        Assert.Equal("FormatException", result.Error.Code);
        Assert.IsType<FormatException>(result.Error.Exception);
    }

    [Fact]
    public async Task TryAsync_InfersTheTypeAndCapturesTheException()
    {
        var result = await Result.TryAsync(() => Task.FromException<int>(new TimeoutException("slow")));

        Assert.True(result.IsFailure);
        Assert.Equal("TimeoutException", result.Error.Code);
    }

    [Fact]
    public void Factory_AndTypeFactory_ProduceTheSameResult()
    {
        Assert.Equal(Result<int>.Ok(7), Result.Ok(7));
        Assert.Equal(Result<int>.Fail(new Error("bad")), Result.Fail<int>(new Error("bad")));
    }
}
