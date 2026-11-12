using System.ComponentModel.DataAnnotations.Schema;

namespace hlasovanisvj.Domain;

public class Resolution
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Question { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public int SessionId { get; set; }
    public Session Session { get; set; } = null!;
    
    public ICollection<Vote> Votes { get; set; } = [];

    // --- Výsledek zafixovaný při uzavření hlasování (podíly v % celého domu) ---
    public DateTime? ClosedAt { get; set; }
    public double? PresentShare { get; set; }
    public double? InFavorShare { get; set; }
    public double? AgainstShare { get; set; }
    public double? AbstainedShare { get; set; }

    [NotMapped]
    public bool HasResult => ClosedAt != null && PresentShare > 0;

    /// <summary>Podíl hlasů v % z přítomných.</summary>
    public double PercentOfPresent(double? share) =>
        PresentShare > 0 ? (share ?? 0) / PresentShare.Value * 100.0 : 0;
}
