using System.ComponentModel.DataAnnotations;

namespace hlasovanisvj.Domain;

public class User
{
    [Key]
    public int Id { get; set; }
    public string Email { get; set; }    
    public string? PasswordHash { get; set; }
    public IList<string> Roles { get; set; } = new List<string>();
    public int OrganizationId { get; set; }
    public Organization Organization { get; set; }
}