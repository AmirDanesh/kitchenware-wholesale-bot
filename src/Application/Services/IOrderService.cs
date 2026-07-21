using KitchenwareBot.Application.DTOs;
using KitchenwareBot.Application.Sessions;
using KitchenwareBot.Domain.Common;
using KitchenwareBot.Domain.Entities;
using KitchenwareBot.Domain.Enums;

namespace KitchenwareBot.Application.Services;

public interface IOrderService
{
    Task<OrderCalculationDto> CalculateOrderAsync(IReadOnlyList<CartItem> cart, CancellationToken ct = default);
    Task<PlaceOrderResultDto> PlaceOrderAsync(long customerTelegramId, string customerName, string? customerPhone,
        IReadOnlyList<CartItem> cart, OrderDraft draft, CancellationToken ct = default);
    Task<PagedResult<Order>> GetCustomerOrdersAsync(long customerTelegramId, int page, int pageSize, CancellationToken ct = default);
    Task<PagedResult<Order>> GetAllOrdersAsync(OrderStatus? status, int page, int pageSize, CancellationToken ct = default);
    Task<Order?> GetOrderAsync(Guid orderId, CancellationToken ct = default);
    Task UpdateOrderStatusAsync(Guid orderId, OrderStatus status, string? note, CancellationToken ct = default);
    Task CancelOrderAsync(Guid orderId, string? note, CancellationToken ct = default);
}
