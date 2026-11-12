using hlasovanisvj.Domain;
using Microsoft.EntityFrameworkCore;

namespace hlasovanisvj.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Member> Members { get; set; }
    public DbSet<Resolution> Resolutions { get; set; }
    public DbSet<Vote> Votes { get; set; }
    public DbSet<Organization> Organizations { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Session> Sessions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(m => m.Email).IsUnique();
            entity.HasIndex(m => m.GoogleSubject).IsUnique();
            entity.HasOne<Organization>(u=> u.Organization)
                .WithOne(o => o.User)
                .HasForeignKey<Organization>(o => o.UserId).IsRequired();
        });
        
        modelBuilder.Entity<Session>(entity =>
        {
            entity.HasOne<Resolution>(a => a.OpenedResolution)
                .WithMany()
                .HasForeignKey(a => a.OpenedResolutionId)
                .OnDelete(DeleteBehavior.SetNull);
            
            entity.HasMany<Member>(o => o.Members)
                .WithOne(m => m.Session)
                .HasForeignKey(m => m.SessionId);
            
            entity.HasMany<Resolution>(a => a.Resolutions)
                .WithOne(r => r.Session)
                .HasForeignKey(a => a.SessionId);
        });
        
        modelBuilder.Entity<Organization>(entity =>
        {
            entity.HasIndex(o => o.UserId)
                .IsUnique();
          
            entity.HasOne<Session>(a => a.CurrentSession)
                .WithMany()
                .HasForeignKey(a => a.CurrentSessionId)
                .OnDelete(DeleteBehavior.SetNull);
            
            entity.HasMany<Session>(o => o.Sessions)
                .WithOne(s => s.Organization)
                .HasForeignKey(s => s.OrganizationId);
        });

        modelBuilder.Entity<Resolution>()
            .HasMany<Vote>(v => v.Votes)
            .WithOne(c => c.Resolution)
            .HasForeignKey(s => s.ResolutionId);

        modelBuilder.Entity<Member>(entity =>
        {
            entity.HasIndex(m => m.GlobalId).IsUnique();

            entity.HasMany<Vote>(v => v.Votes)
                .WithOne(c => c.Member)
                .HasForeignKey(v => v.MemberId);

            entity.HasMany<Member>(m => m.Principals)
                .WithOne(c => c.Proxy)
                .HasForeignKey(m => m.ProxyId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }
}

public static class AppDbContextExtensions
{
    /// <summary>
    /// Vyhodí výjimku, pokud schůze neexistuje nebo je uzavřená (jen pro čtení).
    /// </summary>
    public static async Task EnsureSessionOpenAsync(this AppDbContext dbContext, int sessionId)
    {
        var closedAt = await dbContext.Sessions
            .Where(s => s.Id == sessionId)
            .Select(s => new { s.ClosedAt })
            .FirstOrDefaultAsync()
            ?? throw new InvalidOperationException("Schůze nenalezena.");

        if (closedAt.ClosedAt != null)
            throw new InvalidOperationException("Schůze je uzavřená, lze ji pouze prohlížet.");
    }
}
