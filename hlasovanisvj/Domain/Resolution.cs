namespace hlasovanisvj.Domain;

public class Resolution
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Question { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    //public bool IsOpen { get; set; }
    
    public int SessionId { get; set; }
    public Session Session { get; set; }
    
    public ICollection<Vote> Votes { get; set; }
}