using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace hlasovanisvj.Data;

public class AppDesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

        var configuration = new ConfigurationBuilder()
            .SetBasePath(Path.GetDirectoryName(this.GetType().Assembly.Location))
            .AddJsonFile("appSettings.json")
            .AddJsonFile($"appSettings.{environment}.json", true)
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection");

        Console.WriteLine($"Connection string: {connectionString}");
        var builder = new DbContextOptionsBuilder<AppDbContext>();
        
        Console.WriteLine(Path.GetFullPath("Data/app.db"));

        builder.UseSqlite(connectionString);
        
        return new AppDbContext(builder.Options);
    }

}