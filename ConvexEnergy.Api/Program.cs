using System.Globalization;
using System.Text;
using ConvexEnergy.Shared;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ConvexDbContext>(o =>
    o.UseSqlite(builder.Configuration.GetConnectionString("Convex")));

var app = builder.Build();

var madrid = TimeZoneInfo.FindSystemTimeZoneById("Europe/Madrid");

app.MapGet("/api/day-ahead-prices", async (
    ConvexDbContext db,
    HttpRequest request,
    DateOnly? from,
    DateOnly? to,
    CancellationToken ct) =>
{
    var query = db.DayAheadPrices.AsNoTracking().AsQueryable();
    if (from is not null) query = query.Where(p => p.DeliveryDate >= from);
    if (to is not null) query = query.Where(p => p.DeliveryDate <= to);

    var rows = await query
        .OrderBy(p => p.DeliveryDate).ThenBy(p => p.Period)
        .ToListAsync(ct);

    var points = rows.Select(p => new
    {
        startCet = PeriodStartCet(p.DeliveryDate, p.Period),
        deliveryDate = p.DeliveryDate,
        period = p.Period,
        priceEsEurMwh = p.PriceEs,
        pricePtEurMwh = p.PricePt
    }).ToList();

    // Content negotiation per assignment: application/json (default) and text/plain (semicolon CSV).
    if (request.Headers.Accept.ToString().Contains("text/plain", StringComparison.OrdinalIgnoreCase))
    {
        var sb = new StringBuilder();
        sb.AppendLine("startCet;deliveryDate;period;priceEsEurMwh;pricePtEurMwh");
        foreach (var p in points)
            sb.AppendLine(string.Create(CultureInfo.InvariantCulture,
                $"{p.startCet:yyyy-MM-dd'T'HH:mm:sszzz};{p.deliveryDate:yyyy-MM-dd};{p.period};{p.priceEsEurMwh};{p.pricePtEurMwh}"));
        return Results.Text(sb.ToString(), "text/plain");
    }

    return Results.Json(points);
});

app.Run();

// Period N starts at Madrid-local midnight (as an absolute instant) plus (N-1)*15min
// of ABSOLUTE time, then rendered back in Madrid local time. This stays correct on
// DST days (92/100 periods), where naive wall-clock arithmetic would drift.
DateTimeOffset PeriodStartCet(DateOnly day, int period)
{
    var localMidnight = day.ToDateTime(TimeOnly.MinValue);           // unspecified kind
    var utcMidnight = TimeZoneInfo.ConvertTimeToUtc(localMidnight, madrid);
    var startUtc = DateTime.SpecifyKind(
        utcMidnight.AddMinutes((period - 1) * 15), DateTimeKind.Utc);
    return TimeZoneInfo.ConvertTime((DateTimeOffset)startUtc, madrid);
}