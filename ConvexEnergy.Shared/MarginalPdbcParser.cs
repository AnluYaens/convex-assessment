using System.Globalization;

namespace ConvexEnergy.Shared;

public sealed record AuctionPrice(
    DateOnly DeliveryDate,
    int Period,
    decimal PricePt,   // file column 5: "Precio marginal zona Portuguesa" (OMIE spec 6.18)
    decimal PriceEs);  // file column 6: "Precio marginal zona Española" (OMIE spec 6.18)

public static class MarginalPdbcParser
{
    public static IReadOnlyList<AuctionPrice> Parse(string content)
    {
        var prices = new List<AuctionPrice>();
        var lines = content.Split('\n',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var line in lines)
        {
            if (line.StartsWith("MARGINALPDBC") || line.StartsWith('*'))
                continue; // header and terminator

            var parts = line.Split(';', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 6)
                throw new FormatException($"Unexpected marginalpdbc line: '{line}'");

            prices.Add(new AuctionPrice(
                DeliveryDate: new DateOnly(int.Parse(parts[0]), int.Parse(parts[1]), int.Parse(parts[2])),
                Period: int.Parse(parts[3]),
                PricePt: decimal.Parse(parts[4], CultureInfo.InvariantCulture),   // OMIE spec 6.18: MarginalPT
                PriceEs: decimal.Parse(parts[5], CultureInfo.InvariantCulture))); // OMIE spec 6.18: MarginalES
        }

        if (prices.Count == 0)
            throw new FormatException("File contained no price rows.");

        return prices;
    }
}