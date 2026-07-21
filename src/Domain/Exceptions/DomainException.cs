namespace KitchenwareBot.Domain.Exceptions;

/// <summary>
/// Base class for expected, business-rule violations. Handlers translate these into
/// friendly Persian messages; they are never surfaced as raw stack traces to users.
/// </summary>
public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message) { }
}
