namespace KitchenwareBot.Domain.Common;

/// <summary>
/// Base type for all entities. Ids are <see cref="Guid"/> (per architecture rules)
/// and generated up-front so aggregates can wire up relationships before persistence.
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
}
