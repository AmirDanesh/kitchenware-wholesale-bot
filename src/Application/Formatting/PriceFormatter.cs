using System.Globalization;
using System.Text;

namespace KitchenwareBot.Application.Formatting;

/// <summary>
/// Persian-aware formatting/parsing helpers. All money is displayed in Toman with Persian digits
/// and the Arabic thousands separator, e.g. 1500000 → "۱٬۵۰۰٬۰۰۰ تومان".
/// </summary>
public static class PriceFormatter
{
    private static readonly char[] PersianDigits = { '۰', '۱', '۲', '۳', '۴', '۵', '۶', '۷', '۸', '۹' };
    private const char ThousandsSeparator = '٬'; // U+066C Arabic thousands separator
    private const char DecimalSeparator = '٫';   // U+066B Arabic decimal separator

    public static string ToPersianDigits(string input)
    {
        if (string.IsNullOrEmpty(input)) return input ?? string.Empty;
        var sb = new StringBuilder(input.Length);
        foreach (var ch in input)
            sb.Append(ch is >= '0' and <= '9' ? PersianDigits[ch - '0'] : ch);
        return sb.ToString();
    }

    /// <summary>Formats an amount as Toman: "۱٬۵۰۰٬۰۰۰ تومان" (rounded to whole Toman).</summary>
    public static string FormatToman(decimal amount)
    {
        var value = Math.Round(amount, 0, MidpointRounding.AwayFromZero);
        var grouped = value.ToString("#,##0", CultureInfo.InvariantCulture).Replace(',', ThousandsSeparator);
        return $"{ToPersianDigits(grouped)} تومان";
    }

    /// <summary>Formats a percent: 15 → "۱۵٪", 12.5 → "۱۲٫۵٪".</summary>
    public static string FormatPercent(decimal percent)
    {
        var s = percent % 1 == 0
            ? ((long)percent).ToString(CultureInfo.InvariantCulture)
            : percent.ToString("0.##", CultureInfo.InvariantCulture).Replace('.', DecimalSeparator);
        return $"{ToPersianDigits(s)}٪";
    }

    /// <summary>Formats a plain integer with Persian digits (no unit), e.g. 20 → "۲۰".</summary>
    public static string FormatNumber(long value)
        => ToPersianDigits(value.ToString(CultureInfo.InvariantCulture));

    // ── Parsing (admin may type Persian, Arabic or Latin digits) ──────────────
    public static string ToLatinDigits(string input)
    {
        if (string.IsNullOrEmpty(input)) return input ?? string.Empty;
        var sb = new StringBuilder(input.Length);
        foreach (var ch in input)
        {
            sb.Append(ch switch
            {
                >= '۰' and <= '۹' => (char)('0' + (ch - '۰')), // Persian
                >= '٠' and <= '٩' => (char)('0' + (ch - '٠')), // Arabic-Indic
                _ => ch
            });
        }
        return sb.ToString();
    }

    /// <summary>Parses a positive integer, tolerating Persian/Arabic digits and thousands separators.</summary>
    public static bool TryParseInt(string? input, out int value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(input)) return false;
        var normalized = ToLatinDigits(input).Trim()
            .Replace(",", "").Replace("٬", "").Replace(" ", "").Replace("،", "");
        return int.TryParse(normalized, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
    }

    /// <summary>Parses a non-negative decimal, tolerating Persian/Arabic digits and separators.</summary>
    public static bool TryParseDecimal(string? input, out decimal value)
    {
        value = 0m;
        if (string.IsNullOrWhiteSpace(input)) return false;
        var normalized = ToLatinDigits(input).Trim()
            .Replace(",", "").Replace("٬", "").Replace(" ", "").Replace("،", "")
            .Replace('٫', '.');
        return decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
    }
}
