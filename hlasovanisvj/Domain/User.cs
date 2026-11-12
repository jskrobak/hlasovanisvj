using System.ComponentModel.DataAnnotations;

namespace hlasovanisvj.Domain;

public class User
{
    [Key]
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? PasswordHash { get; set; }
    public IList<string> Roles { get; set; } = new List<string>();
    public int OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;

    /// <summary>Identifikátor účtu Google ("sub"), pokud se uživatel přihlašuje přes Google.</summary>
    public string? GoogleSubject { get; set; }

    /// <summary>SHA-256 hash jednorázového tokenu pro reset hesla.</summary>
    public string? PasswordResetTokenHash { get; set; }
    public DateTime? PasswordResetExpiresAt { get; set; }
}
