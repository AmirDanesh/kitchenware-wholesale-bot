using KitchenwareBot.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace KitchenwareBot.Infrastructure.Persistence;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _db;
    public UnitOfWork(AppDbContext db) => _db = db;

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);

    /// <summary>
    /// Runs <paramref name="action"/> and commits inside a single transaction, honouring the
    /// SqlServer retrying execution strategy. Used for atomic operations such as placing an
    /// order and reserving its stock.
    /// </summary>
    public async Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken ct = default)
    {
        if (IsInMemory())
        {
            // EF InMemory has no transaction support. Unsaved tracked changes disappear with
            // the failed request scope, which is sufficient for local Debug workflows.
            await action(ct);
            await _db.SaveChangesAsync(ct);
            return;
        }

        var strategy = _db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _db.Database.BeginTransactionAsync(ct);
            try
            {
                await action(ct);
                await _db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw;
            }
        });
    }

    private bool IsInMemory()
        => string.Equals(_db.Database.ProviderName, "Microsoft.EntityFrameworkCore.InMemory",
            StringComparison.Ordinal);
}
