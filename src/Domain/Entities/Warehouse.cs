namespace KitchenwareBot.Domain.Entities;

public class Warehouse
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = default!;
    public string? Location { get; private set; }
    public bool IsActive { get; private set; }

    public ICollection<InventoryItem> InventoryItems { get; private set; } = new List<InventoryItem>();

    private Warehouse() { }

    public static Warehouse Create(string name, string? location = null)
    {
        return new Warehouse
        {
            Id = Guid.NewGuid(),
            Name = name,
            Location = location,
            IsActive = true
        };
    }

    public void Update(string name, string? location)
    {
        Name = name;
        Location = location;
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}
