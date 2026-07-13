using System.Globalization;
using ConvexEnergy.Shared;

namespace ConvexEnergy.Tests;

public class MarginalPdbcParserTests
{
    private static string SamplePath =>
        Path.Combine(AppContext.BaseDirectory, "samples", "marginalpdbc_20260712.1");

    [Fact]
    public void Parse_RealSampleFile_Returns96SequentialPeriods()
    {
        var prices = MarginalPdbcParser.Parse(File.ReadAllText(SamplePath));

        Assert.Equal(96, prices.Count);
        Assert.All(prices, p => Assert.Equal(new DateOnly(2026, 7, 12), p.DeliveryDate));
        Assert.Equal(Enumerable.Range(1, 96), prices.Select(p => p.Period));
    }

    [Fact]
    public void Parse_RealSampleFile_MatchesKnownValues()
    {
        var prices = MarginalPdbcParser.Parse(File.ReadAllText(SamplePath));

        var first = prices.Single(p => p.Period == 1);
        Assert.Equal(140.51m, first.PricePt);
        Assert.Equal(140.51m, first.PriceEs);

        Assert.Equal(139.93m, prices.Single(p => p.Period == 96).PriceEs);
    }

    [Fact]
    public void Parse_ToleratesCrlfLineEndings()
    {
        var prices = MarginalPdbcParser.Parse("MARGINALPDBC;\r\n2026;07;12;1;100.5;101.25;\r\n*\r\n");

        var p = Assert.Single(prices);
        Assert.Equal(100.5m, p.PricePt);
        Assert.Equal(101.25m, p.PriceEs);
    }

    [Fact]
    public void Parse_UsesInvariantCulture_RegardlessOfSystemLocale()
    {
        var original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE"); // ',' is the decimal separator here
            var prices = MarginalPdbcParser.Parse("MARGINALPDBC;\n2026;07;12;1;140.51;140.51;\n*\n");
            Assert.Equal(140.51m, prices[0].PriceEs);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Fact]
    public void Parse_ThrowsOnContentWithoutPriceRows()
    {
        Assert.Throws<FormatException>(() => MarginalPdbcParser.Parse("MARGINALPDBC;\n*\n"));
    }
}