using LayeredCraft.Toolkit.Functional;

namespace LayeredCraft.Toolkit.Functional.Tests;

public sealed class ResultOfTTests
{
    [Fact]
    public void Ok_And_Fail_ExposeTheirPayloads()
    {
        var ok = Result<int>.Ok(1);
        var fail = Result<int>.Fail(new Error("bad"));

        Assert.True(ok.IsSuccess);
        Assert.False(ok.IsFailure);
        Assert.False(ok.IsUninitialized);
        Assert.Equal(1, ok.Value);

        Assert.False(fail.IsSuccess);
        Assert.True(fail.IsFailure);
        Assert.False(fail.IsUninitialized);
        Assert.Equal("bad", fail.Error.Message);
    }

    [Fact]
    public void Default_IsUninitializedFailure()
    {
        var result = default(Result<int>);

        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.True(result.IsUninitialized);
        Assert.Equal(Error.Uninitialized, result.Error);
        Assert.True(result.Error.IsUninitialized);
        Assert.Throws<InvalidOperationException>(() => _ = result.Value);
    }

    [Fact]
    public void Default_MatchesTheErrorArmWithoutANullArm()
    {
        static string Describe(Result<int> result) => result switch
        {
            int value => $"ok:{value}",
            Error error => $"error:{error.Message}",
        };

        Assert.Equal("ok:1", Describe(Result<int>.Ok(1)));
        Assert.Equal("error:bad", Describe(Result<int>.Fail(new Error("bad"))));
        Assert.Equal("error:The result was not initialized.", Describe(default));
    }

    [Fact]
    public void TryGet_ReportsCaseForDefault()
    {
        var result = default(Result<int>);

        Assert.False(result.TryGetValue(out _));
        Assert.True(result.TryGetError(out var error));
        Assert.Equal(Error.Uninitialized, error);
    }

    [Fact]
    public void TryGetError_OnSuccess_ReturnsFalseWithoutThrowing()
    {
        var result = Result<int>.Ok(1);

        Assert.False(result.TryGetError(out var error));
        Assert.Equal(default(Error), error);
    }

    [Fact]
    public void Union_ErrorArmFirst_OnSuccess_DoesNotThrow()
    {
        static string Describe(Result<int> result) => result switch
        {
            Error error => $"error:{error.Code}",
            int value => $"ok:{value}",
        };

        Assert.Equal("ok:1", Describe(Result<int>.Ok(1)));
        Assert.Equal(
            "error:BOOM",
            Describe(Result<int>.Fail(new Error("boom", Code: "BOOM"))));
    }

    [Fact]
    public void Map_And_Bind_ShortCircuitTheDefaultFailure()
    {
        var result = default(Result<int>);

        Assert.Equal(Error.Uninitialized, result.Map(v => v + 1).Error);
        Assert.Equal(Error.Uninitialized, result.Bind(v => Result<int>.Ok(v + 1)).Error);
        Assert.Equal(2, Result<int>.Ok(1).Map(v => v + 1).Value);
        Assert.Equal(2, Result<int>.Ok(1).Bind(v => Result<int>.Ok(v + 1)).Value);
    }

    [Fact]
    public void MapError_PreservesTheDefaultFailure()
    {
        var result = default(Result<int>).MapError(error => new Error("mapped", Code: error.Code));

        Assert.True(result.IsUninitialized);
        Assert.Equal(Error.Uninitialized, result.Error);
    }

    [Fact]
    public void Ensure_RejectsValuesThatFailThePredicate()
    {
        var ok = Result<int>.Ok(5);

        Assert.Equal(5, ok.Ensure(v => v > 4, new Error("too small")).Value);
        Assert.Equal("too small", ok.Ensure(v => v > 5, new Error("too small")).Error.Message);
        Assert.Equal(
            "rejected 5",
            ok.Ensure(v => v > 5, v => new Error($"rejected {v}")).Error.Message);
    }

    [Fact]
    public void GetOrElse_And_OrElse_UseFallbacks()
    {
        Assert.Equal(1, Result<int>.Ok(1).GetOrElse(0));
        Assert.Equal(0, Result<int>.Fail(new Error("bad")).GetOrElse(0));
        Assert.Equal(3, Result<int>.Fail(new Error("bad")).GetOrElse(e => e.Message.Length));

        Assert.Equal(1, Result<int>.Ok(1).OrElse(Result<int>.Ok(2)).Value);
        Assert.Equal(2, Result<int>.Fail(new Error("bad")).OrElse(Result<int>.Ok(2)).Value);
    }

    [Fact]
    public void Contains_Exists_And_ForAll_InspectSuccessValues()
    {
        var ok = Result<int>.Ok(1);
        var fail = Result<int>.Fail(new Error("bad"));

        Assert.True(ok.Contains(1));
        Assert.False(ok.Contains(2));
        Assert.True(ok.Exists(v => v == 1));
        Assert.False(fail.Exists(_ => true));
        Assert.True(ok.ForAll(v => v == 1));
        Assert.True(fail.ForAll(_ => false));
    }

    [Fact]
    public void Try_WrapsTheExceptionAndUsesItsTypeAsTheCode()
    {
        var result = Result<int>.Try(() => throw new InvalidOperationException("boom"));

        Assert.True(result.IsFailure);
        Assert.Equal("boom", result.Error.Message);
        Assert.Equal(nameof(InvalidOperationException), result.Error.Code);
        Assert.IsType<InvalidOperationException>(result.Error.Exception);
    }

    [Fact]
    public async Task TryAsync_WrapsTheException()
    {
        var result = await Result<int>.TryAsync(() => Task.FromException<int>(new TimeoutException("slow")));

        Assert.True(result.IsFailure);
        Assert.Equal(nameof(TimeoutException), result.Error.Code);
    }

    [Fact]
    public void Equality_TreatsDefaultAndExplicitUninitializedAsEqual()
    {
        var uninitialized = default(Result<int>);
        var explicitFailure = Result<int>.Fail(Error.Uninitialized);

        Assert.Equal(uninitialized, explicitFailure);
        Assert.Equal(uninitialized.GetHashCode(), explicitFailure.GetHashCode());
        Assert.NotEqual(Result<int>.Ok(1), Result<int>.Fail(new Error("bad")));
    }

    [Fact]
    public void Union_Conversion_AcceptsBothCaseTypes()
    {
        Result<int> fromValue = 42;
        Result<int> fromError = new Error("bad");

        Assert.Equal(42, fromValue.Value);
        Assert.Equal("bad", fromError.Error.Message);
    }

    [Fact]
    public void ConvertsToAndFromTheGenericResult()
    {
        Result<int> fixedResult = Result<int>.Ok(1);
        Result<int, Error> generic = fixedResult;
        Assert.Equal(1, generic.Value);

        Result<int> roundTrip = generic;
        Assert.Equal(1, roundTrip.Value);

        Result<int> fromDefault = default(Result<int, Error>);
        Assert.Equal(Error.Uninitialized, fromDefault.Error);

        Result<int, Error> fromUninitialized = default(Result<int>);
        Assert.Equal(Error.Uninitialized, fromUninitialized.Error);
    }

    [Fact]
    public void Maybe_ToResult_ProducesTheFixedType()
    {
        var present = Maybe<int>.Some(1).ToResult(new Error("missing"));
        var absent = Maybe<int>.None.ToResult(new Error("missing"));

        Assert.Equal(1, present.Value);
        Assert.Equal("missing", absent.Error.Message);
    }

    [Fact]
    public void SequenceHelpers_ShortCircuitOnTheFirstFailure()
    {
        var results = new[] { Result<int>.Ok(1), default, Result<int>.Ok(3) };

        Assert.Equal(new[] { 1, 3 }, results.Successes());
        Assert.Equal(new[] { Error.Uninitialized }, results.Failures());
        Assert.Equal(new[] { 1, 3 }, results.Partition().Successes);
        Assert.Equal(new[] { Error.Uninitialized }, results.Partition().Failures);
        Assert.Equal(Error.Uninitialized, results.Sequence().Error);
        Assert.Equal(Error.Uninitialized, results.Traverse(v => v).Error);
    }

    [Fact]
    public void Switch_HandlesTheDefaultFailure()
    {
        string? observed = null;

        default(Result<int>).Switch(
            _ => observed = "ok",
            error => observed = error.Message);

        Assert.Equal("The result was not initialized.", observed);
    }

    [Fact]
    public void Composition_PreservesTheUninitializedDefault()
    {
        var result = default(Result<int>);

        Assert.True(result.Map(v => v + 1).IsUninitialized);
        Assert.True(result.Bind(v => Result<int>.Ok(v + 1)).IsUninitialized);
        Assert.True(result.MapError(_ => new Error("mapped")).IsUninitialized);
        Assert.True(result.Ensure(v => true, new Error("bad")).IsUninitialized);
        Assert.True(Result<Result<int>>.Ok(default).Flatten().IsUninitialized);
    }

    [Fact]
    public async Task Composition_PreservesTheUninitializedDefaultAsynchronously()
    {
        var result = default(Result<int>);

        Assert.True((await result.MapAsync(v => Task.FromResult(v + 1))).IsUninitialized);
        Assert.True((await result.BindAsync(v => Task.FromResult(Result<int>.Ok(v + 1))))
            .IsUninitialized);
        Assert.True((await result.MapErrorAsync(_ => Task.FromResult(new Error("mapped"))))
            .IsUninitialized);
        Assert.True((await result.EnsureAsync(_ => Task.FromResult(true), new Error("bad")))
            .IsUninitialized);
    }

    [Fact]
    public async Task AsyncParityHelpers_Compose()
    {
        var failed = Result<int>.Fail(new Error("bad", Code: "BAD"));

        Assert.Equal(
            "mapped",
            (await failed.MapErrorAsync(_ => Task.FromResult(new Error("mapped")))).Error.Message);
        Assert.Equal(
            5,
            (await Result<int>.Ok(5).EnsureAsync(v => Task.FromResult(v > 4), new Error("small")))
                .Value);
        Assert.Equal(
            "rejected 5",
            (await Result<int>.Ok(5).EnsureAsync(
                v => Task.FromResult(v > 5),
                v => new Error($"rejected {v}"))).Error.Message);

        Error? tapped = null;
        await failed.TapErrorAsync(error =>
        {
            tapped = error;
            return Task.CompletedTask;
        });
        Assert.Equal("BAD", tapped?.Code);

        Assert.Equal(0, await failed.GetOrElseAsync(_ => Task.FromResult(0)));
        Assert.Equal(2, (await failed.OrElseAsync(_ => Task.FromResult(Result<int>.Ok(2)))).Value);
        Assert.Equal(
            "ok:1",
            await Result<int>.Ok(1).MatchAsync(
                value => Task.FromResult($"ok:{value}"),
                error => Task.FromResult($"error:{error.Message}")));

        var switched = 0;
        await Result<int>.Ok(1).SwitchAsync(
            value =>
            {
                switched = value;
                return Task.CompletedTask;
            },
            _ => Task.CompletedTask);
        Assert.Equal(1, switched);

        Assert.True(await Result<int>.Ok(1).ExistsAsync(v => Task.FromResult(v == 1)));
        Assert.True(await failed.ForAllAsync(_ => Task.FromResult(false)));
        Assert.True(await Task.FromResult(Result<int>.Ok(1)).ContainsAsync(1));

        Assert.Equal(
            "mapped",
            (await Task.FromResult(failed).MapErrorAsync(
                _ => Task.FromResult(new Error("mapped")))).Error.Message);
        Assert.Equal(
            "ok:1",
            await Task.FromResult(Result<int>.Ok(1)).MatchAsync(
                value => Task.FromResult($"ok:{value}"),
                error => Task.FromResult($"error:{error.Message}")));
    }
}
