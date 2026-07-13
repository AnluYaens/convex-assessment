using Microsoft.Extensions.Options;
using Quartz;

namespace ConvexEnergy.Worker;

[DisallowConcurrentExecution] // startup trigger and cron trigger must never overlap
public sealed class DownloadAuctionResultsJob(
    MarginalPdbcImportService importer,
    IOptions<OmieOptions> options,
    ILogger<DownloadAuctionResultsJob> logger) : IJob
{
    private static readonly TimeZoneInfo Madrid =
        TimeZoneInfo.FindSystemTimeZoneById("Europe/Madrid");

    public async Task Execute(IJobExecutionContext context)
    {
        // "Today" defined in market-local time, not server time: a server in UTC
        // or elsewhere must still agree with OMIE about which delivery day is next.
        var todayMadrid = DateOnly.FromDateTime(
            TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Madrid));

        var from = todayMadrid.AddDays(-options.Value.BackfillDays);
        var to = todayMadrid.AddDays(1); // D+1 publishes early afternoon of day D

        logger.LogInformation("Run started: checking delivery days {From} to {To}", from, to);

        for (var day = from; day <= to; day = day.AddDays(1))
        {
            try
            {
                await importer.ImportDayAsync(day, context.CancellationToken);
            }
            catch (Exception ex)
            {
                // One bad day (network blip, malformed file) must not abort the rest.
                logger.LogError(ex, "Import failed for {Day}", day);
            }
        }

        logger.LogInformation("Run finished");
    }
}