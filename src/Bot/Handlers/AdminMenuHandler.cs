using KitchenwareBot.Application.Messages;
using KitchenwareBot.Application.Sessions;
using KitchenwareBot.Bot.Common;
using KitchenwareBot.Bot.Keyboards;

namespace KitchenwareBot.Bot.Handlers;

public class AdminMenuHandler : HandlerBase
{
    public AdminMenuHandler(BotResponder bot) : base(bot) { }

    public async Task ShowMenuAsync(BotUpdateContext ctx, CancellationToken ct)
    {
        ctx.Session.ResetToIdle();
        ctx.Session.State = BotState.AdminMenu;
        await Answer(ctx, ct: ct);
        await Show(ctx, BotMessages.AdminWelcome, AdminKeyboards.AdminMain(), ct);
    }
}
