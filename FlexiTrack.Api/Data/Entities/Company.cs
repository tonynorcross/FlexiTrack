namespace FlexiTrack.Api.Data.Entities;

public class Company
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public DateTime Created { get; set; } = DateTime.UtcNow;
    public DateTime? Removed { get; set; }

    public ICollection<ApplicationUser> Users { get; set; } = [];
}
