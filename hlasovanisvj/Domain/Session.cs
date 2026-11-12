using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace hlasovanisvj.Domain;

public class Session
{
    [Key]
    public int Id { get; set; }
    public DateOnly Date { get; set; }
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Kdy byla schůze uzavřena. Uzavřenou schůzi lze jen prohlížet
    /// (nelze otevírat hlasování, měnit účast, účastníky ani usnesení).
    /// </summary>
    public DateTime? ClosedAt { get; set; }

    [NotMapped]
    public bool IsClosed => ClosedAt != null;
    
    public int OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;
    
    public Resolution? OpenedResolution { get; set; }
    public int? OpenedResolutionId { get; set; }
    public ICollection<Resolution> Resolutions { get; set; } = [];
    public ICollection<Member> Members { get; set; } = [];
}
