using System.Globalization;

namespace KitchenwareBot.Application.Formatting;

/// <summary>Formats timestamps in the Persian (Jalali) calendar with Persian digits.
/// UTC input is shifted to Iran Standard Time (UTC+03:30) before conversion.</summary>
public static class PersianDate
{
    private static readonly PersianCalendar Calendar = new();
    private static readonly TimeSpan IranOffset = TimeSpan.FromMinutes(210); // +03:30

    public static string Format(DateTime utc)
    {
        var local = DateTime.SpecifyKind(utc, DateTimeKind.Utc).Add(IranOffset);
        var text = $"{Calendar.GetYear(local):0000}/{Calendar.GetMonth(local):00}/{Calendar.GetDayOfMonth(local):00}" +
                   $" {local.Hour:00}:{local.Minute:00}";
        return PriceFormatter.ToPersianDigits(text);
    }
}
