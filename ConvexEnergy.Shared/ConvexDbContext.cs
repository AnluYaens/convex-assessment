using Microsoft.EntityFrameworkCore;

namespace ConvexEnergy.Shared;

public class ConvexDbContext(DbContextOptions<ConvexDbContext> options) : DbContext(options)
{
    public DbSet<DayAheadPeriodPrice> DayAheadPrices => Set<DayAheadPeriodPrice>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DayAheadPeriodPrice>(e =>
        {
            e.HasIndex(x => new { x.DeliveryDate, x.Period }).IsUnique();
        });
    }
}