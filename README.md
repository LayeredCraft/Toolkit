# LayeredCraft.Toolkit.Functional

Non-boxing functional primitives for .NET 11. The package provides `Maybe<T>`,
`Result<TValue>`, `Result<TValue, TError>`, `Error`, `Unit`, and composition helpers for synchronous
and asynchronous code. It has no runtime dependencies.

`Maybe<T>`, `Result<TValue>`, and `Result<TValue, TError>` are C# 15 union types: `switch`
expressions unwrap their cases and the compiler warns about a missing case. The union storage is
hand-written so a union never boxes a payload and never stores an `object`.

This README is the documentation for the package. There is no separate doc site.

## Why this package exists

A plain `union Maybe<T>(T, None)` declaration stores its contents as `object?`, so every value type
is boxed. These types use the C# 15 custom union support instead:

- `[Union]` plus `IUnion` makes the type a real union for conversions, pattern matching, and
  exhaustiveness.
- A nested `IUnionMembers` provider declares the case types with `static Create` and the
  **non-boxing access pattern** (`HasValue` and `TryGetValue`).
- `Maybe<T>` stores `T` and a `bool`; `Result<TValue>` wraps the generic union with `Error` as the
  fixed error type; `Result<TValue, TError>` stores both payloads and a `byte` case tag.
- The provider keeps the strongly typed `Value` and `Error` API; the `object?` union view is
  explicit and is only used when a caller asks for it.
- A default `Result<TValue>` is a real failure carrying `Error.Uninitialized`, so a switch needs no
  `null` arm. A default `Maybe<T>` is `None`, and a default `Result<TValue, TError>` is the
  uninitialized union contents.

The types are deliberately boring: constructors are factories, helpers are extension methods, and
matching is a normal `switch`. Union conversion lets a case type convert implicitly, so
`Maybe<int> maybe = 42;`, `Result<int> result = 42;`, and `Result<int, Error> generic = 42;` work.

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
        ? Result<decimal>.Ok(value)
        : Result<decimal>.Fail(new Error("Price must be positive.", Code: "PRICE_POSITIVE")))
    .Map(value => Math.Round(value, 2))
    .Tap(value => Console.WriteLine($"price: {value}"));

Console.WriteLine(price switch
{
    decimal value => $"total: {value * quantity}",
    Error error => $"failed: {error.Code} {error.Message}",
});

static Maybe<int> ParseQuantity(string text)
    => int.TryParse(text, out var value) && value > 0 ? value : Maybe.None;

static Result<decimal> ParsePrice(string text)
    => decimal.TryParse(text, out var value)
        ? Result<decimal>.Ok(value)
        : Result<decimal>.Fail(new Error("Price must be a number.", Code: "PRICE_FORMAT"));
```

The runnable version lives in
[
`samples/LayeredCraft.Toolkit.Functional.Sample`](samples/LayeredCraft.Toolkit.Functional.Sample/Program.cs).

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

The `null` arm is the empty case, not a nullability check. A struct union's `default` always has
`null`
contents, so `default(Maybe<int>)` is `None` and only the `null` arm matches it. The compiler
requires
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
`MaybeSequenceExtensions`, `ResultSequenceExtensions`, and `ResultOfTSequenceExtensions`.

| Helper                   | Receiver                              | Purpose                                                                         |
| ------------------------ | ------------------------------------- | ------------------------------------------------------------------------------- |
| `Choose`                 | `IEnumerable<Maybe<T>>`               | Yields the present values and drops the absent ones.                            |
| `Sequence`               | `IEnumerable<Maybe<T>>`               | Returns all values, or `None` when any value is absent.                         |
| `Traverse`               | `IEnumerable<T>`                      | Maps each item and returns all values, or `None` when any projection is absent. |
| `FirstOrNone`            | `IEnumerable<T>`                      | Returns the first item (optionally matching a predicate), or `None`.            |
| `Successes` / `Failures` | `IEnumerable<Result<TValue>>`         | Yields the successes or the failures.                                           |
| `Partition`              | `IEnumerable<Result<TValue>>`         | Splits the results into success and failure lists.                              |
| `Sequence`               | `IEnumerable<Result<TValue>>`         | Returns all values, or the first failure.                                       |
| `Traverse`               | `IEnumerable<T>`                      | Maps each item and returns all values, or the first failure.                    |
| `Successes` / `Failures` | `IEnumerable<Result<TValue, TError>>` | Yields the successes or the failures.                                           |
| `Partition`              | `IEnumerable<Result<TValue, TError>>` | Splits the results into success and failure lists.                              |
| `Sequence`               | `IEnumerable<Result<TValue, TError>>` | Returns all values, or the first failure.                                       |
| `Traverse`               | `IEnumerable<T>`                      | Maps each item and returns all values, or the first failure.                    |

```csharp
var totals = orders
    .Traverse(order => order.Validate())
    .Map(valid => valid.Sum(order => order.Total));
```

`Result<TValue, TError>.Sequence` and `.Traverse` throw `InvalidOperationException` when a sequence
contains an uninitialized result, because that is a bug rather than a failure. The fixed
`Result<TValue>` helpers treat a default result as an `Error.Uninitialized` failure and short-circuit
on it.

## Choosing a result type

`Result<TValue>` is the default: a value or an `Error`. `Result<TValue, TError>` is the escape hatch
when the failure is a closed set of domain cases. Both are binary groupings; the difference is how
much structure the failure carries.

| Caller's first question                            | Type                                              |
| -------------------------------------------------- | ------------------------------------------------- |
| "Did it succeed?"                                  | `Result<TValue>`                                  |
| "Did it succeed, and if not, why?"                 | `Result<TValue>` with an `Error` code             |
| "Did it succeed, and which domain failure was it?" | `Result<TValue, TError>` with a domain error type |
| "Which of several peer states is this?"            | A domain union, not a `Result`                    |

A `Result` is a grouping: success and failure. It is binary at the top level even when the failure
is a union. Use a domain union when the outcomes are peers and the caller's first question is "which
state?", for example a workflow that moves through `Pending`, `Paid`, and `Shipped`.

### Two pipes

`Result<TValue>` and `Result<TValue, TError>` are two separate types, not one type with a default.
C# has no default type arguments, so `Result<TValue>` cannot be sugar for `Result<TValue, Error>`.
The duplicate type is the price of the ergonomic default.

- Each pipe has its own helper surface, so a call site never needs an explicit conversion.
- A value converts implicitly between the pipes only when the error type is `Error`
  (`Result<TValue>` ↔ `Result<TValue, Error>`). A domain error type maps to `Error` with `MapError`
  first; the fixed pipe's `ToGeneric` handles the `Error` case.
- Pick one pipe per layer. The fixed pipe is the default; the generic pipe is the escape hatch.

## `Result<TValue>`

`Result<TValue>` is the ergonomic default: a union with two case types, `TValue` and `Error`. The
default value is a failure carrying `Error.Uninitialized`, so a `switch` needs no `null` arm.

```csharp
public Result<Order> LoadOrder(Guid id)
{
    var order = repository.Find(id);

    return order is null
        ? Result<Order>.Fail(new Error("Order not found.", Code: "ORDER_NOT_FOUND"))
        : Result<Order>.Ok(order);
}

var message = LoadOrder(id) switch
{
    Order order => $"Loaded {order.Id}",
    Error error => $"Failed: {error.Code} {error.Message}",
};

var viaMatch = LoadOrder(id).Match(
    order => $"Loaded {order.Id}",
    error => $"Failed: {error.Message}");
```

A default result is a failure, not a distinct empty state:

```csharp
Result<Order> missing = default;

missing.IsSuccess;      // false
missing.IsFailure;      // true
missing.IsUninitialized; // true
missing.Error.Code;      // "RESULT_UNINITIALIZED"
```

That is why the switch above has no `null` arm: `default(Result<TValue>)` matches the `Error` case.
Check `IsUninitialized` when a forgotten factory call must be distinguishable from a real failure.

### Factories

The non-generic `Result` class infers the value type, so the common factories read without a type
argument:

```csharp
var ok = Result.Ok(42);                          // Result<int>
var failed = Result.Fail<Order>(error);          // Result<Order>; a failure has no value to infer
var parsed = Result.Try(() => int.Parse("42"));  // Result<int>
```

`Result<TValue>.Ok(...)` and `Result<TValue>.Fail(...)` remain available and are identical. Use
whichever reads better at the call site.

### `Result<TValue>` members

| Member                                                 | Purpose                                                     |
| ------------------------------------------------------ | ----------------------------------------------------------- |
| `Result.Ok(value)`                                     | Infers the value type. The same as `Result<TValue>.Ok`.     |
| `Result.Fail<TValue>(error)`                           | Creates a failure; the type argument is required.           |
| `Result.Try(operation)` / `Result.TryAsync(operation)` | Infers the value type from the operation's return type.     |
| `Result<TValue>.Ok(value)`                             | Creates a success. Throws when `value` is `null`.           |
| `Result<TValue>.Fail(error)`                           | Creates a failure. Throws when `error` is `null`.           |
| `Try(operation)` / `Try(operation, factory)`           | Runs an operation and captures the exception as an `Error`. |
| `TryAsync(operation)` / `TryAsync(operation, factory)` | The asynchronous equivalent.                                |
| `IsSuccess` / `IsFailure` / `IsUninitialized`          | Reports the case.                                           |
| `Value` / `Error`                                      | Returns the matching payload or throws.                     |
| `TryGetValue` / `TryGetError`                          | Reads a payload without throwing.                           |
| `Match` / `Switch`                                     | Projects or observes both cases.                            |
| `ToGeneric()` / `From(generic)`                        | Converts to and from `Result<TValue, Error>`.               |
| union conversion from either case type                 | Creates a success or a failure without calling a factory.   |

### `Result<TValue>` helpers

The helpers mirror the generic ones and return `Result<TNext>`: `Map`, `Bind`, `Select`,
`SelectMany`, `MapError`, `Ensure`, `Tap`, `GetOrElse`, `OrElse`, `GetOrThrow`, `ToMaybe`,
`ToEnumerable`, `Flatten`, `MapAsync`, `BindAsync`, and `TapAsync`. The `Task<Result<TValue>>`
receiver has `MapAsync`, `BindAsync`, and `TapAsync` overloads.

## `Result<TValue, TError>`

`Result<TValue, TError>` is a union with two case types: `Ok(value)` or `Fail(error)`. Both payload
types are `notnull`, and the case tag is separate from the payloads, so the typed `Value` and
`Error` properties always report the case that was created.

```csharp
public Result<Order, OrderFailure> LoadOrder(Guid id)
{
    var order = repository.Find(id);

    return order is null
        ? Result<Order, OrderFailure>.Fail(new NotFound(id))
        : Result<Order, OrderFailure>.Ok(order);
}

var message = LoadOrder(id) switch
{
    Order order => $"Loaded {order.Id}",
    NotFound notFound => $"Missing: {notFound.Id}",
    Forbidden forbidden => $"Forbidden: {forbidden.Reason}",
    null => "Uninitialized",   // default(Result<...>)
};

var viaMatch = LoadOrder(id).Match(
    order => $"Loaded {order.Id}",
    failure => $"Failed: {failure.Message}");
```

The error type is a closed set of domain cases:

```csharp
public closed record class OrderFailure(string Message);

public sealed record class NotFound(Guid Id) : OrderFailure($"Order {Id} was not found.");

public sealed record class Forbidden(string Reason) : OrderFailure($"Order access is forbidden: {Reason}.");
```

`default(Result<TValue, TError>)` is **uninitialized**, not a success or a failure. It is the
`null` contents of the union. Accessing `Value`, `Error`, or `Match` on it throws, and a `switch`
reaches it through the `null` arm. This catches a forgotten factory call instead of silently
treating a default value as a valid success. The fixed `Result<TValue>` avoids this by folding the
default into `Error.Uninitialized`; the generic type cannot, because an arbitrary `TError` has no
meaningful default to synthesize.

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

`Error` is a small `readonly record struct` for the common case where a failure needs a message, an
optional exception, and an optional machine-readable code.

```csharp
var error = Error.FromException(exception);          // Code = exception type name
var simple = new Error("Order not found.", Code: "ORDER_NOT_FOUND");
Error fromString = "Order not found.";
var uninitialized = Error.Uninitialized;             // Code = "RESULT_UNINITIALIZED"
```

`Error` has value equality through its record struct implementation. `Exception` is compared by
reference. `Error.IsUninitialized` identifies the default failure of a `Result<TValue>`.

## `Unit`

`Unit` represents success when there is no value to return.

```csharp
Result<Unit> Save()
{
    try
    {
        repository.Save();
        return Result<Unit>.Ok(Unit.Value);
    }
    catch (Exception exception)
    {
        return Result<Unit>.Fail(Error.FromException(exception));
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
| `default(Result<TValue>)`                            | Failure with `Error.Uninitialized`; `switch` matches the `Error` arm and needs no `null` arm.           |
| `default(Result<TValue, TError>)`                    | Uninitialized; property access and `Match` throw, `switch` reaches the `null` arm.                      |
| `default(Error)`                                     | A struct default with a `null` message. Use `Error.Uninitialized` for a real failure.                   |
| `Maybe<T>.Some(null!)` / `Ok(null!)` / `Fail(null!)` | Throws `ArgumentNullException`, including through union conversion.                                     |
| `Result<TValue>.Try` / `Result<TValue, TError>.Try`  | Catches `Exception`, including cancellation. Use normal `try`/`catch` when cancellation must propagate. |
| Equality                                             | Compares the case and the payload. `None` and `Unit` have value equality.                               |
| `Maybe<T>.ToString()` / `Result<...>.ToString()`     | Debug output, not a serialization contract.                                                             |

## EF Core

`Result` and `Maybe` are query and handler types, not entity property types. EF Core cannot translate
union construction to SQL, and a struct union has no convention mapping. Use them at the repository
boundary.

```csharp
// Query: materialize first, then wrap. The projection stays in memory.
var orders = await db.Orders
    .Where(order => order.CustomerId == customerId)
    .ToListAsync(cancellationToken);

var result = orders.Count == 0
    ? Result<IReadOnlyList<Order>>.Fail(new Error("No orders.", Code: "ORDERS_EMPTY"))
    : Result<IReadOnlyList<Order>>.Ok(orders);

// Lookup: a single row is an optional value.
var maybeOrder = await db.Orders.FindAsync([id], cancellationToken) is { } order
    ? Maybe<Order>.Some(order)
    : Maybe<Order>.None;

// Write: wrap the save and keep the concurrency failure as data.
var saved = await Result<Unit>.TryAsync(async () =>
{
    await db.SaveChangesAsync(cancellationToken);
    return Unit.Value;
});
```

- **Do not map a `Result` or `Maybe` to a column.** They are not entities or value objects. Store the
  domain state (`OrderStatus`, `CustomerId`) and build the result in the handler.
- **Do not project to a union in an `IQueryable`.** EF Core cannot translate it. Call `ToListAsync`,
  `FirstOrDefaultAsync`, or `FindAsync` first, then wrap the materialized value.
- **`FindAsync` and `SingleOrDefaultAsync` return `null` on a miss.** Convert the miss to `Maybe`
  or to a `Result` failure at the boundary.
- **`SaveChangesAsync` returns an affected-row count.** Return `Result<int>` when the count matters,
  or `Result<Unit>` when it does not. `DbUpdateConcurrencyException` and `DbUpdateException` arrive
  through `Result.Try`, and `Error.FromException` records the exception type as the code.
- **`AsNoTracking` queries still cannot construct unions.** The restriction is translation, not
  tracking.

## Limits and interop

- **JSON.** A union serializes its `Value` without a case tag, so `System.Text.Json` cannot
  round-trip a
  union or a result. Convert to a DTO at the boundary instead of serializing these types.
- **Exhaustiveness.** A `switch` expression warns when a case is missing. Set
  `<WarningsAsErrors>CS8509</WarningsAsErrors>` or the repo-wide `TreatWarningsAsErrors` to make a
  missing case a build error.
- **Async surface.** Async helpers use `Task`. There are no `ValueTask` overloads; await a
  `ValueTask`
  or call `.AsTask()` first.
- **Same payload types.** `Result<string, string>` compiles, but a `switch` cannot tell the two
  cases
  apart and union conversion is ambiguous. Use distinct payload types when you pattern match.

## Native AOT and trimming

The library has no reflection, serialization, or dynamic code. The project sets
`IsAotCompatible`, so trim and AOT analyzers run during the build. CI publishes the sample with
`PublishAot` and runs it.

## Repository layout

| Path                                             | Purpose                                       |
| ------------------------------------------------ | --------------------------------------------- |
| `src/LayeredCraft.Toolkit.Functional`            | The package.                                  |
| `src/LayeredCraft.Toolkit.Functional/Maybe`      | `Maybe<T>`, `None`, and its helpers.          |
| `src/LayeredCraft.Toolkit.Functional/Result`     | The result types, factories, and helpers.     |
| `src/LayeredCraft.Toolkit.Functional/Sequences`  | Collection helpers for both types.            |
| `tests/LayeredCraft.Toolkit.Functional.Tests`    | xUnit v3 tests on Microsoft Testing Platform. |
| `samples/LayeredCraft.Toolkit.Functional.Sample` | Runnable usage example.                       |

The package keeps one namespace, `LayeredCraft.Toolkit.Functional`, so the folders are a reading
aid only. Moving a type between folders does not change its public name.

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
