using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace KitchenwareBot.Infrastructure.Persistence;

/// <summary>
/// Design-time factory used by the EF Core tools (`dotnet ef migrations` / `database update`).
/// The connection string here is only used at design time; the running app configures the
/// context from configuration in Infrastructure.DependencyInjection.
/// </summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("ConnectionStrings__Default")
            ?? "Server=(localdb)\\mssqllocaldb;Database=KitchenwareBot;Trusted_Connection=True;TrustServerCertificate=True";

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(connection)
            .Options;

        return new AppDbContext(options);
    }
}
