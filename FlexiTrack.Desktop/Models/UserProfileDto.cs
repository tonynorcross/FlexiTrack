namespace FlexiTrack.Desktop.Models;

public record UserProfileDto(
    string Id,
    string Email,
    string FirstName,
    string LastName,
    bool IsCompanyAdmin,
    bool IsSystemAdmin,
    decimal? WeeklyHoursTarget,
    string? DefaultStartTime,
    string? CompanyName);

public record ClientsResponse(IEnumerable<string> Clients);
