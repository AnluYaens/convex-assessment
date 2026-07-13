using ConvexEnergy.Shared;
using Microsoft.EntityFrameworkCore;

namespace ConvexEnergy.Worker;

public sealed class MarginalPdbcImportService(
    ConvexDbContext db,
    OmieDownloadClient client,
    ILogger<MarginalPdbcImportService> logger)
{
    public async Task ImportDayAsync(DateOnly day, CancellationToken ct)
    {
        var latest = await client.TryDownloadLatestAsync(day, ct);
        if (latest is null)
        {
            logger.LogInformation("No file available yet for {Day}", day);
            return;
        }

        var (version, content) = latest.Value;

        var storedVersion = await db.DayAheadPrices
            .Where(p => p.DeliveryDate == day)
            .MaxAsync(p => (int?)p.FileVersion, ct);

        if (storedVersion >= version)
        {
            logger.LogInformation(
                "Skipping {Day}: stored version {Stored} >= available version {Available}",
                day, storedVersion, version);
            return;
        }

        var prices = MarginalPdbcParser.Parse(content);

        // Replace-day-atomically: delete old rows and insert the new version in one
        // transaction, so readers never see a half-imported day and the unique index
        // on (DeliveryDate, Period) stays satisfied.
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var deleted = await db.DayAheadPrices
            .Where(p => p.DeliveryDate == day)
            .ExecuteDeleteAsync(ct);

        var importedAt = DateTime.UtcNow;
        db.DayAheadPrices.AddRange(prices.Select(p => new DayAheadPeriodPrice
        {
            DeliveryDate = p.DeliveryDate,
            Period = p.Period,
            PricePt = p.PricePt,
            PriceEs = p.PriceEs,
            FileVersion = version,
            ImportedAtUtc = importedAt
        }));

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        logger.LogInformation(
            "Imported {Count} periods for {Day} (file version {Version}, replaced {Deleted} rows of version {Old})",
            prices.Count, day, version, deleted, storedVersion?.ToString() ?? "none");
    }
}