namespace KitchenwareBot.Domain.Common;

/// <summary>
/// Marks an entity whose <see cref="CreatedAt"/>/<see cref="UpdatedAt"/> stamps are
/// maintained centrally by the AppDbContext SaveChanges override.
/// </summary>
public interface IAuditable
{
    DateTime CreatedAt { get; }
    DateTime UpdatedAt { get; }

    void OnCreated(DateTime utcNow);
    void OnUpdated(DateTime utcNow);
}
