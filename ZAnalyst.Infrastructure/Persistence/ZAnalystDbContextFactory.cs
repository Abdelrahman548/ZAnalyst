using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ZAnalyst.Infrastructure.Persistence;

public sealed class ZAnalystDbContextFactory : IDesignTimeDbContextFactory<ZAnalystDbContext>
{
    public ZAnalystDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ZANALYST_CONNECTION_STRING")
            ?? "Data Source=zanalyst.db";

        var options = new DbContextOptionsBuilder<ZAnalystDbContext>()
            .UseSqlite(connectionString)
            .Options;

        return new ZAnalystDbContext(options);
    }
}
