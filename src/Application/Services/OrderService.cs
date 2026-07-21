using KitchenwareBot.Application.Abstractions;
using KitchenwareBot.Application.DTOs;
using KitchenwareBot.Application.Sessions;
using KitchenwareBot.Domain.Common;
using KitchenwareBot.Domain.Entities;
using KitchenwareBot.Domain.Enums;
using KitchenwareBot.Domain.Exceptions;
using KitchenwareBot.Domain.Repositories;

namespace KitchenwareBot.Application.Services;

public class OrderService : IOrderService
{
    private readonly IOrderRepository _orders;
    private readonly IProductRepository _products;
    private readonly IInventoryRepository _inventory;
    private readonly IDiscountRepository _discounts;
    private readonly IPaymentSettingsRepository _payment;
    private readonly IWarehouseRepository _warehouses;
    private readonly IUnitOfWork _uow;
    private readonly INotificationService _notifier;

    public OrderService(
        IOrderRepository orders,
        IProductRepository products,
        IInventoryRepository inventory,
        IDiscountRepository discounts,
        IPaymentSettingsRepository payment,
        IWarehouseRepository warehouses,
        IUnitOfWork uow,
        INotificationService notifier)
    {
        _orders = orders;
        _products = products;
        _inventory = inventory;
        _discounts = discounts;
        _payment = payment;
        _warehouses = warehouses;
        _uow = uow;
        _notifier = notifier;
    }

    public async Task<OrderCalculationDto> CalculateOrderAsync(IReadOnlyList<CartItem> cart, CancellationToken ct = default)
    {
        var lines = new List<OrderLineDto>(cart.Count);
        foreach (var item in cart)
        {
            var product = await _products.GetByIdAsync(item.ProductId, ct);
            if (product is null || !product.IsActive)
                throw new ProductUnavailableException(item.ProductId, product?.Name ?? item.Name);

            var percent = await _discounts.ResolveDiscountAsync(item.ProductId, item.Quantity, ct);
            var finalUnit = Math.Round(product.Price * (1 - percent / 100m), 2, MidpointRounding.AwayFromZero);
            var lineTotal = finalUnit * item.Quantity;
            var saved = (product.Price - finalUnit) * item.Quantity;
            lines.Add(new OrderLineDto(item.ProductId, product.Name, item.Quantity,
                product.Price, percent, finalUnit, lineTotal, saved));
        }

        return new OrderCalculationDto(
            lines,
            lines.Sum(l => l.LineTotal),
            lines.Sum(l => l.Saved),
            cart.Sum(c => c.Quantity));
    }

    public async Task<PlaceOrderResultDto> PlaceOrderAsync(long customerTelegramId, string customerName, string? customerPhone,
        IReadOnlyList<CartItem> cart, OrderDraft draft, CancellationToken ct = default)
    {
        if (cart is null || cart.Count == 0)
            throw new InvalidOperationException("Cart is empty.");

        var settings = await _payment.GetAsync(ct);
        if (!settings.IsShopOpen)
            throw new ShopClosedException();

        var payment = draft.Payment ?? throw new InvalidOperationException("Payment method not selected.");
        if (payment == PaymentMethod.BankTransfer && !settings.BankTransferEnabled) throw new ShopClosedException();
        if (payment == PaymentMethod.Cash && !settings.CashEnabled) throw new ShopClosedException();

        var delivery = draft.Delivery ?? throw new InvalidOperationException("Delivery type not selected.");
        var address = delivery == DeliveryType.Shipping ? draft.Address : null;
        if (delivery == DeliveryType.Shipping && string.IsNullOrWhiteSpace(address))
            throw new InvalidOperationException("Shipping address is required.");

        Order order = null!;
        var lowStockItems = new List<LowStockItemDto>();
        await _uow.ExecuteInTransactionAsync(async innerCt =>
        {
            lowStockItems.Clear();
            order = Order.Create(customerTelegramId, customerName, customerPhone, payment, delivery, address);

            // Deterministic lock order prevents two multi-product checkouts from deadlocking.
            foreach (var item in cart.OrderBy(i => i.ProductId))
            {
                var product = await _products.GetByIdAsync(item.ProductId, innerCt);
                if (product is null || !product.IsActive)
                    throw new ProductUnavailableException(item.ProductId, product?.Name ?? item.Name);

                var percent = await _discounts.ResolveDiscountAsync(product.Id, item.Quantity, innerCt);
                // Snapshots taken from the CURRENT product price (prices lock at order time).
                order.AddItem(product.Id, product.Name, product.Price, percent, item.Quantity);
                var newlyLowStock = await _inventory.ReserveAsync(product.Id, item.Quantity, innerCt);
                lowStockItems.AddRange(newlyLowStock.Select(stock => new LowStockItemDto(
                    stock.ProductId,
                    stock.Product?.Name ?? product.Name,
                    stock.Warehouse?.Name ?? string.Empty,
                    stock.AvailableQuantity,
                    stock.LowStockThreshold)));
            }

            order.RecalculateTotal();
            await _orders.AddAsync(order, innerCt);
        }, ct);

        // Notify admins after the transaction commits (never call Telegram inside a DB tx).
        await _notifier.NotifyAdminsNewOrderAsync(order, ct);
        foreach (var item in lowStockItems)
            await _notifier.NotifyAdminsLowStockAsync(item, ct);

        BankDetailsDto? bank = payment == PaymentMethod.BankTransfer
            ? new BankDetailsDto(settings.BankName, settings.BankAccountNumber, settings.BankAccountName, settings.BankNote)
            : null;

        return new PlaceOrderResultDto(order.Id, order.ShortCode, order.TotalAmount, payment, delivery, bank);
    }

    public Task<PagedResult<Order>> GetCustomerOrdersAsync(long customerTelegramId, int page, int pageSize, CancellationToken ct = default)
        => _orders.GetByCustomerAsync(customerTelegramId, page, pageSize, ct);

    public Task<PagedResult<Order>> GetAllOrdersAsync(OrderStatus? status, int page, int pageSize, CancellationToken ct = default)
        => _orders.GetAllAsync(status, page, pageSize, ct);

    public Task<Order?> GetOrderAsync(Guid orderId, CancellationToken ct = default)
        => _orders.GetByIdAsync(orderId, ct);

    public async Task UpdateOrderStatusAsync(Guid orderId, OrderStatus status, string? note, CancellationToken ct = default)
    {
        if (status == OrderStatus.Cancelled)
        {
            await CancelOrderAsync(orderId, note, ct);
            return;
        }

        var order = await _orders.GetByIdAsync(orderId, ct)
                    ?? throw new EntityNotFoundException(nameof(Order), orderId);

        if (status == OrderStatus.Confirmed && order.Status == OrderStatus.Pending)
        {
            // Confirming consumes the reserved stock.
            await _uow.ExecuteInTransactionAsync(async innerCt =>
            {
                order.UpdateStatus(status, note);
                foreach (var item in order.Items)
                    await _inventory.ConsumeAsync(item.ProductId, item.Quantity, innerCt);
            }, ct);
        }
        else
        {
            order.UpdateStatus(status, note); // validates the transition
            await _uow.SaveChangesAsync(ct);
        }

        await _notifier.NotifyCustomerOrderStatusAsync(order, ct);
    }

    public async Task CancelOrderAsync(Guid orderId, string? note, CancellationToken ct = default)
    {
        var order = await _orders.GetByIdAsync(orderId, ct)
                    ?? throw new EntityNotFoundException(nameof(Order), orderId);

        if (order.Status == OrderStatus.Cancelled) return; // idempotent

        // Pending orders only hold a reservation; confirmed+ orders already consumed stock.
        var wasConsumed = order.Status != OrderStatus.Pending;

        await _uow.ExecuteInTransactionAsync(async innerCt =>
        {
            order.UpdateStatus(OrderStatus.Cancelled, note); // throws if Delivered (terminal)
            foreach (var item in order.Items)
            {
                if (wasConsumed)
                    await RestockAsync(item.ProductId, item.Quantity, innerCt);
                else
                    await _inventory.ReleaseAsync(item.ProductId, item.Quantity, innerCt);
            }
        }, ct);

        await _notifier.NotifyCustomerOrderStatusAsync(order, ct);
    }

    private async Task RestockAsync(Guid productId, int qty, CancellationToken ct)
    {
        var items = await _inventory.GetByProductAsync(productId, ct);
        if (items.Count > 0)
        {
            items[0].Adjust(qty);
            return;
        }

        // No inventory row survived — recreate one in the default warehouse.
        var warehouse = await _warehouses.GetDefaultAsync(ct);
        if (warehouse is null) return;
        await _inventory.AddAsync(InventoryItem.Create(productId, warehouse.Id, qty), ct);
    }
}
