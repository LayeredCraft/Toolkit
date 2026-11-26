using LayeredCraft.Toolkit.Functional;

// ---------------------------------------------------------------------------
// Maybe<T>: an optional value. switch is the idiomatic consumer.
// ---------------------------------------------------------------------------
var customers = new Dictionary<string, string> { ["ada"] = "Ada Lovelace" };

Maybe<string> FindCustomer(string id)
    => customers.TryGetValue(id, out var name) ? name : Maybe.None;

Console.WriteLine(FindCustomer("grace") switch
{
    string name => $"customer: {name}",
    null => "customer: not found",
});

// ---------------------------------------------------------------------------
// Result<T>: a value or an Error. The default is a failure with
// Error.Uninitialized, so the switch needs no null arm.
// ---------------------------------------------------------------------------
Console.WriteLine(CreateOrder("widget", 0) switch
{
    Order order => $"order: {order.Sku} x{order.Quantity}",
    Error error => $"order failed: {error.Code} {error.Message}",
});

Result<Order> uninitialized = default;
Console.WriteLine(uninitialized switch
{
    Order order => $"order: {order.Sku}",
    Error error => $"order failed: {error.Code} {error.Message}",
});

// ---------------------------------------------------------------------------
// Result<T, TError>: a binary grouping whose failure is a closed domain set.
// The caller's first question is "did we get the user?", then "why not?".
// ---------------------------------------------------------------------------
Console.WriteLine(FindUser("ada") switch
{
    User user => $"user: {user.Id}",
    NotFound notFound => $"not found: {notFound.Id}",
    Unauthorized unauthorized => $"unauthorized: {unauthorized.Reason}",
    null => "user: uninitialized",
});

// The binary grouping earns its layer when the success path composes.
var display = FindUser("ada")
    .Map(user => user.Id.ToUpperInvariant())
    .MapError(failure => new Error($"lookup failed: {failure.Message}", Code: "USER_LOOKUP"));

Console.WriteLine(display switch
{
    string id => $"user: {id}",
    Error error => $"user failed: {error.Code} {error.Message}",
    null => "user: uninitialized",
});

// ---------------------------------------------------------------------------
// A flat domain union: when the caller's first question is "which state?".
// ---------------------------------------------------------------------------
Console.WriteLine(GetUser("banned") switch
{
    User user => $"user: {user.Id}",
    NotFound notFound => $"not found: {notFound.Id}",
    Unauthorized unauthorized => $"unauthorized: {unauthorized.Reason}",
    null => "user: uninitialized",
});

// ---------------------------------------------------------------------------
// Sequence helpers short-circuit on the first failure.
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

// ---------------------------------------------------------------------------
// LINQ query syntax composes Maybe and Result values.
// ---------------------------------------------------------------------------
var maybeTotal = from price in ParseMaybePrice("19.99")
                 from quantity in ParseQuantity("3")
                 select price * quantity;

Console.WriteLine(maybeTotal switch
{
    decimal total => $"maybe total: {total}",
    null => "maybe total: missing",
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
// Async composition keeps the same shape.
// ---------------------------------------------------------------------------
Console.WriteLine(await ParsePriceAsync("19.99").MapAsync(price => price * 2) switch
{
    decimal total => $"async total: {total}",
    Error error => $"async total failed: {error.Message}",
});

// ---------------------------------------------------------------------------
// Result.Try captures an exception without throwing.
// ---------------------------------------------------------------------------
Console.WriteLine(Result<int>.Try(() => throw new InvalidOperationException("boom")) switch
{
    int value => $"parsed: {value}",
    Error error => $"exception: {error.Code} {error.Message}",
});

// ---------------------------------------------------------------------------
// A repository shaped like EF Core: queries return Maybe, writes return Result.
// The same shapes work with DbSet<T>.FindAsync and SaveChangesAsync.
// ---------------------------------------------------------------------------
var repository = new OrderRepository();

Console.WriteLine(repository.Find("widget") switch
{
    Order order => $"found: {order.Sku}",
    null => "not found",
});

Console.WriteLine(repository.Save(new Order("widget", 1)) switch
{
    Order order => $"saved: {order.Sku}",
    Error error => $"save failed: {error.Code} {error.Message}",
});

static Maybe<int> ParseQuantity(string text)
    => int.TryParse(text, out var value) && value > 0 ? value : Maybe.None;

static Result<int> ParseQuantityResult(string text)
    => int.TryParse(text, out var value) && value > 0
        ? Result<int>.Ok(value)
        : Result<int>.Fail(new Error($"'{text}' is not a positive integer.", Code: "QUANTITY_FORMAT"));

static Maybe<decimal> ParseMaybePrice(string text)
    => decimal.TryParse(text, out var value) ? value : Maybe.None;

static Result<decimal> ParsePrice(string text)
    => decimal.TryParse(text, out var value)
        ? Result<decimal>.Ok(value)
        : Result<decimal>.Fail(new Error("Price must be a number.", Code: "PRICE_FORMAT"));

static Task<Result<decimal>> ParsePriceAsync(string text)
    => Task.FromResult(ParsePrice(text));

static Result<Order> CreateOrder(string sku, int quantity)
    => quantity > 0
        ? Result<Order>.Ok(new Order(sku, quantity))
        : Result<Order>.Fail(new Error("Quantity must be positive.", Code: "ORDER_QUANTITY"));

static Result<User, UserLookupFailure> FindUser(string id)
    => id switch
    {
        "ada" => Result<User, UserLookupFailure>.Ok(new User("ada")),
        "banned" => Result<User, UserLookupFailure>.Fail(new Unauthorized("banned")),
        _ => Result<User, UserLookupFailure>.Fail(new NotFound(id)),
    };

static GetUserResult GetUser(string id)
    => id switch
    {
        "ada" => new User("ada"),
        "banned" => new Unauthorized("banned"),
        _ => new NotFound(id),
    };

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
}
