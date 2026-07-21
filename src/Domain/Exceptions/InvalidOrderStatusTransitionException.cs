using KitchenwareBot.Domain.Enums;

namespace KitchenwareBot.Domain.Exceptions;

public sealed class InvalidOrderStatusTransitionException : DomainException
{
    public OrderStatus From { get; }
    public OrderStatus To { get; }

    public InvalidOrderStatusTransitionException(OrderStatus from, OrderStatus to)
        : base($"Cannot change order status from {from} to {to}.")
    {
        From = from;
        To = to;
    }
}
