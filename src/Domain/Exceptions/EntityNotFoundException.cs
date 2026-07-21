namespace KitchenwareBot.Domain.Exceptions;

public sealed class EntityNotFoundException : DomainException
{
    public EntityNotFoundException(string entity, object key)
        : base($"{entity} with key '{key}' was not found.") { }
}
