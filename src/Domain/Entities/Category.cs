namespace KitchenwareBot.Domain.Entities;

public class Category
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = default!;
    public Guid? ParentId { get; private set; }
    public int DisplayOrder { get; private set; }
    public bool IsActive { get; private set; }

    public Category? Parent { get; private set; }
    public ICollection<Category> Children { get; private set; } = new List<Category>();
    public ICollection<Product> Products { get; private set; } = new List<Product>();

    private Category() { }

    public static Category Create(string name, Guid? parentId, int displayOrder)
    {
        return new Category
        {
            Id = Guid.NewGuid(),
            Name = name,
            ParentId = parentId,
            DisplayOrder = displayOrder,
            IsActive = true
        };
    }

    public void Update(string name, Guid? parentId, int displayOrder)
    {
        Name = name;
        ParentId = parentId;
        DisplayOrder = displayOrder;
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}
