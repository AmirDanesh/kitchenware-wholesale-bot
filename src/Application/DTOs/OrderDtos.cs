using KitchenwareBot.Domain.Enums;

namespace KitchenwareBot.Application.DTOs;

public record OrderLineDto(
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal OriginalUnitPrice,
    decimal DiscountPercent,
    decimal FinalUnitPrice,
    decimal LineTotal,
    decimal Saved);

/// <summary>Itemized order calculation with discounts applied (used in cart + checkout preview).</summary>
public record OrderCalculationDto(
    IReadOnlyList<OrderLineDto> Lines,
    decimal GrandTotal,
    decimal TotalSaved,
    int TotalItems);

public record PlaceOrderResultDto(
    Guid OrderId,
    string ShortCode,
    decimal Total,
    PaymentMethod PaymentMethod,
    DeliveryType DeliveryType,
    BankDetailsDto? BankDetails);
