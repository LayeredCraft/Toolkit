---
name: layeredcraft-functional-types
description: >-
  Use LayeredCraft.Toolkit.Functional to model optional values and failures with
  Maybe and Result in .NET 11 / C# 15. Covers choosing between Maybe, Result<T>,
  Result<T, TError>, and a domain union; the struct-union default state and its
  null arm; non-boxing union storage; the two Result pipes; type-inference
  factories; and EF Core boundaries. Use this whenever adding Maybe or Result to
  a .NET 11 app, returning results from repositories or handlers, deciding how to
  model "not found" versus "unauthorized" versus "validation failed", mapping
  results to HTTP responses, or debugging a union switch that throws on a default
  value. Also use for C# 15 union questions about boxing, exhaustiveness, or
  default structs.
---

# LayeredCraft Functional Types

`LayeredCraft.Toolkit.Functional` supplies `Maybe<T>`, `Result<TValue>`,
`Result<TValue, TError>`, `Error`, `Unit`, and composition helpers. Everything is a
`readonly struct` union with hand-written storage, so nothing boxes on `switch`,
`HasValue`, or `TryGetValue`.

The package targets .NET 11 and C# 15. The types are `[Union]` + `IUnion` with a
nested `IUnionMembers` provider that declares the non-boxing access pattern.

## Choose the type first

| Question the caller asks                 | Type                     |
| ---------------------------------------- | ------------------------ |
| "Is there a value?"                      | `Maybe<T>`               |
| "Did it work?"                           | `Result<TValue>`         |
| "Did it work, and which domain failure?" | `Result<TValue, TError>` |
| "Which of these states am I in?"         | A domain `union`         |

`Maybe<T>` is absence without a reason. `Result<TValue>` is a success or an
`Error`. `Result<TValue, TError>` is a success or a closed set of domain failures.
A domain union is for peer states where the first question is "which state?", not
"did it succeed?" — for example an order workflow of `Pending | Paid | Shipped |
Cancelled`.

A `Result` is a binary grouping. Its `TError` can itself be a union, so
`Result<User, UserLookupFailure>` models "one success plus three failure reasons"
without flattening. Prefer the flat domain union when the caller's first decision
is a multi-way switch on the outcome; prefer `Result` when the codebase composes
with `Map`/`Bind`/`Sequence` and the first decision is "did we get the user?".

## Defaults and the null arm

A struct union's `default` has null contents. This is the most common surprise.

| Type                     | `default(...)`                                   | Switch arm needed            |
| ------------------------ | ------------------------------------------------ | ---------------------------- |
| `Maybe<T>`               | `None` (meaningful)                              | `null` arm is the `None` arm |
| `Result<TValue>`         | `Fail(Error.Uninitialized)` (meaningful)         | no `null` arm                |
| `Result<TValue, TError>` | uninitialized, throws on `Value`/`Error`/`Match` | `null` arm catches it        |

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

Check `Result<TValue>.IsUninitialized` when "forgot to construct" must be
distinguishable from "constructed as a failure". `Error.Uninitialized` has code
`RESULT_UNINITIALIZED`.

Do not make a default match a domain case on a generic result. It silently turns a
bug into a domain outcome. The fixed pipe's default is meaningful because the
error type is fixed; the generic pipe cannot synthesize a `TError`.

## C# 15 union mechanics

- A generated `union` declaration stores contents as `object?` and boxes struct
  payloads. For hot paths, hand-write `[Union]` + `IUnion` + a nested
  `IUnionMembers` provider with `TryGetValue`, as this package does.
- Union conversion is implicit from each case type: `Maybe<int> m = 42;`,
  `Result<int> r = new Error("boom");`.
- Null is rejected in `Create` members and factories, including through union
  conversion. `Result<string> r = null!;` throws `ArgumentNullException`.
- `Result<string, string>` compiles but union conversion is ambiguous. Use
  factories when the payload types collide.
- Set `<WarningsAsErrors>CS8509</WarningsAsErrors>` so a missed union case fails
  the build instead of silently falling through.

## The two Result pipes

`Result<TValue>` and `Result<TValue, TError>` are separate types. C# has no default
type argument, so `Result<TValue>` cannot be written as `Result<TValue, Error>`.

- Each pipe has its own helper surface so call sites keep type inference.
- Implicit conversion works between `Result<TValue>` and `Result<TValue, Error>`
  only. A domain error type needs `MapError` or `ToGeneric`.
- Pick one pipe per layer. Use the fixed pipe for expected failures and the generic
  pipe where a closed domain failure set earns its own type.

Do not try to unify the pipes with an interface. An interface receiver
(`this IResult<T>`) boxes each call — measured at 24 bytes per call — and a
generic constrained receiver (`this TResult where TResult : IResult<T>`) loses
type inference on `TValue`, so helpers cannot be called fluently. Either way the
interface costs more than the duplicate type.

## Factories

```csharp
Result.Ok(42);                        // Result<int>, type inferred
Result.Fail<Order>(error);            // TValue cannot be inferred; name it
Result.Fail<Order>("not found");      // implicit string -> Error
Result.Try(() => Parse(text));        // Result<T>, type inferred
Result<T>.Ok(42);                     // same as Result.Ok
```

`Ok`, `Try`, and `TryAsync` infer the success type. `Fail` cannot: a failure has
no success payload to infer from. `Try` catches all `Exception`, including
cancellation — document that when cancellation matters.

## EF Core boundaries

Unions are query and handler types, not entity types. EF Core cannot translate
them and cannot map them as owned types without fighting the model.

- Query: materialize first, then wrap. `ToListAsync()` then `Select(Maybe.Some)`.
- Lookup: `FindAsync` → `Maybe<T>`; a miss is `None`.
- Write: `SaveChangesAsync` → `Result.Try` or `Result<Unit>`.
- Concurrency: catch `DbUpdateConcurrencyException` and return
  `Error.FromException(exception)`.
- Never put `Maybe`/`Result` in a `DbSet` or an entity property. Use them at the
  repository boundary and map to DTOs there.

```csharp
public async Task<Maybe<Order>> FindAsync(Guid id, CancellationToken ct)
    => await db.Orders.FindAsync([id], ct) is { } order
        ? Maybe<Order>.Some(order)
        : Maybe.None;

public async Task<Result<Unit>> SaveAsync(Order order, CancellationToken ct)
    => await Result.TryAsync(async () =>
    {
        db.Orders.Update(order);
        await db.SaveChangesAsync(ct);
        return Unit.Value;
    });
```

## Anti-patterns

- Using `Result<T, Exception>` as the default error type. It allocates per failure,
  is an open hierarchy so switches cannot be exhaustive, and compares by
  reference. Use `Result<T>` with `Error.FromException` when you need to carry an
  exception.
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
```

When a union switch throws `SwitchExpressionException`, look for a `default`
value reaching it — usually a struct field, array element, or DTO property that
was never assigned. Add the `null` arm or initialize the value.
