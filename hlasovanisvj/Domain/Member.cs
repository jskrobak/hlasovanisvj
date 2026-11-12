using System.ComponentModel.DataAnnotations;

namespace hlasovanisvj.Domain;

public class Member
{
    [Key]
    public int Id { get; set; }
    
    public Guid GlobalId { get; set; } = Guid.NewGuid();
    
    public string Name { get; set; } = string.Empty;
    public List<string> Units { get; set; } = [];
    public List<string> Addresses { get; set; } = [];
    public bool IsPresent { get; set; }
    public double ShareValue { get; set; }
    public string ShareFraction { get; set; } = string.Empty;

    /// <summary>
    /// Host – není členem SVJ (nulový podíl), na schůzi zastupuje členy na základě plné moci.
    /// </summary>
    public bool IsGuest { get; set; }

    public ICollection<Vote> Votes { get; set; } = [];
    public ICollection<Member> Principals { get; set; } = []; //Ti, co udělili plnou moc
    public Member? Proxy { get; set; } //Ten, kdo má plnou moc
    public int? ProxyId { get; set; }

    public int SessionId { get; set; }
    public Session Session { get; set; } = null!;
}
