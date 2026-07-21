using KitchenwareBot.Domain.Common;

namespace KitchenwareBot.Domain.Entities;

public class Warehouse : BaseEntity
{
    /// <summary>Well-known Id of the default warehouse seeded on first migration.</summary>
    public static readonly Guid DefaultId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    public string Name { get; private set; } = string.Empty;
    public string? Location { get; private set; }
    public bool IsActive { get; private set; } = true;

    // Navigation
    public ICollection<InventoryItem> InventoryItems { get; private set; } = new List<InventoryItem>();

    private Warehouse() { }

    public static Warehouse Create(string name, string? location = null)
        => new()
        {
            Name = (name ?? throw new ArgumentNullException(nameof(name))).Trim(),
            Location = location,
            IsActive = true
        };

    public void Update(string name, string? location)
    {
        Name = name.Trim();
        Location = location;
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}
