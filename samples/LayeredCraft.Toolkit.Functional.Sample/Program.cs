using LayeredCraft.Toolkit.Functional;

var quantity = ParseQuantity("3")
    .Map(n => n * 2)
    .Filter(n => n <= 10)
    .GetOrElse(0);

Console.WriteLine($"quantity: {quantity}");

var price = ParsePrice("19.99")
    .Bind(value => value > 0
        ? Result<decimal, Error>.Ok(value)
        : Result<decimal, Error>.Fail("Price must be positive."))
    .Map(value => Math.Round(value, 2))
    .Tap(value => Console.WriteLine($"price: {value}"));

Console.WriteLine(price.Match(
    value => $"total: {value * quantity}",
    error => $"failed: {error.Message}"));

// The types are C# 15 unions: switch unwraps the case, and null is the empty state.
Console.WriteLine(Describe(ParseQuantity("3")));
Console.WriteLine(Describe(ParseQuantity("x")));
Console.WriteLine(DescribeResult(price));

// Sequence helpers short-circuit on the first absent or failed value.
var quantities = new[] { "1", "2", "3" }.Traverse(ParseQuantity);
Console.WriteLine(quantities.Match(
    values => $"quantities: {string.Join(",", values)}",
    () => "quantities: none"));

var prices = new[] { "1.50", "2.50" }.Traverse(ParsePrice);
Console.WriteLine(prices.Match(
    values => $"prices: {string.Join(",", values)}",
    error => $"prices: failed ({error.Message})"));

static string Describe(Maybe<int> maybe) => maybe switch
{
    int value => $"has {value}",
    null => "nothing",
};

static string DescribeResult(Result<decimal, Error> result) => result switch
{
    decimal value => $"ok {value}",
    Error error => $"error {error.Message}",
    null => "uninitialized",
};

static Maybe<int> ParseQuantity(string text)
    => int.TryParse(text, out var value) && value > 0 ? value : Maybe.None;

static Result<decimal, Error> ParsePrice(string text)
    => decimal.TryParse(text, out var value)
        ? Result<decimal, Error>.Ok(value)
        : Result<decimal, Error>.Fail("Price must be a number.");
