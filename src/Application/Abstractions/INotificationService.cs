using KitchenwareBot.Application.DTOs;
using KitchenwareBot.Domain.Entities;

namespace KitchenwareBot.Application.Abstractions;

/// <summary>
/// Outbound notifications. Implemented in the Bot layer (Telegram) — Application only depends on
/// this abstraction so it never calls the Telegram API directly (architecture rule).
/// Implementations must be resilient: a failed notification must not break the calling operation.
/// </summary>
public interface INotificationService
{
    Task NotifyAdminsNewOrderAsync(Order order, CancellationToken ct = default);
    Task NotifyAdminsLowStockAsync(LowStockItemDto item, CancellationToken ct = default);
    Task NotifyCustomerOrderStatusAsync(Order order, CancellationToken ct = default);
}
