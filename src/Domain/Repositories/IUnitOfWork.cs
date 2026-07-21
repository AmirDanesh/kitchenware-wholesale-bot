namespace KitchenwareBot.Domain.Repositories;

/// <summary>Commits changes staged on the repositories, and runs multi-step operations
/// (like placing an order + reserving stock) inside a single transaction.</summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
    Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken ct = default);
}
