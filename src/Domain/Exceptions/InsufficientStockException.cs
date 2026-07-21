namespace KitchenwareBot.Domain.Exceptions;

public sealed class InsufficientStockException : DomainException
{
    public Guid ProductId { get; }
    public string ProductName { get; }
    public int Requested { get; }
    public int Available { get; }

    public InsufficientStockException(Guid productId, string productName, int requested, int available)
        : base($"Insufficient stock for product '{productName}' ({productId}): requested {requested}, available {available}.")
    {
        ProductId = productId;
        ProductName = productName;
        Requested = requested;
        Available = available;
    }
}
