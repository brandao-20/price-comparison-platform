using Npgsql;

namespace WebAPI.Configuration;

public static class ApplicationSettings
{
    public static string GetConnectionString(IConfiguration configuration)
    {
        var value = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(value) || value.Contains("REPLACE", StringComparison.OrdinalIgnoreCase)
            || value.Contains("SUA_SENHA", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("ConnectionStrings:DefaultConnection must contain your PostgreSQL connection settings.");
        try
        {
            var connection = new NpgsqlConnectionStringBuilder(value);
            if (string.IsNullOrWhiteSpace(connection.Host) || string.IsNullOrWhiteSpace(connection.Database))
                throw new ArgumentException();
        }
        catch (ArgumentException)
        {
            throw new InvalidOperationException("ConnectionStrings:DefaultConnection must be a valid PostgreSQL connection string with Host and Database.");
        }
        return value;
    }

    public static Uri GetFrontendUrl(IConfiguration configuration, bool isDevelopment)
    {
        var value = configuration["Frontend:BaseUrl"] ?? (isDevelopment ? "http://localhost:5116" : null);
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || !string.IsNullOrEmpty(uri.UserInfo)
            || uri.AbsolutePath != "/" || !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment)
            || (uri.Scheme != Uri.UriSchemeHttps && !(isDevelopment && uri.IsLoopback && uri.Scheme == Uri.UriSchemeHttp)))
            throw new InvalidOperationException("Frontend:BaseUrl must be an HTTPS origin without a path, query, or credentials. HTTP loopback is allowed in Development.");
        return uri;
    }
}
