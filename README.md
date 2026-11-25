# LayeredCraft.Toolkit.Functional

Non-boxing functional primitives for modern .NET. The package provides `Maybe<T>`,
`Result<TValue, TError>`, `Error`, `Unit`, and composition helpers for synchronous and
asynchronous code. It has no runtime dependencies.

This README is the documentation for the package. There is no separate doc site.

## Why this package exists

Most .NET optional and result libraries model their cases with a shared `object` or an interface,
which boxes every value type and hides the cases from the type system. These primitives use a small
amount of state inside a `readonly struct` instead:

- `Maybe<T>` stores `T` and a `bool`.
- `Result<TValue, TError>` stores both payloads and a `byte` case tag.
- Matching, mapping, binding, and equality work through generic code paths, not through `object`.
- There is no `IUnion`, `object Value`, or implicit interface conversion.

The types are deliberately boring: constructors are factories, helpers are extension methods, and
matching is a normal method call. They work on any C# version that can consume a `net10.0` assembly.

## Requirements

- .NET 10 or later.

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

`Maybe<T>` is either `Some(value)` or `None`. `T` is constrained to `notnull`, so a missing
reference is never confused with a present `null`.

```csharp
Maybe<int> present = Maybe<int>.Some(42);
Maybe<int> absent = Maybe.None;

if (present.TryGetValue(out var value))
    Console.WriteLine(value);

var label = present.Match(v => $"value: {v}", () => "missing");
```

`default(Maybe<T>)` is `None`. That makes a struct field or a `default` initialization safe.

### `Maybe<T>` members

| Member                       | Purpose                                                          |
| ---------------------------- | ---------------------------------------------------------------- |
| `Maybe<T>.Some(value)`       | Creates a present value. Throws when `value` is `null`.          |
| `Maybe<T>.None`              | Creates the empty case.                                          |
| `Maybe.None`                 | Typed empty case that converts to any `Maybe<T>`.                |
| `HasValue` / `IsNone`        | Reports the case without matching.                               |
| `Value`                      | Returns the present value or throws `InvalidOperationException`. |
| `TryGetValue(out T)`         | Reads the value without throwing.                                |
| `Match(some, none)`          | Projects both cases into one result.                             |
| implicit `T` → `Maybe<T>`    | Wraps a value without calling `Some`.                            |
| implicit `None` → `Maybe<T>` | Lets `return Maybe.None;` work in a `Maybe<T>` method.           |

### `Maybe<T>` helpers

| Helper                                | Purpose                                                              |
| ------------------------------------- | -------------------------------------------------------------------- |
| `ToMaybe()`                           | Wraps a nullable value type or reference.                            |
| `Map` / `Bind`                        | Transforms or chains a present value.                                |
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

## `Result<TValue, TError>`

`Result<TValue, TError>` is either `Ok(value)` or `Fail(error)`. The two payload types stay
distinct even when they are the same type, because the case tag is separate from the payload.

```csharp
public Result<Order, Error> LoadOrder(Guid id)
{
    var order = repository.Find(id);

    return order is null
        ? Result<Order, Error>.Fail("Order not found.")
        : Result<Order, Error>.Ok(order);
}

var message = LoadOrder(id).Match(
    order => $"Loaded {order.Id}",
    error => $"Failed: {error.Message}");
```

`default(Result<TValue, TError>)` is **uninitialized**, not a success or a failure. Accessing
`Value`, `Error`, or `Match` on it throws. This catches a forgotten factory call instead of silently
treating a default value as a valid success.

### `Result<TValue, TError>` members

| Member                                      | Purpose                                                             |
| ------------------------------------------- | ------------------------------------------------------------------- |
| `Result<TValue, TError>.Ok(value)`          | Creates a success.                                                  |
| `Result<TValue, TError>.Fail(error)`        | Creates a failure.                                                  |
| `Try(operation, errorFactory)`              | Runs a function and captures an exception.                          |
| `TryAsync(operation, errorFactory)`         | Awaits a function and captures an exception.                        |
| `IsSuccess` / `IsFailure` / `IsInitialized` | Reports the case.                                                   |
| `Value` / `Error`                           | Returns the matching payload or throws `InvalidOperationException`. |
| `TryGetValue(out TValue)`                   | Reads a success without throwing.                                   |
| `TryGetError(out TError)`                   | Reads a failure without throwing.                                   |
| `Match(success, failure)`                   | Projects both cases into one result.                                |

### `Result` helpers

| Helper                                | Purpose                                                                       |
| ------------------------------------- | ----------------------------------------------------------------------------- |
| `Map` / `Bind`                        | Transforms or chains a success; failures pass through.                        |
| `MapError`                            | Transforms a failure; successes pass through.                                 |
| `Tap` / `TapError`                    | Observes the matching case and returns the original result.                   |
| `Ensure`                              | Turns a success into a failure when a predicate does not hold.                |
| `GetOrElse` / `OrElse`                | Supplies a value or another result on failure.                                |
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

| Case                                             | Behavior                                                                                                |
| ------------------------------------------------ | ------------------------------------------------------------------------------------------------------- |
| `default(Maybe<T>)`                              | `None`.                                                                                                 |
| `default(Result<TValue, TError>)`                | Uninitialized; property access and `Match` throw.                                                       |
| `Maybe<T>.Some(null!)`                           | Throws `ArgumentNullException`.                                                                         |
| `Result<TValue, TError>.Try`                     | Catches `Exception`, including cancellation. Use normal `try`/`catch` when cancellation must propagate. |
| Equality                                         | Compares the case and the payload. `None` and `Unit` have value equality.                               |
| `Maybe<T>.ToString()` / `Result<...>.ToString()` | Debug output, not a serialization contract.                                                             |

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
