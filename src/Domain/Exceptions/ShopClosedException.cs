namespace KitchenwareBot.Domain.Exceptions;

public sealed class ShopClosedException : DomainException
{
    public ShopClosedException()
        : base("The shop is closed: no payment methods are enabled.") { }
}
