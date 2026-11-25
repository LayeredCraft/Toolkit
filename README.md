# LayeredCraft.Toolkit.Functional

Non-boxing functional primitives for .NET 11. The package provides `Maybe<T>`,
`Result<TValue, TError>`, `Error`, `Unit`, and composition helpers for synchronous and
asynchronous code. It has no runtime dependencies.

`Maybe<T>` and `Result<TValue, TError>` are C# 15 union types: `switch` expressions unwrap their
cases and the compiler warns about a missing case. The union storage is hand-written so a union
never boxes a payload and never stores an `object`.

This README is the documentation for the package. There is no separate doc site.

## Why this package exists

A plain `union Maybe<T>(T, None)` declaration stores its contents as `object?`, so every value type
is boxed. These types use the C# 15 custom union support instead:

- `[Union]` plus `IUnion` makes the type a real union for conversions, pattern matching, and
  exhaustiveness.
- A nested `IUnionMembers` provider declares the case types with `static Create` and the
  **non-boxing access pattern** (`HasValue` and `TryGetValue`).
- `Maybe<T>` stores `T` and a `bool`; `Result<TValue, TError>` stores both payloads and a `byte`
  case tag.
- The provider keeps the strongly typed `Value` and `Error` API; the `object?` union view is
  explicit and is only used when a caller asks for it.

The types are deliberately boring: constructors are factories, helpers are extension methods, and
matching is a normal `switch`. Union conversion lets a case type convert implicitly, so
`Maybe<int> maybe = 42;` and `Result<int, Error> result = 42;` work.

## Requirements

- .NET 11 or later (C# 15).
- Topics and packages that consume these types must also target .NET 11 or later; union pattern
  matching is a C# 15 feature.

The package has no external dependencies and enables the trim and Native AOT analyzers.

## Install

```bash
dotnet add package LayeredCraft.Toolkit.Functional
```

## Quick start

```csharp
using LayeredCraft.Toolkit.Functional;

var quantity = ParseQuantity("3")
    .Map(n => n * 2)
    .Filter(n => n <= 10)
    .GetOrElse(0);

var price = ParsePrice("19.99")
    .Bind(value => value > 0
        ? Result<decimal, Error>.Ok(value)
        : Result<decimal, Error>.Fail("Price must be positive."))
    .Map(value => Math.Round(value, 2))
    .Tap(value => Console.WriteLine($"price: {value}"));

Console.WriteLine(price.Match(
    value => $"total: {value * quantity}",
    error => $"failed: {error.Message}"));

static Maybe<int> ParseQuantity(string text)
    => int.TryParse(text, out var value) && value > 0 ? value : Maybe.None;

static Result<decimal, Error> ParsePrice(string text)
    => decimal.TryParse(text, out var value)
        ? Result<decimal, Error>.Ok(value)
        : Result<decimal, Error>.Fail("Price must be a number.");
```

The runnable version lives in
[`samples/LayeredCraft.Toolkit.Functional.Sample`](samples/LayeredCraft.Toolkit.Functional.Sample/Program.cs).

## `Maybe<T>`

`Maybe<T>` is a union with the single case type `T`: either `Some(value)` or `None`. `T` is
constrained to `notnull`, so a missing reference is never confused with a present `null`. Absent is
the `null` contents of the union.

```csharp
Maybe<int> present = 42;      // union conversion from the case type
Maybe<int> absent = Maybe.None;

// Exhaustive: the compiler warns when a case type is missing.
var label = present switch
{
    int value => $"value: {value}",
    null => "missing",
};

if (present.TryGetValue(out var value))
    Console.WriteLine(value);
```

`default(Maybe<T>)` is `None`. That makes a struct field or a `default` initialization safe.

The `null` arm is the empty case, not a nullability check. A struct union's `default` always has `null`
contents, so `default(Maybe<int>)` is `None` and only the `null` arm matches it. The compiler requires
that arm only when a case type is nullable; when every case type is non-nullable it compiles without
one and throws `SwitchExpressionException` on a default value. Include the `null` arm wherever a
default can reach the switch.

### `Maybe<T>` members

| Member                       | Purpose                                                          |
| ---------------------------- | ---------------------------------------------------------------- |
| `Maybe<T>.Some(value)`       | Creates a present value. Throws when `value` is `null`.          |
| `Maybe<T>.None`              | Creates the empty case.                                          |
| `Maybe.None`                 | Typed empty case that converts to any `Maybe<T>`.                |
| `HasValue` / `IsNone`        | Reports the case without matching.                               |
| `Value`                      | Returns the present value or throws `InvalidOperationException`. |
| `TryGetValue(out T)`         | Reads the value without throwing or boxing.                      |
| `Match(some, none)`          | Projects both cases into one result.                             |
| union conversion from `T`    | Wraps a value without calling `Some`.                            |
| implicit `None` → `Maybe<T>` | Lets `return Maybe.None;` work in a `Maybe<T>` method.           |

### `Maybe<T>` helpers

| Helper                                | Purpose                                                              |
| ------------------------------------- | -------------------------------------------------------------------- |
| `ToMaybe()`                           | Wraps a nullable value type or reference.                            |
| `Map` / `Bind`                        | Transforms or chains a present value.                                |
| `Select` / `SelectMany` / `Where`     | LINQ query syntax forms of `Map`, `Bind`, and `Filter`.              |
| `Switch`                              | Runs an action for the present or absent case.                       |
| `Filter`                              | Keeps a value only when a predicate passes.                          |
| `GetOrElse` / `OrElse`                | Supplies a value or another optional value.                          |
| `Fold`                                | Reduces both cases to one value.                                     |
| `Tap` / `TapNone`                     | Observes the matching case and returns the original value.           |
| `Zip`                                 | Combines two optional values into an optional tuple.                 |
| `Flatten`                             | Turns `Maybe<Maybe<T>>` into `Maybe<T>`.                             |
| `Contains` / `Exists` / `ForAll`      | Inspects a present value.                                            |
| `ToNullable` / `ToNullableReference`  | Converts back to a nullable value or reference.                      |
| `ToEnumerable`                        | Yields zero or one values.                                           |
| `ToResult`                            | Converts `None` into a failure with a fixed or lazily created error. |
| `MapAsync` / `BindAsync` / `TapAsync` | Asynchronous composition.                                            |

The `Task<Maybe<T>>` receiver also has `MapAsync` and `BindAsync` overloads for fluent async chains.

`Select`, `SelectMany`, and `Where` make query syntax work on optional values:

```csharp
var total =
    from price in Maybe<decimal>.Some(19.99m)
    from quantity in ParseQuantity("3")
    select price * quantity;
```

## Collections

Sequence helpers keep the first absent or failed value as the short-circuit point. They live in
`MaybeSequenceExtensions` and `ResultSequenceExtensions`.

| Helper                   | Receiver                              | Purpose                                                                         |
| ------------------------ | ------------------------------------- | ------------------------------------------------------------------------------- |
| `Choose`                 | `IEnumerable<Maybe<T>>`               | Yields the present values and drops the absent ones.                            |
| `Sequence`               | `IEnumerable<Maybe<T>>`               | Returns all values, or `None` when any value is absent.                         |
| `Traverse`               | `IEnumerable<T>`                      | Maps each item and returns all values, or `None` when any projection is absent. |
| `FirstOrNone`            | `IEnumerable<T>`                      | Returns the first item (optionally matching a predicate), or `None`.            |
| `Successes` / `Failures` | `IEnumerable<Result<TValue, TError>>` | Yields the successes or the failures.                                           |
| `Partition`              | `IEnumerable<Result<TValue, TError>>` | Splits the results into success and failure lists.                              |
| `Sequence`               | `IEnumerable<Result<TValue, TError>>` | Returns all values, or the first failure.                                       |
| `Traverse`               | `IEnumerable<T>`                      | Maps each item and returns all values, or the first failure.                    |

```csharp
var totals = orders
    .Traverse(order => order.Validate())
    .Map(valid => valid.Sum(order => order.Total));
```

`Result.Sequence` and `Result.Traverse` throw `InvalidOperationException` when a sequence contains an
uninitialized result, because that is a bug rather than a failure.

## `Result<TValue, TError>`

`Result<TValue, TError>` is a union with two case types: `Ok(value)` or `Fail(error)`. Both payload
types are `notnull`, and the case tag is separate from the payloads, so the typed `Value` and
`Error` properties always report the case that was created.

```csharp
public Result<Order, Error> LoadOrder(Guid id)
{
    var order = repository.Find(id);

    return order is null
        ? Result<Order, Error>.Fail("Order not found.")
        : Result<Order, Error>.Ok(order);
}

var message = LoadOrder(id) switch
{
    Order order => $"Loaded {order.Id}",
    Error error => $"Failed: {error.Message}",
    null => "Uninitialized",   // default(Result<...>)
};

var viaMatch = LoadOrder(id).Match(
    order => $"Loaded {order.Id}",
    error => $"Failed: {error.Message}");
```

`default(Result<TValue, TError>)` is **uninitialized**, not a success or a failure. It is the
`null` contents of the union. Accessing `Value`, `Error`, or `Match` on it throws, and a `switch`
reaches it through the `null` arm. This catches a forgotten factory call instead of silently
treating a default value as a valid success.

The `null` arm is the default state, not a nullability check. The compiler does not require it when
both payload types are non-nullable, so a switch without it compiles and throws on a default value.

The two case types must stay distinguishable when pattern matching: `Result<string, string>` still
tracks success and failure through the tag, but a `switch` cannot tell the two `string` cases apart.
Use distinct payload types when you pattern match.

### `Result<TValue, TError>` members

| Member                                      | Purpose                                                             |
| ------------------------------------------- | ------------------------------------------------------------------- |
| `Result<TValue, TError>.Ok(value)`          | Creates a success. Throws when `value` is `null`.                   |
| `Result<TValue, TError>.Fail(error)`        | Creates a failure. Throws when `error` is `null`.                   |
| `Try(operation, errorFactory)`              | Runs a function and captures an exception.                          |
| `TryAsync(operation, errorFactory)`         | Awaits a function and captures an exception.                        |
| `IsSuccess` / `IsFailure` / `IsInitialized` | Reports the case.                                                   |
| `Value` / `Error`                           | Returns the matching payload or throws `InvalidOperationException`. |
| `TryGetValue(out TValue)`                   | Reads a success without throwing or boxing.                         |
| `TryGetError(out TError)`                   | Reads a failure without throwing or boxing.                         |
| `Match(success, failure)`                   | Projects both cases into one result.                                |
| union conversion from either case type      | Creates a success or a failure without calling a factory.           |

### `Result` helpers

| Helper                                | Purpose                                                                       |
| ------------------------------------- | ----------------------------------------------------------------------------- |
| `Map` / `Bind`                        | Transforms or chains a success; failures pass through.                        |
| `Select` / `SelectMany`               | LINQ query syntax forms of `Map` and `Bind`.                                  |
| `Switch`                              | Runs an action for the success or failure case.                               |
| `MapError`                            | Transforms a failure; successes pass through.                                 |
| `Tap` / `TapError`                    | Observes the matching case and returns the original result.                   |
| `Ensure`                              | Turns a success into a failure when a predicate does not hold.                |
| `GetOrElse` / `OrElse`                | Supplies a value or another result on failure.                                |
| `GetOrThrow`                          | Returns the success value or throws a built exception.                        |
| `ToMaybe`                             | Converts a success to a present optional value.                               |
| `ToEnumerable`                        | Yields zero or one success values.                                            |
| `Flatten`                             | Turns `Result<Result<TValue, TError>, TError>` into `Result<TValue, TError>`. |
| `MapAsync` / `BindAsync` / `TapAsync` | Asynchronous composition.                                                     |

The `Task<Result<TValue, TError>>` receiver also has `MapAsync`, `BindAsync`, and `TapAsync`
overloads.

## `Error`

`Error` is a small `readonly record struct` for the common case where a failure needs a message and
an optional exception.

```csharp
var error = Error.FromException(exception);
var simple = new Error("Order not found.");
Error fromString = "Order not found.";
```

`Error` has value equality through its record struct implementation. `Exception` is compared by
reference.

## `Unit`

`Unit` represents success when there is no value to return.

```csharp
Result<Unit, Error> Save()
{
    try
    {
        repository.Save();
        return Result<Unit, Error>.Ok(Unit.Value);
    }
    catch (Exception exception)
    {
        return Result<Unit, Error>.Fail(Error.FromException(exception));
    }
}
```

## Async composition

Async helpers await the callback and preserve the short-circuit behavior of the synchronous
helpers.

```csharp
var result = await LoadOrder(id)
    .MapAsync(order => pricing.PriceAsync(order))
    .BindAsync(order => inventory.ReserveAsync(order))
    .TapAsync(order => logger.LogInformationAsync("Reserved {OrderId}", order.Id));
```

## Defaults and edge cases

| Case                                                 | Behavior                                                                                                |
| ---------------------------------------------------- | ------------------------------------------------------------------------------------------------------- |
| `default(Maybe<T>)`                                  | `None`; the union contents are `null`.                                                                  |
| `default(Result<TValue, TError>)`                    | Uninitialized; property access and `Match` throw, `switch` reaches the `null` arm.                      |
| `Maybe<T>.Some(null!)` / `Ok(null!)` / `Fail(null!)` | Throws `ArgumentNullException`, including through union conversion.                                     |
| `Result<TValue, TError>.Try`                         | Catches `Exception`, including cancellation. Use normal `try`/`catch` when cancellation must propagate. |
| Equality                                             | Compares the case and the payload. `None` and `Unit` have value equality.                               |
| `Maybe<T>.ToString()` / `Result<...>.ToString()`     | Debug output, not a serialization contract.                                                             |

## Limits and interop

- **JSON.** A union serializes its `Value` without a case tag, so `System.Text.Json` cannot round-trip a
  union or a result. Convert to a DTO at the boundary instead of serializing these types.
- **Exhaustiveness.** A `switch` expression warns when a case is missing. Set
  `<WarningsAsErrors>CS8509</WarningsAsErrors>` or the repo-wide `TreatWarningsAsErrors` to make a
  missing case a build error.
- **Async surface.** Async helpers use `Task`. There are no `ValueTask` overloads; await a `ValueTask`
  or call `.AsTask()` first.
- **Same payload types.** `Result<string, string>` compiles, but a `switch` cannot tell the two cases
  apart and union conversion is ambiguous. Use distinct payload types when you pattern match.

## Native AOT and trimming

The library has no reflection, serialization, or dynamic code. The project sets
`IsAotCompatible`, so trim and AOT analyzers run during the build. CI publishes the sample with
`PublishAot` and runs it.

## Repository layout

| Path                                             | Purpose                                       |
| ------------------------------------------------ | --------------------------------------------- |
| `src/LayeredCraft.Toolkit.Functional`            | The package.                                  |
| `tests/LayeredCraft.Toolkit.Functional.Tests`    | xUnit v3 tests on Microsoft Testing Platform. |
| `samples/LayeredCraft.Toolkit.Functional.Sample` | Runnable usage example.                       |

The `LayeredCraft.Toolkit` solution is intended to host additional `LayeredCraft.Toolkit.*`
packages.

## Build and test

```bash
dotnet restore LayeredCraft.Toolkit.slnx
dotnet build LayeredCraft.Toolkit.slnx -c Release
dotnet test --solution LayeredCraft.Toolkit.slnx -c Release --no-build
dotnet run --project samples/LayeredCraft.Toolkit.Functional.Sample -c Release
```

## License

MIT. See [LICENSE](LICENSE).
