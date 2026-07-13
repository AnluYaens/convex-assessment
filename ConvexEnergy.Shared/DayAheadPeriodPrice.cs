namespace ConvexEnergy.Shared;

public class DayAheadPeriodPrice
{
    public long Id { get; set; }
    public DateOnly DeliveryDate { get; set; }
    public int Period { get; set; }             // 1-based 15-min period
    public decimal PriceEs { get; set; }        // EUR/MWh, Spanish system
    public decimal PricePt { get; set; }        // EUR/MWh, Portuguese system
    public int FileVersion { get; set; }        // the .N suffix of the source file
    public DateTime ImportedAtUtc { get; set; }
}