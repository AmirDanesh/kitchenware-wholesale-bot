namespace KitchenwareBot.Infrastructure.Persistence;

/// <summary>Fixed identifiers used for deterministic EF Core seed data (HasData).</summary>
public static class SeedData
{
    public static class CategoryIds
    {
        public static readonly Guid Cookware   = Guid.Parse("a0000000-0000-0000-0000-000000000001");
        public static readonly Guid Tableware  = Guid.Parse("a0000000-0000-0000-0000-000000000002");
        public static readonly Guid Appliances = Guid.Parse("a0000000-0000-0000-0000-000000000003");
        public static readonly Guid Storage    = Guid.Parse("a0000000-0000-0000-0000-000000000004");
        public static readonly Guid Utensils   = Guid.Parse("a0000000-0000-0000-0000-000000000005");
    }
}
