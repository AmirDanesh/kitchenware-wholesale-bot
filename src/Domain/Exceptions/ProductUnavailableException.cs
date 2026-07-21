namespace KitchenwareBot.Domain.Exceptions;

public sealed class ProductUnavailableException : DomainException
{
    public Guid ProductId { get; }
    public string ProductName { get; }

    public ProductUnavailableException(Guid productId, string productName)
        : base($"Product '{productName}' ({productId}) is not available for ordering.")
    {
        ProductId = productId;
        ProductName = productName;
    }
}
