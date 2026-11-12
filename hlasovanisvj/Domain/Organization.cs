using System.ComponentModel.DataAnnotations;

namespace hlasovanisvj.Domain;

public class Organization
{
    [Key]
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public User User { get; set; }
    public int UserId { get; set; }
    
    public ICollection<Session> Sessions { get; set; }
    
    public Session? CurrentSession { get; set; } = null;
    public int? CurrentSessionId { get; set; } = null;
}