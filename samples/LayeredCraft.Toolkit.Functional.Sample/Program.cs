using LayeredCraft.Toolkit.Functional;

// ---------------------------------------------------------------------------
// The package has three core shapes:
//   Maybe<T>                -> T or None
//   Result<TValue>          -> TValue or Error
//   Result<TValue, TError>  -> TValue or a closed domain failure
//
// Switch expressions are the primary consumer. Helpers compose the same data
// flow without leaving the shape.
// ---------------------------------------------------------------------------

// ---------------------------------------------------------------------------
// Maybe<T>: optional data. None is a real union case, so switches use None.
// ---------------------------------------------------------------------------
var customers = new Dictionary<string, string> { ["ada"] = "Ada Lovelace" };

Maybe<string> FindCustomer(string id)
    => customers.TryGetValue(id, out var name) ? name : Maybe.None;

Console.WriteLine(FindCustomer("ada") switch
{
    string name => $"customer: {name}",
    None => "customer: not found",
});

// default(Maybe<T>) is None; the same None arm handles it.
Console.WriteLine(default(Maybe<string>) switch
{
    string name => $"customer: {name}",
    None => "customer: not found",
});

// Maybe.Some infers the value type, matching Result.Ok and Maybe.None.
Console.WriteLine(Maybe.Some("grace") switch
{
    string name => $"customer: {name}",
    None => "customer: not found",
});

// Composition: Map, Filter, lazy fallbacks.
var customerLabel = FindCustomer("ada")
    .Map(name => name.ToUpperInvariant())
    .Filter(name => name.Length > 3)
    .GetOrElse("UNKNOWN");
Console.WriteLine($"maybe label: {customerLabel}");

Console.WriteLine($"maybe lazy: {Maybe<string>.None.GetOrElse(() => "created lazily")}");
Console.WriteLine($"maybe or-else: {Maybe<string>.None.OrElse(() => Maybe.Some("fallback")).Value}");
Console.WriteLine($"maybe fold: {FindCustomer("ada").Fold(0, (total, name) => total + name.Length)}");

// Match and Switch project or observe both cases.
Console.WriteLine(FindCustomer("grace").Match(
    name => $"maybe match: {name}",
    () => "maybe match: missing"));

FindCustomer("ada").Switch(
    name => Console.WriteLine($"maybe switch: {name}"),
    () => Console.WriteLine("maybe switch: missing"));

// Predicate helpers.
Console.WriteLine(
    $"maybe inspect: contains={FindCustomer("ada").Contains("Ada Lovelace")}, "
    + $"exists={FindCustomer("ada").Exists(name => name.StartsWith("Ada"))}, "
    + $"forall={FindCustomer("ada").ForAll(name => name.Length > 0)}");

// Zip keeps both values only when both are present.
if (FindCustomer("ada").Zip(ParseQuantity("3")).TryGetValue(out var zipped))
    Console.WriteLine($"maybe zip: {zipped.Item1} x{zipped.Item2}");

// Bridge to Result when absence becomes a failure.
Console.WriteLine(FindCustomer("grace")
    .ToResult(new Error("Customer not found.", Code: "CUSTOMER_NOT_FOUND")) switch
{
    string name => $"maybe to result: {name}",
    Error error => $"maybe to result failed: {error.Code} {error.Message}",
});

// ---------------------------------------------------------------------------
// Maybe<T> async: same shape, awaited callbacks.
// ---------------------------------------------------------------------------
var asyncQuantity = await ParseQuantityAsync("3")
    .FilterAsync(quantity => Task.FromResult(quantity > 0))
    .MapAsync(quantity => Task.FromResult(quantity * 2))
    .TapNoneAsync(() =>
    {
        Console.WriteLine("async maybe: none");
        return Task.CompletedTask;
    })
    .GetOrElseAsync(() => Task.FromResult(0));
Console.WriteLine($"async maybe quantity: {asyncQuantity}");

Console.WriteLine(await ParseQuantityAsync("x").MatchAsync(
    quantity => Task.FromResult($"async maybe match: {quantity}"),
    () => Task.FromResult("async maybe match: missing")));

Console.WriteLine(
    $"async maybe fallback: {(await ParseQuantityAsync("x").OrElseAsync(
        () => Task.FromResult(Maybe.Some(1)))).Value}");
Console.WriteLine(
    $"async maybe fold: {await ParseQuantityAsync("3").FoldAsync(
        0,
        (total, quantity) => Task.FromResult(total + quantity))}");
Console.WriteLine(
    $"async maybe inspect: exists={await ParseQuantityAsync("3").ExistsAsync(
        quantity => Task.FromResult(quantity > 0))}, "
    + $"forall={await ParseQuantityAsync("3").ForAllAsync(
        quantity => Task.FromResult(quantity > 0))}, "
    + $"contains={await ParseQuantityAsync("3").ContainsAsync(3)}");

if ((await ParseQuantityAsync("3").ZipAsync(ParseQuantityAsync("4"))).TryGetValue(out var asyncZip))
    Console.WriteLine($"async maybe zip: {asyncZip.Item1} x{asyncZip.Item2}");

// ---------------------------------------------------------------------------
// Result<TValue>: a value or an Error. The default is Error.Uninitialized,
// so switches need no null arm.
// ---------------------------------------------------------------------------
Console.WriteLine(CreateOrder("widget", 0) switch
{
    Order order => $"order: {order.Sku} x{order.Quantity}",
    Error error => $"order failed: {error.Code} {error.Message}",
});

Result<Order> uninitializedOrder = default;
Console.WriteLine(uninitializedOrder switch
{
    Order order => $"order: {order.Sku} x{order.Quantity}",
    Error error => $"order failed: {error.Code} {error.Message}",
});

// Composition: Ensure, Map, Tap, TapError.
var composedOrder = CreateOrder("widget", 2)
    .Ensure(order => order.Quantity <= 5, new Error("Too many items.", Code: "ORDER_LIMIT"))
    .Map(order => order with { Quantity = order.Quantity * 2 })
    .Tap(order => Console.WriteLine($"order tap: {order.Sku} x{order.Quantity}"))
    .TapError(error => Console.WriteLine($"order tap error: {error.Code}"));
Console.WriteLine(composedOrder switch
{
    Order order => $"composed order: {order.Sku} x{order.Quantity}",
    Error error => $"composed order failed: {error.Code} {error.Message}",
});

// MapError changes the failure; GetOrElse / OrElse recover.
Console.WriteLine(CreateOrder("widget", 0)
    .MapError(error => new Error($"order: {error.Message}", Code: "ORDER_WRAPPED")) switch
{
    Order order => $"mapped error order: {order.Sku}",
    Error error => $"mapped error: {error.Code} {error.Message}",
});

Console.WriteLine($"order fallback: {CreateOrder("widget", 0).GetOrElse(new Order("fallback", 0)).Sku}");
Console.WriteLine($"order or-else: {CreateOrder("widget", 0).OrElse(Result<Order>.Ok(new Order("alt", 1))).Value.Sku}");
Console.WriteLine($"order unwrap: {CreateOrder("widget", 1).GetOrThrow().Sku}");

// Conversions and inspection.
Console.WriteLine($"order to maybe: {CreateOrder("widget", 1).ToMaybe().Value.Sku}");
Console.WriteLine($"order flatten: {Result<Result<Order>>.Ok(CreateOrder("widget", 1)).Flatten().Value.Sku}");
Console.WriteLine(
    $"order inspect: contains={CreateOrder("widget", 1).Contains(new Order("widget", 1))}, "
    + $"exists={CreateOrder("widget", 1).Exists(order => order.Sku == "widget")}, "
    + $"forall={CreateOrder("widget", 1).ForAll(order => order.Quantity > 0)}");

// Match and Switch for side effects.
Console.WriteLine(CreateOrder("widget", 1).Match(
    order => $"order match: {order.Sku}",
    error => $"order match failed: {error.Code}"));
CreateOrder("widget", 0).Switch(
    order => Console.WriteLine($"order switch: {order.Sku}"),
    error => Console.WriteLine($"order switch failed: {error.Code}"));

// ---------------------------------------------------------------------------
// Result<TValue> async.
// ---------------------------------------------------------------------------
var asyncOrder = await SaveOrderAsync(new Order("gadget", 2))
    .MapAsync(order => Task.FromResult(order with { Quantity = order.Quantity + 1 }))
    .BindAsync(order => Task.FromResult(Result<Order>.Ok(order)))
    .EnsureAsync(
        order => Task.FromResult(order.Quantity > 0),
        new Error("Quantity must be positive.", Code: "ORDER_QUANTITY"))
    .TapAsync(order =>
    {
        Console.WriteLine($"async order tap: {order.Sku} x{order.Quantity}");
        return Task.CompletedTask;
    })
    .TapErrorAsync(error =>
    {
        Console.WriteLine($"async order tap error: {error.Code}");
        return Task.CompletedTask;
    });
Console.WriteLine(asyncOrder switch
{
    Order order => $"async order: {order.Sku} x{order.Quantity}",
    Error error => $"async order failed: {error.Code} {error.Message}",
});

Console.WriteLine(await SaveOrderAsync(new Order("widget", 1)).MatchAsync(
    order => Task.FromResult($"async order match: {order.Sku}"),
    error => Task.FromResult($"async order match failed: {error.Code}")));

Console.WriteLine(await SaveOrderAsync(new Order("widget", 1))
    .MapErrorAsync(error => Task.FromResult(new Error($"save: {error.Message}", Code: "SAVE_WRAPPED"))) switch
{
    Order order => $"async mapped error order: {order.Sku}",
    Error error => $"async mapped error: {error.Code} {error.Message}",
});

Console.WriteLine(
    $"async order fallback: {(await SaveOrderAsync(new Order("widget", 1))
        .GetOrElseAsync(_ => Task.FromResult(new Order("fallback", 0)))).Sku}");
Console.WriteLine(
    $"async order or-else: {(await SaveOrderAsync(new Order("widget", 1))
        .OrElseAsync(_ => Task.FromResult(Result<Order>.Ok(new Order("alt", 1))))).Value.Sku}");
Console.WriteLine(
    $"async order inspect: exists={await SaveOrderAsync(new Order("gadget", 1))
        .ExistsAsync(order => Task.FromResult(order.Quantity > 0))}, "
    + $"forall={await SaveOrderAsync(new Order("gadget", 1))
        .ForAllAsync(order => Task.FromResult(order.Quantity > 0))}, "
    + $"contains={await SaveOrderAsync(new Order("gadget", 1))
        .ContainsAsync(new Order("gadget", 1))}");

// ---------------------------------------------------------------------------
// Result<TValue, TError>: the failure is a closed domain set. Callers first
// ask "did it work?", then "which failure?".
// ---------------------------------------------------------------------------
Console.WriteLine(UserDirectory.Find("ada") switch
{
    User user => $"user: {user.Id}",
    NotFound notFound => $"not found: {notFound.Id}",
    Unauthorized unauthorized => $"unauthorized: {unauthorized.Reason}",
    null => "user: uninitialized",
});

var userDisplay = UserDirectory.Find("ada")
    .Map(user => user.Id.ToUpperInvariant())
    .Ensure(id => id.Length > 0, new Unauthorized("empty id"))
    .MapError(failure => new Error(failure.Message, Code: "USER_LOOKUP"));

Console.WriteLine(userDisplay switch
{
    string id => $"user display: {id}",
    Error error => $"user display failed: {error.Code} {error.Message}",
    null => "user display: uninitialized",
});

Console.WriteLine($"user fallback: {UserDirectory.Find("banned").GetOrElse(failure => new User(failure.Message)).Id}");
Console.WriteLine($"user or-else: {UserDirectory.Find("banned").OrElse(_ => Result<User, UserLookupFailure>.Ok(new User("fallback"))).Value.Id}");
Console.WriteLine($"user unwrap: {UserDirectory.Find("ada").GetOrThrow().Id}");
Console.WriteLine(
    $"user inspect: contains={UserDirectory.Find("ada").Contains(new User("ada"))}, "
    + $"exists={UserDirectory.Find("ada").Exists(user => user.Id == "ada")}, "
    + $"forall={UserDirectory.Find("ada").ForAll(user => user.Id.Length > 0)}");

// Async domain composition.
var asyncUser = await UserDirectory.FindAsync("ada")
    .MapAsync(user => Task.FromResult(user.Id.ToUpperInvariant()))
    .MapErrorAsync(failure => Task.FromResult(new Error(failure.Message, Code: "USER_LOOKUP")))
    .EnsureAsync(
        id => Task.FromResult(id.Length > 0),
        new Error("Empty user id.", Code: "USER_EMPTY"));
Console.WriteLine(asyncUser switch
{
    string id => $"async user: {id}",
    Error error => $"async user failed: {error.Code} {error.Message}",
    null => "async user: uninitialized",
});

Console.WriteLine(await UserDirectory.FindAsync("banned").MatchAsync(
    user => Task.FromResult($"async user match: {user.Id}"),
    failure => Task.FromResult($"async user match failed: {failure.Message}")));
Console.WriteLine(
    $"async user fallback: {(await UserDirectory.FindAsync("banned")
        .GetOrElseAsync(_ => Task.FromResult(new User("fallback")))).Id}");
Console.WriteLine(
    $"async user or-else: {(await UserDirectory.FindAsync("banned")
        .OrElseAsync(_ => Task.FromResult(Result<User, UserLookupFailure>.Ok(new User("alt"))))).Value.Id}");
Console.WriteLine(
    $"async user inspect: exists={await UserDirectory.FindAsync("ada")
        .ExistsAsync(user => Task.FromResult(user.Id == "ada"))}, "
    + $"forall={await UserDirectory.FindAsync("ada")
        .ForAllAsync(user => Task.FromResult(user.Id.Length > 0))}, "
    + $"contains={await UserDirectory.FindAsync("ada").ContainsAsync(new User("ada"))}");

await UserDirectory.FindAsync("banned").SwitchAsync(
    user =>
    {
        Console.WriteLine($"async user switch: {user.Id}");
        return Task.CompletedTask;
    },
    failure =>
    {
        Console.WriteLine($"async user switch failed: {failure.Message}");
        return Task.CompletedTask;
    });

// A flat domain union is the right shape when the caller's first question is
// "which state?" rather than "did it work?".
Console.WriteLine(GetUser("banned") switch
{
    User user => $"user state: {user.Id}",
    NotFound notFound => $"user state: not found {notFound.Id}",
    Unauthorized unauthorized => $"user state: unauthorized {unauthorized.Reason}",
    null => "user state: uninitialized",
});

// ---------------------------------------------------------------------------
// Uninitialized defaults: composition preserves the bug state; terminal
// helpers throw. Result<TValue> folds the default into Error.Uninitialized.
// ---------------------------------------------------------------------------
var preservedUninitialized = default(Result<Order>)
    .Map(order => order.Sku)
    .MapError(error => error)
    .IsUninitialized;
Console.WriteLine($"uninitialized preserved: {preservedUninitialized}");

var preservedDomain = default(Result<Order, UserLookupFailure>)
    .Map(order => order.Sku)
    .IsInitialized;
Console.WriteLine($"domain uninitialized preserved: {!preservedDomain}");

// ---------------------------------------------------------------------------
// Sequence helpers: short-circuit on the first absence or failure.
// ---------------------------------------------------------------------------
var quantities = new[] { "1", "2", "3" }.Traverse(ParseQuantityResult);
Console.WriteLine(quantities switch
{
    IReadOnlyList<int> values => $"quantities: {string.Join(", ", values)}",
    Error error => $"quantities failed: {error.Message}",
});

var present = new[] { "1", "x", "3" }.Select(ParseQuantity).Choose();
Console.WriteLine($"present: {string.Join(", ", present)}");

var parsed = new[] { "1", "x", "3" }.Select(ParseQuantityResult).ToArray();
var (successes, failures) = parsed.Partition();
Console.WriteLine($"successes: {string.Join(", ", successes)}; failures: {failures.Count}");

Console.WriteLine($"first or none: {new[] { "1", "2", "3" }.FirstOrNone().Value}");

var asyncQuantities = await new[] { "1", "2", "3" }.TraverseAsync(ParseQuantityResultAsync);
Console.WriteLine(asyncQuantities switch
{
    IReadOnlyList<int> values => $"async quantities: {string.Join(", ", values)}",
    Error error => $"async quantities failed: {error.Message}",
});

var asyncMaybeSequence = await new[]
{
    ParseQuantityAsync("1"),
    ParseQuantityAsync("x"),
    ParseQuantityAsync("3"),
}.SequenceAsync();
Console.WriteLine(asyncMaybeSequence switch
{
    IReadOnlyList<int> values => $"async maybe sequence: {string.Join(", ", values)}",
    None => "async maybe sequence: missing",
});

var asyncLookupSequence = await new[]
{
    UserDirectory.FindAsync("ada"),
    UserDirectory.FindAsync("banned"),
}.SequenceAsync();
Console.WriteLine(asyncLookupSequence switch
{
    IReadOnlyList<User> users => $"async user sequence: {string.Join(", ", users.Select(user => user.Id))}",
    UserLookupFailure failure => $"async user sequence failed: {failure.Message}",
    null => "async user sequence: uninitialized",
});

// ---------------------------------------------------------------------------
// LINQ query syntax composes Maybe and Result values.
// ---------------------------------------------------------------------------
var maybeTotal = from price in ParseMaybePrice("19.99")
                 from quantity in ParseQuantity("3")
                 select price * quantity;

Console.WriteLine(maybeTotal switch
{
    decimal total => $"maybe total: {total}",
    None => "maybe total: missing",
});

var resultTotal = from price in ParsePrice("19.99")
                  from quantity in ParseQuantity("3").ToResult(new Error("Quantity is required."))
                  select price * quantity;

Console.WriteLine(resultTotal switch
{
    decimal total => $"result total: {total}",
    Error error => $"result total failed: {error.Message}",
});

// ---------------------------------------------------------------------------
// Error handling: exceptions become values; codes stay stable.
// ---------------------------------------------------------------------------
Console.WriteLine(Result.Ok(42) switch
{
    int value => $"factory ok: {value}",
    Error error => $"factory failed: {error.Message}",
});

Console.WriteLine(Result.Try(() => int.Parse("not a number")) switch
{
    int value => $"factory try: {value}",
    Error error => $"factory try failed: {error.Code}",
});

Console.WriteLine(Result<int>.Try(() => throw new InvalidOperationException("boom")) switch
{
    int value => $"exception value: {value}",
    Error error => $"exception: {error.Code} {error.Message}",
});

Console.WriteLine(await Result<int>.TryAsync(
    () => Task.FromException<int>(new TimeoutException("slow"))) switch
{
    int value => $"async exception value: {value}",
    Error error => $"async exception: {error.Code} {error.Message}",
});

// ---------------------------------------------------------------------------
// Data management: repository reads return Maybe, writes return Result.
// The same shapes work with DbSet<T>.FindAsync and SaveChangesAsync.
// ---------------------------------------------------------------------------
var repository = new OrderRepository();

Console.WriteLine((await repository.FindAsync("widget")) switch
{
    Order order => $"found: {order.Sku} x{order.Quantity}",
    None => "not found",
});

var workflow = await (await repository.FindAsync("widget"))
    .Map(order => order with { Quantity = order.Quantity + 1 })
    .ToResult(new Error("Order not found.", Code: "ORDER_NOT_FOUND"))
    .BindAsync(order => repository.SaveAsync(order))
    .EnsureAsync(
        order => Task.FromResult(order.Quantity > 0),
        new Error("Quantity must be positive.", Code: "ORDER_QUANTITY"))
    .TapAsync(order =>
    {
        Console.WriteLine($"workflow saved: {order.Sku} x{order.Quantity}");
        return Task.CompletedTask;
    })
    .TapErrorAsync(error =>
    {
        Console.WriteLine($"workflow failed: {error.Code}");
        return Task.CompletedTask;
    });

Console.WriteLine(workflow switch
{
    Order order => $"workflow result: {order.Sku} x{order.Quantity}",
    Error error => $"workflow failed: {error.Code} {error.Message}",
});

// ---------------------------------------------------------------------------
// Local helpers and domain types.
// ---------------------------------------------------------------------------
static Maybe<int> ParseQuantity(string text)
    => int.TryParse(text, out var value) && value > 0 ? value : Maybe.None;

static Task<Maybe<int>> ParseQuantityAsync(string text)
    => Task.FromResult(ParseQuantity(text));

static Result<int> ParseQuantityResult(string text)
    => int.TryParse(text, out var value) && value > 0
        ? Result<int>.Ok(value)
        : Result<int>.Fail(new Error($"'{text}' is not a positive integer.", Code: "QUANTITY_FORMAT"));

static Task<Result<int>> ParseQuantityResultAsync(string text)
    => Task.FromResult(ParseQuantityResult(text));

static Maybe<decimal> ParseMaybePrice(string text)
    => decimal.TryParse(text, out var value) ? value : Maybe.None;

static Result<decimal> ParsePrice(string text)
    => decimal.TryParse(text, out var value)
        ? Result<decimal>.Ok(value)
        : Result<decimal>.Fail(new Error("Price must be a number.", Code: "PRICE_FORMAT"));

static Result<Order> CreateOrder(string sku, int quantity)
    => quantity > 0
        ? Result<Order>.Ok(new Order(sku, quantity))
        : Result<Order>.Fail(new Error("Quantity must be positive.", Code: "ORDER_QUANTITY"));

static GetUserResult GetUser(string id)
    => id switch
    {
        "ada" => new User("ada"),
        "banned" => new Unauthorized("banned"),
        _ => new NotFound(id),
    };

static Task<Result<Order>> SaveOrderAsync(Order order)
    => Task.FromResult(order.Sku == "widget"
        ? Result<Order>.Fail(new Error("Concurrency conflict.", Code: "DB_CONCURRENCY"))
        : Result<Order>.Ok(order));

internal static class UserDirectory
{
    public static Result<User, UserLookupFailure> Find(string id)
        => id switch
        {
            "ada" => Result<User, UserLookupFailure>.Ok(new User("ada")),
            "banned" => Result<User, UserLookupFailure>.Fail(new Unauthorized("banned")),
            _ => Result<User, UserLookupFailure>.Fail(new NotFound(id)),
        };

    public static Task<Result<User, UserLookupFailure>> FindAsync(string id)
        => Task.FromResult(Find(id));
}

internal sealed record Order(string Sku, int Quantity);

internal sealed record User(string Id);

internal closed record class UserLookupFailure(string Message);

internal sealed record class NotFound(string Id) : UserLookupFailure($"User {Id} was not found.");

internal sealed record class Unauthorized(string Reason)
    : UserLookupFailure($"User is unauthorized: {Reason}.");

internal union GetUserResult(User, NotFound, Unauthorized);

internal sealed class OrderRepository
{
    private readonly Dictionary<string, Order> _orders = new() { ["widget"] = new("widget", 1) };

    public Maybe<Order> Find(string sku)
        => _orders.TryGetValue(sku, out var order) ? order : Maybe.None;

    public Result<Order> Save(Order order)
    {
        if (order.Sku == "widget")
            return Result<Order>.Fail(new Error("Concurrency conflict.", Code: "DB_CONCURRENCY"));

        _orders[order.Sku] = order;
        return Result<Order>.Ok(order);
    }

    public Task<Maybe<Order>> FindAsync(string sku) => Task.FromResult(Find(sku));

    public Task<Result<Order>> SaveAsync(Order order) => Task.FromResult(Save(order));
}
