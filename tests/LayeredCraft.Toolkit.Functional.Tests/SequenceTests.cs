using LayeredCraft.Toolkit.Functional;

namespace LayeredCraft.Toolkit.Functional.Tests;

public sealed class SequenceTests
{
    [Fact]
    public void Maybe_Choose_DropsAbsentValues()
    {
        Maybe<int>[] values = [1, Maybe<int>.None, 3];

        Assert.Equal([1, 3], values.Choose());
    }

    [Fact]
    public void Maybe_Sequence_KeepsAllValuesWhenAllArePresent()
    {
        Maybe<int>[] values = [1, 2, 3];

        var sequenced = values.Sequence();

        Assert.True(sequenced.TryGetValue(out var present));
        Assert.Equal([1, 2, 3], present);
    }

    [Fact]
    public void Maybe_Sequence_IsNoneWhenAnyValueIsAbsent()
    {
        Maybe<int>[] values = [1, Maybe<int>.None, 3];

        Assert.True(values.Sequence().IsNone);
    }

    [Fact]
    public void Maybe_Traverse_ProjectsEveryItem()
    {
        var traversed = new[] { "1", "2", "3" }.Traverse(text
            => int.TryParse(text, out var value) ? Maybe<int>.Some(value) : Maybe<int>.None);

        Assert.True(traversed.TryGetValue(out var present));
        Assert.Equal([1, 2, 3], present);
    }

    [Fact]
    public void Maybe_Traverse_IsNoneWhenAnyProjectionIsAbsent()
    {
        var traversed = new[] { "1", "x", "3" }.Traverse(text
            => int.TryParse(text, out var value) ? Maybe<int>.Some(value) : Maybe<int>.None);

        Assert.True(traversed.IsNone);
    }

    [Fact]
    public void Maybe_FirstOrNone_ReturnsTheFirstItem()
    {
        Assert.Equal(1, new[] { 1, 2, 3 }.FirstOrNone().Value);
        Assert.True(Array.Empty<int>().FirstOrNone().IsNone);
        Assert.Equal(2, new[] { 1, 2, 3 }.FirstOrNone(value => value > 1).Value);
        Assert.True(new[] { 1, 2, 3 }.FirstOrNone(value => value > 3).IsNone);
    }

    [Fact]
    public void Maybe_QuerySyntax_ComposesOptionalValues()
    {
        var sum = from a in Maybe<int>.Some(1) from b in Maybe<int>.Some(2) select a + b;

        Assert.Equal(3, sum.Value);
    }

    [Fact]
    public void Maybe_QuerySyntax_IsNoneWhenAnyValueIsAbsent()
    {
        var sum = from a in Maybe<int>.Some(1) from b in Maybe<int>.None select a + b;

        Assert.True(sum.IsNone);
    }

    [Fact]
    public void Maybe_Switch_RunsTheMatchingAction()
    {
        var tapped = 0;

        Maybe<int>.Some(5).Switch(value => tapped = value, () => tapped = -1);
        Assert.Equal(5, tapped);

        Maybe<int>.None.Switch(_ => tapped = -1, () => tapped = 0);
        Assert.Equal(0, tapped);
    }

    [Fact]
    public void Result_SuccessesAndFailures_SplitTheResults()
    {
        Result<int, string>[] results = [1, "a", 2, "b"];

        Assert.Equal([1, 2], results.Successes());
        Assert.Equal(["a", "b"], results.Failures());
    }

    [Fact]
    public void Result_Partition_SplitsValuesAndErrors()
    {
        Result<int, string>[] results = [1, "a", 2, "b"];

        var (successes, failures) = results.Partition();

        Assert.Equal([1, 2], successes);
        Assert.Equal(["a", "b"], failures);
    }

    [Fact]
    public void Result_Sequence_KeepsAllValuesWhenAllAreSuccesses()
    {
        Result<int, string>[] results = [1, 2, 3];

        var sequenced = results.Sequence();

        Assert.True(sequenced.TryGetValue(out var present));
        Assert.Equal([1, 2, 3], present);
    }

    [Fact]
    public void Result_Sequence_ReturnsTheFirstFailure()
    {
        Result<int, string>[] results = [1, "a", "b"];

        var sequenced = results.Sequence();

        Assert.True(sequenced.TryGetError(out var error));
        Assert.Equal("a", error);
    }

    [Fact]
    public void Result_Traverse_ProjectsEveryItem()
    {
        var traversed = new[] { "1", "2", "3" }.Traverse(text
            => int.TryParse(text, out var value)
                ? Result<int, string>.Ok(value)
                : Result<int, string>.Fail($"not a number: {text}"));

        Assert.True(traversed.TryGetValue(out var present));
        Assert.Equal([1, 2, 3], present);
    }

    [Fact]
    public void Result_Traverse_ReturnsTheFirstFailure()
    {
        var traversed = new[] { "1", "x", "3" }.Traverse(text
            => int.TryParse(text, out var value)
                ? Result<int, string>.Ok(value)
                : Result<int, string>.Fail($"not a number: {text}"));

        Assert.True(traversed.TryGetError(out var error));
        Assert.Equal("not a number: x", error);
    }

    [Fact]
    public void Result_Sequence_ThrowsOnUninitializedResult()
    {
        Result<int, string>[] results = [1, default];

        Assert.Throws<InvalidOperationException>(() => results.Sequence());
    }

    [Fact]
    public void Result_QuerySyntax_ComposesResults()
    {
        var sum =
            from a in Result<int, string>.Ok(1) from b in Result<int, string>.Ok(2) select a + b;

        Assert.Equal(3, sum.Value);
    }

    [Fact]
    public void Result_QuerySyntax_StopsAtTheFirstFailure()
    {
        var sum =
            from a in Result<int, string>.Ok(1)
            from b in Result<int, string>.Fail("bad")
            select a + b;

        Assert.True(sum.TryGetError(out var error));
        Assert.Equal("bad", error);
    }

    [Fact]
    public void Result_Switch_RunsTheMatchingAction()
    {
        var tapped = 0;

        Result<int, string>.Ok(5).Switch(value => tapped = value, _ => tapped = -1);
        Assert.Equal(5, tapped);

        Result<int, string>.Fail("bad").Switch(_ => tapped = -1, error => tapped = error.Length);
        Assert.Equal(3, tapped);
    }

    [Fact]
    public void Result_GetOrThrow_ReturnsTheValueOrThrows()
    {
        Assert.Equal(5, Result<int, string>.Ok(5).GetOrThrow());

        var failure =
            Assert.Throws<InvalidOperationException>(()
                => Result<int, string>.Fail("bad").GetOrThrow());
        Assert.Contains("bad", failure.Message);

        var custom = Assert.Throws<ArgumentException>(()
            => Result<int, string>.Fail("bad").GetOrThrow(error => new ArgumentException(error)));
        Assert.Equal("bad", custom.Message);
    }
}
