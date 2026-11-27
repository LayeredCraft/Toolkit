---
name: layeredcraft-toolkit-functional
description: >-
  Use LayeredCraft.Toolkit.Functional to model optional values and failures with
  Maybe and Result in .NET 11 / C# 15. Covers choosing between Maybe, Result<T>,
  Result<T, TError>, and a domain union; the struct-union default state and its
  null arm; non-boxing union storage; the two Result pipes; implicit union
  conversion on return; factories, helpers, LINQ and async composition; sequence
  helpers; Error and Unit; EF Core boundaries; and JSON/AOT limits. Use this
  whenever adding Maybe or Result to a .NET 11 app, returning results from
  repositories or handlers, deciding how to model "not found" versus
  "unauthorized" versus "validation failed", mapping results to HTTP responses,
  combining many optional or result values, or debugging a union switch that throws
  on a default value. Also use when choosing between an Error code and a custom
  error type, or between implicit return conversion and a factory. Also use for
  C# 15 union questions about boxing, exhaustiveness, or default structs.
---

# LayeredCraft Toolkit Functional

`LayeredCraft.Toolkit.Functional` supplies `Maybe<T>`, `Result<TValue>`,
`Result<TValue, TError>`, `Error`, `Unit`, and composition helpers. `Maybe` and the
two results are `readonly struct` unions with hand-written storage, so nothing boxes
on `switch`, `HasValue`, or `TryGetValue`. The package targets .NET 11 / C# 15, has
no runtime dependencies, and keeps one namespace: `LayeredCraft.Toolkit.Functional`.

The repository `README.md` is the full API reference. Read the matching section
there for a complete member table: `README.md` §`Maybe<T>`, §Collections,
§`Result<TValue>`, §`Result<TValue, TError>`, §`Error`, §`Unit`, §Async composition,
§Limits and interop.

## Choose the type first

| Question the caller asks         | Type             |
| -------------------------------- | ---------------- |
| "Is there a value?"              | `Maybe<T>`       |
| "Did it work?"                   | `Result<TValue>` |
| "Which of these states am I in?" | A domain `union` |

Strongly prefer `Result<TValue>` with `Error` as the default failure type. `Error`
carries a message, an optional exception, and a stable machine-readable code, which
covers almost every failure a repository or handler returns. Reach for
`Result<TValue, TError>` only when the failure payload is genuinely structured data
that callers must switch on — for example a validation failure carrying a field map,
or a `NotFound | Forbidden | Conflict` set that each holds different data. A single
failure reason with a distinguishable code is an `Error` code (`ORDER_NOT_FOUND`),
not a custom error type.

`Maybe<T>` is absence without a reason. A domain union is for peer states where the
first question is "which state?", not "did it succeed?" — for example
`Pending | Paid | Shipped | Cancelled`. A `Result` is binary at the top level even
when its failure type is itself a union, so `Result<User, UserLookupFailure>` models
"one success plus three failure reasons" without flattening. Prefer the domain union
when the caller's first decision is a multi-way switch on the outcome; prefer
`Result` when the code composes with `Map`/`Bind`/`Sequence` and the first decision
is "did we get the user?".

## Return the case, not the factory

The modern .NET 11 / C# 15 pattern is union conversion: declare the return type and
return the case value. The compiler converts it implicitly, so a return statement
does not call `Ok`, `Fail`, or `Some`.

```csharp
// Prefer this.
public Maybe<Order> FindOrder(Guid id) => repository.Find(id);

public Result<decimal> ParsePrice(string text)
    => decimal.TryParse(text, out var value)
        ? value
        : new Error("Price must be a number.", Code: "PRICE_FORMAT");

public Result<Unit> Save()
    => Unit.Value;
```

```csharp
// Avoid this: the factory adds noise without adding a check.
public Maybe<Order> FindOrder(Guid id) => Maybe<Order>.Some(repository.Find(id));

public Result<decimal> ParsePrice(decimal value)
    => Result<decimal>.Ok(value);
```

Union conversion still rejects `null` (`ArgumentNullException`) and respects
`notnull`, so returning the case value is as safe as calling the factory. An
`Error` converts to `Result<TValue>`; a plain `string` does not chain through
`Error` into a result, so return `new Error(...)` rather than a raw message.

Factories still earn their place when the target type is not known yet, such as a
local, a collection element, or a lambda whose return type is inferred:

```csharp
var ok = Result.Ok(42);                                  // Result<int>, type inferred
var failures = new List<Result<Order>>
{
    new Error("first"),
    Result.Fail<Order>("not a number"),                  // one conversion: string -> Error
};
var parsed = Result.Try(() => int.Parse(text));
Result.Fail<Order>(error);                               // TValue cannot be inferred; name it
```

Do not mix styles within one method or class: pick the implicit return form and use
it consistently.

## Defaults and the null arm

A struct union's `default` has `null` contents. This is the most common surprise.

| Type                     | `default(...)`                                   | Switch arm needed            |
| ------------------------ | ------------------------------------------------ | ---------------------------- |
| `Maybe<T>`               | `None` (meaningful)                              | `null` arm is the `None` arm |
| `Result<TValue>`         | `Fail(Error.Uninitialized)` (meaningful)         | no `null` arm                |
| `Result<TValue, TError>` | uninitialized, throws on `Value`/`Error`/`Match` | `null` arm catches it        |

`default(Result<TValue, TError>)` is neither success nor failure; it is the empty
union. `default(Result<TValue>)` folds the default into a real failure carrying
`Error.Uninitialized` (code `RESULT_UNINITIALIZED`) because the error type is fixed;
a generic `TError` has no meaningful default to synthesize. Check
`Result<TValue>.IsUninitialized` when "forgot to construct" must be distinguishable
from a constructed failure.

The compiler only warns about a missing `null` arm when a case type is nullable.
With non-nullable case types the switch compiles and throws
`SwitchExpressionException` on a default value. So:

```csharp
// Result<TValue>: default is a real failure, no null arm needed.
var text = result switch
{
    Order order => $"order: {order.Sku}",
    Error error => $"failed: {error.Message}",
};

// Result<TValue, TError>: default is uninitialized, keep the null arm.
var text = result switch
{
    Order order => $"order: {order.Sku}",
    NotFound notFound => $"missing: {notFound.Id}",
    null => "uninitialized",
};
```

Do not make a default match a domain case on a generic result. It silently turns a
bug into a domain outcome.

## C# 15 union mechanics

- A generated `union` declaration stores contents as `object?` and boxes struct
  payloads. For hot paths, hand-write `[Union]` + `IUnion` + a nested
  `IUnionMembers` provider with `TryGetValue`, as this package does.
- `Maybe<T>` stores `T` and a `bool`; `Result<TValue>` wraps
  `Result<TValue, Error>`; `Result<TValue, TError>` stores both payloads and a case
  tag. The strongly typed `Value` / `Error` properties stay on the struct; the
  `object?` union view is only used when a caller asks for it.
- Union conversion is implicit from each case type: `Maybe<int> m = 42;`,
  `Result<int> r = new Error("boom");`, `Maybe<int> none = Maybe.None;`.
- Null is rejected in `Create` members and factories, including through union
  conversion. `Result<string> r = null!;` throws `ArgumentNullException`.
- `Result<string, string>` compiles, but a `switch` cannot tell the two `string`
  cases apart and union conversion is ambiguous. Use distinct payload types when
  you pattern match; use factories when the types collide.
- Set `<WarningsAsErrors>CS8509</WarningsAsErrors>` so a missed union case fails
  the build instead of silently falling through.

## The two Result pipes

`Result<TValue>` and `Result<TValue, TError>` are separate types. C# has no default
type argument, so `Result<TValue>` cannot be written as `Result<TValue, Error>`.

- Each pipe has its own helper surface so call sites keep type inference.
- Implicit conversion works between `Result<TValue>` and `Result<TValue, Error>`
  only: `ToGeneric()` / `From(generic)` or the implicit operators. A domain error
  needs `MapError` first.
- Pick one pipe per layer. The fixed pipe with `Error` is the default; the generic
  pipe is the escape hatch for a failure payload that is structured data, not just a
  reason with a code.

Do not try to unify the pipes with an interface. An interface receiver
(`this IResult<T>`) boxes each call — measured at 24 bytes per call — and a
generic constrained receiver (`this TResult where TResult : IResult<T>`) loses
type inference on `TValue`, so helpers cannot be called fluently. Either way the
interface costs more than the duplicate type.

## Maybe

Members: `Maybe<T>.Some`, `Maybe<T>.None`, `Maybe.None` (converts to any
`Maybe<T>`), `HasValue` / `IsNone`, `Value`, `TryGetValue`, `Match`, and union
conversion from `T`.

Helpers: `ToMaybe`, `Map`, `Bind`, `Select`, `SelectMany`, `Where`, `Switch`,
`Filter`, `GetOrElse`, `OrElse`, `Fold`, `Tap`, `TapNone`, `Zip`, `Flatten`,
`Contains`, `Exists`, `ForAll`, `ToNullable`, `ToNullableReference`,
`ToEnumerable`, `ToResult`, `MapAsync`, `BindAsync`, `TapAsync`.

```csharp
var total =
    from price in Maybe<decimal>.Some(19.99m)
    from quantity in ParseQuantity("3")
    select price * quantity;
```

`ToResult` turns `None` into a failure with a fixed or lazily created `Error`.
`Task<Maybe<T>>` also has `MapAsync` and `BindAsync` overloads.

## Result<TValue>

Return the case directly inside a `Result<TValue>` method; the union converts it:

```csharp
Result<int> parsed = 42;                       // success
Result<int> missing = new Error("not found");  // failure
```

Factories infer the success type where possible:

```csharp
Result.Ok(42);                        // Result<int>
Result.Fail<Order>(error);            // TValue cannot be inferred; name it
Result.Fail<Order>("not found");      // implicit string -> Error
Result.Try(() => Parse(text));        // Result<T>, type inferred
Result<T>.Ok(42);                     // same as Result.Ok
Result<T>.TryAsync(operation);        // Task<Result<T>>
```

`Ok`, `Try`, and `TryAsync` infer the success type. `Fail` cannot: a failure has
no success payload to infer from. Use the factories for locals and inferred
contexts, not for return statements.

Members: `IsSuccess` / `IsFailure` / `IsUninitialized`, `Value` / `Error`,
`TryGetValue` / `TryGetError`, `Match` / `Switch`, `ToGeneric` / `From`, and union
conversion from either case type.

Helpers return `Result<TNext>` and mirror the generic pipe: `Map`, `Bind`,
`Select`, `SelectMany`, `MapError`, `Ensure`, `Tap`, `TapError`, `GetOrElse`,
`OrElse`, `GetOrThrow`, `ToMaybe`, `ToEnumerable`, `Flatten`, `MapAsync`,
`BindAsync`, `TapAsync`. `Task<Result<TValue>>` has the async overloads.

`Try` catches all `Exception`, including cancellation — document that when
cancellation matters. `GetOrThrow` turns a failure into a thrown exception.

## Result<TValue, TError>

This is the escape hatch, not the default. Use it only when failures carry structured
data that callers switch on (a validation map, or `NotFound | Forbidden | Conflict`
cases with different payloads). A failure that is just a reason belongs in
`Result<TValue>` with an `Error` code.

A union with two case types: `Ok(value)` or `Fail(error)`. Both payload types are
`notnull`, and the case tag is separate from the payloads, so `Value` and `Error`
always report the case that was created. Return the cases directly, as with the
fixed pipe:

```csharp
public Result<Order, OrderFailure> LoadOrder(Guid id)
{
    var order = repository.Find(id);

    return order is null
        ? new NotFound(id)
        : order;
}
```

The error type is a closed set of domain cases, commonly `record class` with a
closed base and sealed derived cases. Members: `IsSuccess` / `IsFailure` /
`IsInitialized`, `Value` / `Error`, `TryGetValue` / `TryGetError`, `Match`, and
union conversion from either case type.

Helpers: `Map`, `Bind`, `Select`, `SelectMany`, `Switch`, `MapError`, `Tap`,
`TapError`, `Ensure`, `GetOrElse`, `OrElse`, `GetOrThrow`, `ToMaybe`,
`ToEnumerable`, `Flatten`, `MapAsync`, `BindAsync`, `TapAsync`.

## Collections

Sequence helpers short-circuit on the first absent or failed value. They live in
`MaybeSequenceExtensions`, `ResultSequenceExtensions`, and
`ResultOfTSequenceExtensions`.

| Helper                   | Receiver                              | Purpose                                                                   |
| ------------------------ | ------------------------------------- | ------------------------------------------------------------------------- |
| `Choose`                 | `IEnumerable<Maybe<T>>`               | Yields present values, drops absent ones.                                 |
| `Sequence`               | `IEnumerable<Maybe<T>>`               | All values, or `None` when any is absent.                                 |
| `Traverse`               | `IEnumerable<T>`                      | Maps each item, all values or `None`.                                     |
| `FirstOrNone`            | `IEnumerable<T>`                      | First item (optionally matching a predicate), or `None`.                  |
| `Successes` / `Failures` | `IEnumerable<Result<TValue>>`         | Yields successes or failures.                                             |
| `Partition`              | `IEnumerable<Result<TValue>>`         | Splits into success and failure lists.                                    |
| `Sequence`               | `IEnumerable<Result<TValue>>`         | All values, or the first failure.                                         |
| `Traverse`               | `IEnumerable<T>`                      | Maps each item, all values or the first failure.                          |
| `Successes` / `Failures` | `IEnumerable<Result<TValue, TError>>` | Yields successes or failures.                                             |
| `Partition`              | `IEnumerable<Result<TValue, TError>>` | Splits into success and failure lists.                                    |
| `Sequence`               | `IEnumerable<Result<TValue, TError>>` | All values, or the first failure; throws on an uninitialized element.     |
| `Traverse`               | `IEnumerable<T>`                      | Maps each item, all values or the first failure; throws on uninitialized. |

`Result<TValue, TError>.Sequence` and `.Traverse` throw `InvalidOperationException`
when the sequence contains an uninitialized result, because that is a bug rather
than a failure. The fixed `Result<TValue>` helpers treat a default result as an
`Error.Uninitialized` failure and short-circuit on it.

```csharp
var totals = orders
    .Traverse(order => order.Validate())
    .Map(valid => valid.Sum(order => order.Total));
```

## Error and Unit

`Error` is a `readonly record struct` with `Message`, optional `Exception`, and
optional `Code`. `Error.FromException` uses the exception message and its type name
as the code. `"message"` converts implicitly to `Error`. `Error.Uninitialized`
identifies the default failure of `Result<TValue>`. Equality is structural except
for `Exception`, which compares by reference.

`Unit` represents success with no value: return `Unit.Value` from a
`Result<Unit>` method; `Unit.Value` is `default`.

## EF Core boundaries

Unions are query and handler types, not entity types. EF Core cannot translate
them and cannot map them as owned types without fighting the model.

- Query: materialize first, then wrap. `ToListAsync()` then `Select(Maybe.Some)`.
- Lookup: `FindAsync` → `Maybe<T>`; a miss is `None`.
- Write: `SaveChangesAsync` → `Result<Unit>.TryAsync` or `Result<int>` when the
  affected-row count matters.
- Concurrency: catch `DbUpdateConcurrencyException` and return
  `Error.FromException(exception)`.
- Never put `Maybe`/`Result` in a `DbSet` or an entity property, and never project
  to one inside `IQueryable`. Use them at the repository boundary and map to DTOs
  there. `AsNoTracking` does not lift the translation restriction.

```csharp
public async Task<Maybe<Order>> FindAsync(Guid id, CancellationToken ct)
    => await db.Orders.FindAsync([id], ct) is { } order
        ? order
        : Maybe.None;

public async Task<Result<Unit>> SaveAsync(Order order, CancellationToken ct)
    => await Result<Unit>.TryAsync(async () =>
    {
        db.Orders.Update(order);
        await db.SaveChangesAsync(ct);
        return Unit.Value;
    });
```

## Interop limits

- JSON: a union serializes its `Value` without a case tag, so `System.Text.Json`
  cannot round-trip a union or result. Convert to a DTO at the boundary instead of
  serializing these types.
- Async: the surface is `Task`; there are no `ValueTask` overloads. Await a
  `ValueTask` or call `.AsTask()` first.
- Exhaustiveness: set `<WarningsAsErrors>CS8509</WarningsAsErrors>` or
  `TreatWarningsAsErrors`.
- `ToString()` is debug output, not a serialization contract.

## Anti-patterns

- Prefer implicit union conversion on `return` over `Ok`/`Fail`/`Some` factories.
  Return `value`, `new Error(...)`, or a domain case directly.
- Prefer `Result<TValue>` with an `Error` code over a custom error type. Introduce
  `Result<TValue, TError>` only when the failure carries structured data callers
  switch on.
- Using `Result<T, Exception>` as the default error type. It allocates per failure,
  is an open hierarchy so switches cannot be exhaustive, and compares by
  reference. Use `Result<T>` with `Error.FromException` when you need to carry an
  exception.
- Creating a custom `TError` hierarchy for one failure reason that an `Error` code
  already distinguishes.
- Switching without a `null` arm on `Result<TValue, TError>` and assuming the
  compiler will catch it. It will not unless a case type is nullable.
- Making `Error` a class or interface so domain errors can subclass it. That
  allocates, opens the hierarchy, and loses exhaustiveness.
- Treating "not found" as an exception. It is an expected outcome; return it.
- Returning `Result` from an EF entity or mapping it as a column.

## Verify

```bash
dotnet build LayeredCraft.Toolkit.slnx -c Release
dotnet test --solution LayeredCraft.Toolkit.slnx -c Release --no-build
dotnet run --project samples/LayeredCraft.Toolkit.Functional.Sample -c Release
```

When a union switch throws `SwitchExpressionException`, look for a `default` value
reaching it — usually a struct field, array element, or DTO property that was never
assigned. Add the `null` arm or initialize the value.
