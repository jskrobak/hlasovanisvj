using System.ComponentModel.DataAnnotations;

namespace hlasovanisvj.Domain;

public class Vote
{
    [Key]
    public int Id { get; set; }
    public DateTime Date { get; set; } = DateTime.Now;
    
    public int MemberId { get; set; }
    public Member Member { get; set; }
    
    public int ResolutionId { get; set; }
    public Resolution Resolution { get; set; }
    
    public VoteChoice Choice { get; set; }
}