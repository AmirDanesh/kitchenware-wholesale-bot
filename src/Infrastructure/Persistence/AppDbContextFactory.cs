using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace KitchenwareBot.Infrastructure.Persistence;

internal sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    // Used only by EF Core CLI tools (dotnet ef migrations add / database update).
    // Connection string matches appsettings.json in the Bot project.
    public AppDbContext CreateDbContext(string[] args)
    {
        const string connectionString =
            "Server=(localdb)\\mssqllocaldb;Database=KitchenwareBot;Trusted_Connection=True;";

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new AppDbContext(options);
    }
}
