using KitchenwareBot.Domain.Common;

namespace KitchenwareBot.Domain.Entities;

/// <summary>Product category. Supports one level of nesting (parent → child).</summary>
public class Category : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public Guid? ParentId { get; private set; }
    public int DisplayOrder { get; private set; }
    public bool IsActive { get; private set; } = true;

    // Navigation
    public Category? Parent { get; private set; }
    public ICollection<Category> Children { get; private set; } = new List<Category>();
    public ICollection<Product> Products { get; private set; } = new List<Product>();

    private Category() { }

    public static Category Create(string name, Guid? parentId = null, int displayOrder = 0)
        => new()
        {
            Name = (name ?? throw new ArgumentNullException(nameof(name))).Trim(),
            ParentId = parentId,
            DisplayOrder = displayOrder,
            IsActive = true
        };

    public void Rename(string name) => Name = name.Trim();
    public void SetParent(Guid? parentId) => ParentId = parentId;
    public void SetDisplayOrder(int order) => DisplayOrder = order;
    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}
