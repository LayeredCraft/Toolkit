using LayeredCraft.Toolkit.Functional;

namespace LayeredCraft.Toolkit.Functional.Tests;

public sealed class MaybeTests
{
    [Fact]
    public void Some_ExposesValue()
    {
        var maybe = Maybe<int>.Some(42);

        Assert.True(maybe.HasValue);
        Assert.False(maybe.IsNone);
        Assert.Equal(42, maybe.Value);
    }

    [Fact]
    public void Default_IsNone()
    {
        Maybe<int> maybe = default;

        Assert.False(maybe.HasValue);
        Assert.True(maybe.IsNone);
    }

    [Fact]
    public void ImplicitConversion_WrapsValue()
    {
        Maybe<string> maybe = "value";

        Assert.Equal("value", maybe.Value);
    }

    [Fact]
    public void ImplicitConversion_FromNone_IsEmpty()
    {
        Maybe<string> maybe = Maybe.None;

        Assert.True(maybe.IsNone);
    }

    [Fact]
    public void Some_RejectsNull()
        => Assert.Throws<ArgumentNullException>(() => Maybe<string>.Some(null!));

    [Fact]
    public void Value_OnNone_Throws()
    {
        var maybe = Maybe<int>.None;

        Assert.Throws<InvalidOperationException>(() => _ = maybe.Value);
    }

    [Fact]
    public void TryGetValue_ReportsPresence()
    {
        var present = Maybe<int>.Some(7);
        var absent = Maybe<int>.None;

        Assert.True(present.TryGetValue(out var value));
        Assert.Equal(7, value);
        Assert.False(absent.TryGetValue(out _));
    }

    [Fact]
    public void Match_ProjectsBothCases()
    {
        Assert.Equal("some:1", Maybe<int>.Some(1).Match(v => $"some:{v}", () => "none"));
        Assert.Equal("none", Maybe<int>.None.Match(v => $"some:{v}", () => "none"));
    }

    [Fact]
    public void Map_Bind_And_Filter_ShortCircuit()
    {
        Assert.Equal(10, Maybe<int>.Some(5).Map(v => v * 2).Value);
        Assert.True(Maybe<int>.None.Map(v => v * 2).IsNone);

        Assert.Equal(6, Maybe<int>.Some(5).Bind(v => Maybe<int>.Some(v + 1)).Value);
        Assert.True(Maybe<int>.None.Bind(v => Maybe<int>.Some(v + 1)).IsNone);

        Assert.Equal(5, Maybe<int>.Some(5).Filter(v => v > 4).Value);
        Assert.True(Maybe<int>.Some(5).Filter(v => v > 5).IsNone);
    }

    [Fact]
    public void GetOrElse_And_OrElse_UseFallbacks()
    {
        Assert.Equal(1, Maybe<int>.None.GetOrElse(1));
        Assert.Equal(5, Maybe<int>.Some(5).GetOrElse(1));

        Assert.Equal(2, Maybe<int>.None.OrElse(Maybe<int>.Some(2)).Value);
        Assert.Equal(5, Maybe<int>.Some(5).OrElse(Maybe<int>.Some(2)).Value);
    }

    [Fact]
    public void Fold_Tap_And_TapNone_RunExpectedBranches()
    {
        Assert.Equal(6, Maybe<int>.Some(5).Fold(1, (seed, value) => seed + value));
        Assert.Equal(1, Maybe<int>.None.Fold(1, (seed, value) => seed + value));

        var tapped = 0;
        var noneTapped = false;
        var some = Maybe<int>.Some(3);
        var none = Maybe<int>.None;

        Assert.Equal(some, some.Tap(v => tapped = v));
        Assert.False(noneTapped);
        Assert.Equal(3, tapped);

        Assert.Equal(none, none.TapNone(() => noneTapped = true));
        Assert.True(noneTapped);
    }

    [Fact]
    public void Zip_RequiresBothValues()
    {
        var both = Maybe<int>.Some(1).Zip(Maybe<string>.Some("a"));
        var missing = Maybe<int>.Some(1).Zip(Maybe<string>.None);

        Assert.Equal((1, "a"), both.Value);
        Assert.True(missing.IsNone);
    }

    [Fact]
    public void Flatten_UnwrapsNestedMaybe()
    {
        Assert.Equal(1, Maybe<Maybe<int>>.Some(Maybe<int>.Some(1)).Flatten().Value);
        Assert.True(Maybe<Maybe<int>>.Some(Maybe<int>.None).Flatten().IsNone);
    }

    [Fact]
    public void Contains_Exists_And_ForAll_InspectPresentValues()
    {
        var present = Maybe<string>.Some("abc");

        Assert.True(present.Contains("abc"));
        Assert.False(present.Contains("abd"));
        Assert.True(present.Exists(v => v.Length == 3));
        Assert.False(present.Exists(v => v.Length == 2));
        Assert.True(present.ForAll(v => v.Length == 3));
        Assert.True(Maybe<string>.None.ForAll(v => v.Length == 3));
    }

    [Fact]
    public void ToNullable_ConvertsBothShapes()
    {
        Assert.Equal(1, Maybe<int>.Some(1).ToNullable());
        Assert.Null(Maybe<int>.None.ToNullable());
        Assert.Equal("a", Maybe<string>.Some("a").ToNullableReference());
        Assert.Null(Maybe<string>.None.ToNullableReference());
    }

    [Fact]
    public void ToEnumerable_YieldsZeroOrOne()
    {
        Assert.Equal([1], Maybe<int>.Some(1).ToEnumerable());
        Assert.Empty(Maybe<int>.None.ToEnumerable());
    }

    [Fact]
    public void ToResult_UsesFixedOrFactoryError()
    {
        var present = Maybe<int>.Some(1).ToResult("missing");
        var absent = Maybe<int>.None.ToResult("missing");
        var lazy = Maybe<int>.None.ToResult(() => "lazy");

        Assert.True(present.IsSuccess);
        Assert.Equal(1, present.Value);
        Assert.Equal("missing", absent.Error);
        Assert.Equal("lazy", lazy.Error);
    }

    [Fact]
    public async Task AsyncHelpers_Compose()
    {
        var mapped = await Maybe<int>.Some(2).MapAsync(v => Task.FromResult(v * 3));
        var bound =
            await Maybe<int>.Some(2).BindAsync(v => Task.FromResult(Maybe<int>.Some(v + 1)));
        var tapped = 0;

        Assert.Equal(6, mapped.Value);
        Assert.Equal(3, bound.Value);
        Assert.True((await Maybe<int>.None.MapAsync(v => Task.FromResult(v * 3))).IsNone);

        await Maybe<int>
            .Some(4)
            .TapAsync(v =>
            {
                tapped = v;
                return Task.CompletedTask;
            });
        Assert.Equal(4, tapped);

        var fromTask = await Task.FromResult(Maybe<int>.Some(5)).MapAsync(v => v + 1);
        var boundFromTask =
            await Task
                .FromResult(Maybe<int>.Some(5))
                .BindAsync(v => Task.FromResult(Maybe<int>.Some(v + 1)));

        Assert.Equal(6, fromTask.Value);
        Assert.Equal(6, boundFromTask.Value);
    }

    [Fact]
    public void Equality_ComparesCaseAndValue()
    {
        Assert.Equal(Maybe<int>.Some(1), Maybe<int>.Some(1));
        Assert.NotEqual(Maybe<int>.Some(1), Maybe<int>.Some(2));
        Assert.Equal(Maybe<int>.None, Maybe<int>.None);
        Assert.NotEqual(Maybe<int>.Some(1), Maybe<int>.None);
        Assert.Equal(Maybe<int>.Some(1).GetHashCode(), Maybe<int>.Some(1).GetHashCode());
    }

    [Fact]
    public void Maybe_IsAValueType()
    {
        Assert.True(typeof(Maybe<int>).IsValueType);
        Assert.True(typeof(None).IsValueType);
    }

    [Fact]
    public void Union_PatternMatching_UnwrapsTheCase()
    {
        static string Describe(Maybe<int> maybe)
            => maybe switch
            {
                int value => $"some:{value}",
                null => "none",
            };

        Assert.Equal("some:42", Describe(Maybe<int>.Some(42)));
        Assert.Equal("none", Describe(Maybe<int>.None));
        Assert.Equal("none", Describe(default));
    }

    [Fact]
    public void Union_Conversion_AcceptsTheCaseType()
    {
        Maybe<int> fromValue = 7;
        Maybe<string> fromNone = Maybe.None;

        Assert.Equal(7, fromValue.Value);
        Assert.True(fromNone.IsNone);
    }

    [Fact]
    public void Union_SwitchWithoutNullArm_ThrowsOnDefault()
    {
        static string Describe(Maybe<int> maybe)
            => maybe switch
            {
                int value => $"some:{value}",
            };

        Assert.Equal("some:1", Describe(Maybe<int>.Some(1)));
        Assert.Throws<System.Runtime.CompilerServices.SwitchExpressionException>(()
            => Describe(default));
    }
}
