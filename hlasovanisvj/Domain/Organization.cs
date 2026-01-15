using System.ComponentModel.DataAnnotations;

namespace hlasovanisvj.Domain;

public class Organization
{
    [Key]
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Resolution? OpenedResolution { get; set; }
    public int? OpenedResolutionId { get; set; }
    public ICollection<Resolution> Resolutions { get; set; }
    public ICollection<Member> Members { get; set; }
    public User User { get; set; }
    public int UserId { get; set; }
}