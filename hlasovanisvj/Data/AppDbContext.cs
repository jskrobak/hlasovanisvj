using System.Text.RegularExpressions;
using AngleSharp.Html.Parser;
using hlasovanisvj.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace hlasovanisvj.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Member> Members { get; set; }
    public DbSet<Resolution> Resolutions { get; set; }
    public DbSet<Vote> Votes { get; set; }
    public DbSet<Organization> Organizations { get; set; }
    public DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(m => m.Email).IsUnique();
            entity.HasOne<Organization>(u=> u.Organization)
                .WithOne(o => o.User)
                .HasForeignKey<Organization>(o => o.UserId).IsRequired();
        });
        
        modelBuilder.Entity<Organization>(entity =>
        {
            entity.HasIndex(o => o.UserId)
                .IsUnique();
            
            entity.HasOne<Resolution>(a => a.OpenedResolution)
                .WithMany()
                .HasForeignKey(a => a.OpenedResolutionId);
            
            entity.HasMany<Member>(o => o.Members)
                .WithOne(m => m.Organization)
                .HasForeignKey(m => m.OrganizationId);
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
                .HasForeignKey(m => m.ProxyId);
        });
    }
}
