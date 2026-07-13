using ConvexEnergy.Shared;
using ConvexEnergy.Worker;
using Microsoft.EntityFrameworkCore;
using Quartz;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<OmieOptions>(
    builder.Configuration.GetSection(OmieOptions.SectionName));

builder.Services.AddDbContext<ConvexDbContext>(o =>
    o.UseSqlite(builder.Configuration.GetConnectionString("Convex")));

builder.Services.AddHttpClient<OmieDownloadClient>();
builder.Services.AddScoped<MarginalPdbcImportService>();

var cron = builder.Configuration
    .GetSection(OmieOptions.SectionName)[nameof(OmieOptions.CronSchedule)]
    ?? "0 10 * * * ?";

builder.Services.AddQuartz(q =>
{
    var jobKey = new JobKey("download-auction-results");
    q.AddJob<DownloadAuctionResultsJob>(o => o.WithIdentity(jobKey));

    // Immediate run on startup: this is what makes restarts self-healing.
    q.AddTrigger(t => t.ForJob(jobKey)
        .WithIdentity("download-on-startup")
        .StartNow());

    // Recurring run, scheduled in market-local time.
    q.AddTrigger(t => t.ForJob(jobKey)
        .WithIdentity("download-on-schedule")
        .WithCronSchedule(cron, x =>
            x.InTimeZone(TimeZoneInfo.FindSystemTimeZoneById("Europe/Madrid"))));
});

builder.Services.AddQuartzHostedService(o => o.WaitForJobsToComplete = true);

var host = builder.Build();

// The Worker owns the schema: migrate before anything runs (see AGENTS.md).
Directory.CreateDirectory("../data");
using (var scope = host.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<ConvexDbContext>().Database.Migrate();
}

host.Run();