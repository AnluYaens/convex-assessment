using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ConvexEnergy.Shared;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ConvexDbContext>
{
    public ConvexDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<ConvexDbContext>()
            .UseSqlite("Data Source=data/convexenergy.db")
            .Options);
}