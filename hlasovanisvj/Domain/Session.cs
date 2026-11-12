using System.ComponentModel.DataAnnotations;

namespace hlasovanisvj.Domain;

public class Session
{
    [Key]
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public string Name { get; set; }
    
    public int OrganizationId { get; set; }
    public Organization Organization { get; set; }
    
    public Resolution? OpenedResolution { get; set; } = null;
    public int? OpenedResolutionId { get; set; } = null;
    public ICollection<Resolution> Resolutions { get; set; }
    public ICollection<Member> Members { get; set; }
}