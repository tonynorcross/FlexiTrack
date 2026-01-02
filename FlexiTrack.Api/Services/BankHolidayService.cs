namespace FlexiTrack.Api.Services;

public interface IBankHolidayService
{
    bool IsBankHoliday(DateOnly date, string region);
    string? GetHolidayName(DateOnly date, string region);
}

public class BankHolidayService : IBankHolidayService
{
    private static readonly Dictionary<string, List<(DateOnly Date, string Name)>> Holidays = new()
    {
        ["UK"] = new List<(DateOnly, string)>
        {
            // 2025 UK Bank Holidays (England & Wales)
            (new DateOnly(2025, 1, 1), "New Year's Day"),
            (new DateOnly(2025, 4, 18), "Good Friday"),
            (new DateOnly(2025, 4, 21), "Easter Monday"),
            (new DateOnly(2025, 5, 5), "Early May Bank Holiday"),
            (new DateOnly(2025, 5, 26), "Spring Bank Holiday"),
            (new DateOnly(2025, 8, 25), "Summer Bank Holiday"),
            (new DateOnly(2025, 12, 25), "Christmas Day"),
            (new DateOnly(2025, 12, 26), "Boxing Day"),

            // 2026 UK Bank Holidays (England & Wales)
            (new DateOnly(2026, 1, 1), "New Year's Day"),
            (new DateOnly(2026, 4, 3), "Good Friday"),
            (new DateOnly(2026, 4, 6), "Easter Monday"),
            (new DateOnly(2026, 5, 4), "Early May Bank Holiday"),
            (new DateOnly(2026, 5, 25), "Spring Bank Holiday"),
            (new DateOnly(2026, 8, 31), "Summer Bank Holiday"),
            (new DateOnly(2026, 12, 25), "Christmas Day"),
            (new DateOnly(2026, 12, 28), "Boxing Day (substitute)"),
        },
        ["US"] = new List<(DateOnly, string)>
        {
            // 2025 US Federal Holidays
            (new DateOnly(2025, 1, 1), "New Year's Day"),
            (new DateOnly(2025, 1, 20), "Martin Luther King Jr. Day"),
            (new DateOnly(2025, 2, 17), "Presidents' Day"),
            (new DateOnly(2025, 5, 26), "Memorial Day"),
            (new DateOnly(2025, 6, 19), "Juneteenth"),
            (new DateOnly(2025, 7, 4), "Independence Day"),
            (new DateOnly(2025, 9, 1), "Labor Day"),
            (new DateOnly(2025, 10, 13), "Columbus Day"),
            (new DateOnly(2025, 11, 11), "Veterans Day"),
            (new DateOnly(2025, 11, 27), "Thanksgiving Day"),
            (new DateOnly(2025, 12, 25), "Christmas Day"),

            // 2026 US Federal Holidays
            (new DateOnly(2026, 1, 1), "New Year's Day"),
            (new DateOnly(2026, 1, 19), "Martin Luther King Jr. Day"),
            (new DateOnly(2026, 2, 16), "Presidents' Day"),
            (new DateOnly(2026, 5, 25), "Memorial Day"),
            (new DateOnly(2026, 6, 19), "Juneteenth"),
            (new DateOnly(2026, 7, 3), "Independence Day (observed)"),
            (new DateOnly(2026, 9, 7), "Labor Day"),
            (new DateOnly(2026, 10, 12), "Columbus Day"),
            (new DateOnly(2026, 11, 11), "Veterans Day"),
            (new DateOnly(2026, 11, 26), "Thanksgiving Day"),
            (new DateOnly(2026, 12, 25), "Christmas Day"),
        }
    };

    public bool IsBankHoliday(DateOnly date, string region)
    {
        if (!Holidays.TryGetValue(region, out var holidays))
            return false;

        return holidays.Any(h => h.Date == date);
    }

    public string? GetHolidayName(DateOnly date, string region)
    {
        if (!Holidays.TryGetValue(region, out var holidays))
            return null;

        return holidays.FirstOrDefault(h => h.Date == date).Name;
    }
}
