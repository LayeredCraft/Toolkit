using LayeredCraft.Toolkit.Functional;

namespace LayeredCraft.Toolkit.Functional.Tests;

public sealed class ResultTests
{
    [Fact]
    public void Ok_And_Fail_ExposeTheirPayloads()
    {
        var ok = Result<int, string>.Ok(1);
        var fail = Result<int, string>.Fail("bad");

        Assert.True(ok.IsSuccess);
        Assert.False(ok.IsFailure);
        Assert.True(ok.IsInitialized);
        Assert.Equal(1, ok.Value);

        Assert.False(fail.IsSuccess);
        Assert.True(fail.IsFailure);
        Assert.True(fail.IsInitialized);
        Assert.Equal("bad", fail.Error);
    }

    [Fact]
    public void Default_IsUninitialized()
    {
        var result = default(Result<int, string>);

        Assert.False(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.False(result.IsInitialized);
        Assert.Throws<InvalidOperationException>(() => _ = result.Value);
        Assert.Throws<InvalidOperationException>(() => _ = result.Error);
        Assert.Throws<InvalidOperationException>(() => result.Match(_ => 1, _ => 0));
    }

    [Fact]
    public void Accessors_ThrowForTheWrongCase()
    {
        var ok = Result<int, string>.Ok(1);
        var fail = Result<int, string>.Fail("bad");

        Assert.Throws<InvalidOperationException>(() => _ = ok.Error);
        Assert.Throws<InvalidOperationException>(() => _ = fail.Value);
    }

    [Fact]
    public void TryGet_ReportsCase()
    {
        var ok = Result<int, string>.Ok(1);
        var fail = Result<int, string>.Fail("bad");

        Assert.True(ok.TryGetValue(out var value));
        Assert.Equal(1, value);
        Assert.False(ok.TryGetError(out _));

        Assert.True(fail.TryGetError(out var error));
        Assert.Equal("bad", error);
        Assert.False(fail.TryGetValue(out _));
    }

    [Fact]
    public void Match_ProjectsBothCases()
    {
        Assert.Equal("ok:1", Result<int, string>.Ok(1).Match(v => $"ok:{v}", e => $"fail:{e}"));
        Assert.Equal("fail:bad", Result<int, string>.Fail("bad").Match(v => $"ok:{v}", e => $"fail:{e}"));
    }

    [Fact]
    public void Map_And_Bind_ShortCircuit()
    {
        Assert.Equal(2, Result<int, string>.Ok(1).Map(v => v + 1).Value);
        Assert.Equal("bad", Result<int, string>.Fail("bad").Map(v => v + 1).Error);

        Assert.Equal(2, Result<int, string>.Ok(1).Bind(v => Result<int, string>.Ok(v + 1)).Value);
        Assert.Equal("bad", Result<int, string>.Fail("bad").Bind(v => Result<int, string>.Ok(v + 1)).Error);
    }

    [Fact]
    public void MapError_TransformsFailuresOnly()
    {
        Assert.Equal(1, Result<int, string>.Ok(1).MapError(e => e.Length).Value);
        Assert.Equal(3, Result<int, string>.Fail("bad").MapError(e => e.Length).Error);
    }

    [Fact]
    public void Tap_And_TapError_ObserveMatchingCase()
    {
        string? tapped = null;
        string? errorTapped = null;

        Assert.Equal(1, Result<int, string>.Ok(1)
            .Tap(v => tapped = $"ok:{v}")
            .TapError(e => errorTapped = e).Value);
        Assert.Equal("ok:1", tapped);
        Assert.Null(errorTapped);

        Result<int, string>.Fail("bad")
            .Tap(v => tapped = $"ok:{v}")
            .TapError(e => errorTapped = e);
        Assert.Equal("bad", errorTapped);
    }

    [Fact]
    public void Ensure_RejectsValuesThatFailThePredicate()
    {
        var ok = Result<int, string>.Ok(5);

        Assert.Equal(5, ok.Ensure(v => v > 4, "too small").Value);
        Assert.Equal("too small", ok.Ensure(v => v > 5, "too small").Error);
        Assert.Equal("rejected 5", ok.Ensure(v => v > 5, v => $"rejected {v}").Error);
    }

    [Fact]
    public void GetOrElse_And_OrElse_UseFallbacks()
    {
        Assert.Equal(1, Result<int, string>.Ok(1).GetOrElse(0));
        Assert.Equal(0, Result<int, string>.Fail("bad").GetOrElse(0));
        Assert.Equal(3, Result<int, string>.Fail("bad").GetOrElse(e => e.Length));

        Assert.Equal(1, Result<int, string>.Ok(1).OrElse(Result<int, string>.Ok(2)).Value);
        Assert.Equal(2, Result<int, string>.Fail("bad").OrElse(Result<int, string>.Ok(2)).Value);
        Assert.Equal(3, Result<int, string>.Fail("bad")
            .OrElse(e => Result<int, string>.Ok(e.Length)).Value);
    }

    [Fact]
    public void ToMaybe_And_ToEnumerable_KeepOnlySuccess()
    {
        Assert.Equal(1, Result<int, string>.Ok(1).ToMaybe().Value);
        Assert.True(Result<int, string>.Fail("bad").ToMaybe().IsNone);

        Assert.Equal([1], Result<int, string>.Ok(1).ToEnumerable());
        Assert.Empty(Result<int, string>.Fail("bad").ToEnumerable());
    }

    [Fact]
    public void Flatten_UnwrapsNestedResult()
    {
        var ok = Result<Result<int, string>, string>.Ok(Result<int, string>.Ok(1));
        var fail = Result<Result<int, string>, string>.Ok(Result<int, string>.Fail("bad"));

        Assert.Equal(1, ok.Flatten().Value);
        Assert.Equal("bad", fail.Flatten().Error);
    }

    [Fact]
    public void Try_CapturesExceptions()
    {
        var ok = Result<int, string>.Try(() => 1, e => e.Message);
        var fail = Result<int, string>.Try(
            () => throw new InvalidOperationException("boom"),
            e => e.Message);

        Assert.Equal(1, ok.Value);
        Assert.Equal("boom", fail.Error);
    }

    [Fact]
    public async Task TryAsync_CapturesExceptions()
    {
        var ok = await Result<int, string>.TryAsync(() => Task.FromResult(1), e => e.Message);
        var fail = await Result<int, string>.TryAsync(
            () => Task.FromException<int>(new InvalidOperationException("boom")),
            e => e.Message);

        Assert.Equal(1, ok.Value);
        Assert.Equal("boom", fail.Error);
    }

    [Fact]
    public async Task AsyncHelpers_Compose()
    {
        var mapped = await Result<int, string>.Ok(2).MapAsync(v => Task.FromResult(v * 3));
        var bound = await Result<int, string>.Ok(2)
            .BindAsync(v => Task.FromResult(Result<int, string>.Ok(v + 1)));
        var tapped = 0;

        Assert.Equal(6, mapped.Value);
        Assert.Equal(3, bound.Value);
        Assert.Equal("bad", (await Result<int, string>.Fail("bad")
            .MapAsync(v => Task.FromResult(v * 3))).Error);

        await Result<int, string>.Ok(4).TapAsync(v =>
        {
            tapped = v;
            return Task.CompletedTask;
        });
        Assert.Equal(4, tapped);

        var fromTask = await Task.FromResult(Result<int, string>.Ok(5)).MapAsync(v => v + 1);
        var boundFromTask = await Task.FromResult(Result<int, string>.Ok(5))
            .BindAsync(v => Task.FromResult(Result<int, string>.Ok(v + 1)));
        var tappedFromTask = 0;

        await Task.FromResult(Result<int, string>.Ok(6)).TapAsync(v =>
        {
            tappedFromTask = v;
            return Task.CompletedTask;
        });

        Assert.Equal(6, fromTask.Value);
        Assert.Equal(6, boundFromTask.Value);
        Assert.Equal(6, tappedFromTask);
    }

    [Fact]
    public void Equality_ComparesCaseAndPayload()
    {
        Assert.Equal(Result<int, string>.Ok(1), Result<int, string>.Ok(1));
        Assert.NotEqual(Result<int, string>.Ok(1), Result<int, string>.Ok(2));
        Assert.Equal(Result<int, string>.Fail("bad"), Result<int, string>.Fail("bad"));
        Assert.NotEqual(Result<int, string>.Ok(1), Result<int, string>.Fail("bad"));
        Assert.Equal(Result<int, string>.Ok(1).GetHashCode(), Result<int, string>.Ok(1).GetHashCode());
    }
}
