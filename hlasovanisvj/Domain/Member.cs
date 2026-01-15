using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace hlasovanisvj.Domain;

public class Member
{
    [Key]
    public int Id { get; set; }
    
    public Guid GlobalId { get; set; } = Guid.NewGuid();
    
    public string Name { get; set; }
    public List<string> Units { get; set; } = [];
    public List<string> Addresses { get; set; } = [];
    public bool IsPresent { get; set; }
    public double ShareValue { get; set; }
    public string ShareFraction { get; set; }

    public ICollection<Vote> Votes { get; set; }
    public ICollection<Member> Principals { get; set; } = []; //Ti, co udělili plnou moc
    public Member? Proxy { get; set; } //Ten, kdo má plnou moc
    public int? ProxyId { get; set; }

    public int OrganizationId { get; set; }
    public Organization Organization { get; set; }
}