using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using WebAPI.Configuration;

namespace WebAPI.Context;

// EF tooling needs database configuration, but must not run application bootstrap or require OAuth/JWT secrets.
public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var root = Directory.GetCurrentDirectory();
        if (!File.Exists(Path.Combine(root, "WebAPI.csproj")))
            root = Path.Combine(root, "WebAPI");
        var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
            ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
        var configuration = new ConfigurationBuilder()
            .SetBasePath(root)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .AddUserSecrets<AppDbContextFactory>(optional: true)
            .AddEnvironmentVariables()
            .AddCommandLine(args)
            .Build();
        return new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(ApplicationSettings.GetConnectionString(configuration)).Options);
    }
}
