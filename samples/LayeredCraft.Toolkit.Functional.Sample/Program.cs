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

static Maybe<int> ParseQuantity(string text)
    => int.TryParse(text, out var value) && value > 0 ? value : Maybe.None;

static Result<decimal, Error> ParsePrice(string text)
    => decimal.TryParse(text, out var value)
        ? Result<decimal, Error>.Ok(value)
        : Result<decimal, Error>.Fail("Price must be a number.");
