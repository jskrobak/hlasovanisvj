namespace hlasovanisvj.Domain;

public class Resolution
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Question { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsOpen { get; set; }
    
    public int OrganizationId { get; set; }
    public Organization Organization { get; set; }
    
    public ICollection<Vote> Votes { get; set; }
}