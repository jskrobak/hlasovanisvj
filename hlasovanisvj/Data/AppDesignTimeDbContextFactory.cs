using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace hlasovanisvj.Data;

/// <summary>
/// Pro `dotnet ef`. Connection string bere z appsettings, user-secrets
/// nebo proměnné prostředí ConnectionStrings__DefaultConnection.
/// </summary>
public class AppDesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

        var configuration = new ConfigurationBuilder()
            .SetBasePath(Path.GetDirectoryName(GetType().Assembly.Location)!)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .AddUserSecrets<AppDesignTimeDbContextFactory>(optional: true)
            .AddEnvironmentVariables()
            .Build();

        // Pro "migrations add" se k databázi nepřipojuje, stačí platný formát.
        var connectionString = configuration.GetConnectionString("DefaultConnection")
                               ?? "Host=localhost;Database=hlasovanisvj";

        var builder = new DbContextOptionsBuilder<AppDbContext>();
        builder.UseNpgsql(connectionString);
        
        return new AppDbContext(builder.Options);
    }
}
