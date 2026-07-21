namespace KitchenwareBot.Infrastructure.Configuration;

public class RedisOptions
{
    public string Connection { get; set; } = "localhost:6379";
    public int SessionTtlMinutes { get; set; } = 30;
}
