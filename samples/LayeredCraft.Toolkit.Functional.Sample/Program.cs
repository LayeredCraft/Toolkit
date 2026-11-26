using LayeredCraft.Toolkit.Functional;

// ---------------------------------------------------------------------------
// Maybe: a value that may be absent. Absence is not a failure.
// ---------------------------------------------------------------------------
var customers = new Dictionary<int, string> { [1] = "Ada", [2] = "Grace" };

var customer = customers.TryGetValue(3, out var name)
    ? Maybe<string>.Some(name)
    : Maybe<string>.None;

Console.WriteLine(customer.Match(value => $"customer: {value}", () => "customer: not found"));

// Maybe composes without null checks.
var greeting = customer
    .Map(value => $"Hello, {value}!")
    .Filter(value => value.Length > 5)
    .GetOrElse("Hello, stranger!");

Console.WriteLine(greeting);

// ---------------------------------------------------------------------------
// Result with the default Error: validation and expected failure.
// ---------------------------------------------------------------------------
var order = CreateOrder("widget", -2);
Console.WriteLine(
    order.Match(
        value => $"order: {value.Sku} x {value.Quantity}",
        error => $"order failed: {error.Message}"));

// ---------------------------------------------------------------------------
// Result with a domain error union: exhaustive matching on failure cases.
// ---------------------------------------------------------------------------
var payment = Charge("card_declined");
Console.WriteLine(
    payment.Match(
        receipt => $"paid: {receipt.Amount}",
        error => error switch
        {
            CardDeclined declined => $"declined: {declined.Reason}",
            InsufficientFunds funds => $"insufficient: {funds.Available}",
        }));

// ---------------------------------------------------------------------------
// Sequence helpers short-circuit on the first absent or failed value.
// ---------------------------------------------------------------------------
var quantities = new[] { "1", "2", "3" }.Traverse(ParseQuantity);
Console.WriteLine(
    quantities.Match(
        values => $"quantities: {string.Join(", ", values)}",
        () => "quantities: none"));

var present = new[] { "1", "x", "3" }.Select(ParseQuantity).Choose();
Console.WriteLine($"present: {string.Join(", ", present)}");

var parsed = new[] { "1", "x", "3" }.Select(ParseResult).ToArray();
var (successes, failures) = parsed.Partition();
Console.WriteLine($"successes: {string.Join(", ", successes)}; failures: {failures.Count}");

// ---------------------------------------------------------------------------
// LINQ query syntax composes Maybe and Result values.
// ---------------------------------------------------------------------------
var maybeTotal =
    from price in ParseMaybePrice("19.99")
    from quantity in ParseQuantity("3")
    select price * quantity;

Console.WriteLine(maybeTotal.Match(value => $"maybe total: {value}", () => "maybe total: missing"));

var resultTotal =
    from price in ParsePrice("19.99")
    from quantity in ParseQuantity("3").ToResult(new Error("Quantity is required."))
    select price * quantity;

Console.WriteLine(
    resultTotal.Match(
        value => $"result total: {value}",
        error => $"result total failed: {error.Message}"));

// ---------------------------------------------------------------------------
// Async composition keeps the same shape.
// ---------------------------------------------------------------------------
var asyncTotal = await LoadPriceAsync("19.99").MapAsync(price => price * 2);
Console.WriteLine(
    asyncTotal.Match(value => $"async total: {value}", error => $"async failed: {error.Message}"));

// ---------------------------------------------------------------------------
// Maybe to Result: absence becomes an error.
// ---------------------------------------------------------------------------
var required = customer.ToResult(new Error("A customer is required."));
Console.WriteLine(
    required.Match(value => $"required: {value}", error => $"required failed: {error.Message}"));

// ---------------------------------------------------------------------------
// Result.Try captures an exception and wraps it in the error.
// ---------------------------------------------------------------------------
var failed = Result<int, Error>.Try(
    () => throw new InvalidOperationException("boom"),
    Error.FromException);

Console.WriteLine(
    failed.Match(
        value => $"value: {value}",
        error => $"error: {error.Message} ({error.Exception?.GetType().Name})"));

// ---------------------------------------------------------------------------
// Exception as the error type: return it, do not throw it.
// ---------------------------------------------------------------------------
var parsedWithException = ParseWithException("abc");
Console.WriteLine(
    parsedWithException.Match(
        value => $"parsed: {value}",
        exception => $"exception: {exception.GetType().Name}: {exception.Message}"));

// ---------------------------------------------------------------------------
// Switch runs one action per case without producing a value.
// ---------------------------------------------------------------------------
order.Switch(
    value => Console.WriteLine($"switch ok: {value.Sku}"),
    error => Console.WriteLine($"switch error: {error.Message}"));

// ---------------------------------------------------------------------------
// A default result is uninitialized: neither success nor failure.
// ---------------------------------------------------------------------------
var uninitialized = default(Result<int, Error>);
Console.WriteLine($"uninitialized: {uninitialized.IsInitialized}");

static Maybe<int> ParseQuantity(string text)
    => int.TryParse(text, out var value) && value > 0 ? value : Maybe.None;

static Maybe<decimal> ParseMaybePrice(string text)
    => decimal.TryParse(text, out var value) ? value : Maybe.None;

static Result<decimal, Error> ParsePrice(string text)
    => decimal.TryParse(text, out var value)
        ? Result<decimal, Error>.Ok(value)
        : Result<decimal, Error>.Fail("Price must be a number.");

static Result<int, Error> ParseResult(string text)
    => int.TryParse(text, out var value)
        ? Result<int, Error>.Ok(value)
        : Result<int, Error>.Fail("Not a number.");

static Result<decimal, Exception> ParseWithException(string text)
    => decimal.TryParse(text, out var value)
        ? Result<decimal, Exception>.Ok(value)
        : Result<decimal, Exception>.Fail(new FormatException($"'{text}' is not a number."));

static Result<Order, Error> CreateOrder(string sku, int quantity)
    => quantity > 0
        ? Result<Order, Error>.Ok(new Order(sku, quantity))
        : Result<Order, Error>.Fail("Quantity must be positive.");

static Result<Receipt, PaymentError> Charge(string card)
    => card == "card_declined"
        ? Result<Receipt, PaymentError>.Fail(new CardDeclined("The card was declined."))
        : Result<Receipt, PaymentError>.Ok(new Receipt(19.99m));

static Task<Result<decimal, Error>> LoadPriceAsync(string text)
    => Task.FromResult(ParsePrice(text));

internal sealed record Order(string Sku, int Quantity);

internal sealed record Receipt(decimal Amount);

internal closed record class PaymentError(string Message);

internal sealed record class CardDeclined(string Reason) : PaymentError(Reason);

internal sealed record class InsufficientFunds(decimal Available) : PaymentError(
    $"Insufficient funds: {Available}");
