namespace KitchenwareBot.Application.Configuration;

/// <summary>
/// Bound from the <c>Telegram</c> config section. The <c>AdminIds</c> list is a failsafe:
/// these Telegram users are always treated as admins regardless of their DB role.
/// </summary>
public class AdminOptions
{
    public long[] AdminIds { get; set; } = Array.Empty<long>();
}
