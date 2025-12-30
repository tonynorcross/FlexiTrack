using Microsoft.AspNetCore.Identity;

namespace FlexiTrack.Api.Data.Entities;

public class ApplicationUser : IdentityUser
{
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public DateTime Created { get; set; } = DateTime.UtcNow;
    public int LoginCount { get; set; }
    public DateTime? LastLogin { get; set; }
    public DateTime? Removed { get; set; }

    public int? CompanyId { get; set; }
    public Company? Company { get; set; }

    public bool IsCompanyAdmin { get; set; }
    public bool IsSystemAdmin { get; set; }

    public decimal? WeeklyHoursTarget { get; set; }
    public TimeOnly? DefaultStartTime { get; set; }
}
