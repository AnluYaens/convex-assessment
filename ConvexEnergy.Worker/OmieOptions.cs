namespace ConvexEnergy.Worker;

public sealed class OmieOptions
{
    public const string SectionName = "Omie";

    public string DownloadUrlTemplate { get; set; } = "";
    public string CronSchedule { get; set; } = "0 10 * * * ?"; // hourly at :10
    public int BackfillDays { get; set; } = 7;
    public int MaxVersionProbe { get; set; } = 5;
}