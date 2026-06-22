using KitchenwareBot.Domain.Entities;

namespace KitchenwareBot.Domain.Interfaces;

public interface IWarehouseRepository : IRepository<Warehouse>
{
    Task<IReadOnlyList<Warehouse>> GetAllActiveAsync();
}
