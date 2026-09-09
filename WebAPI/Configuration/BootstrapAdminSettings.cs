using System.ComponentModel.DataAnnotations;

namespace WebAPI.Configuration;

public sealed class BootstrapAdminSettings
{
    public bool Enabled { get; init; }
    public string Username { get; init; } = "";
    public string Email { get; init; } = "";
    public string Password { get; init; } = "";

    public static BootstrapAdminSettings Load(IConfiguration configuration)
    {
        var settings = configuration.GetSection("BootstrapAdmin").Get<BootstrapAdminSettings>() ?? new();
        if (settings.Enabled && (string.IsNullOrWhiteSpace(settings.Username) || settings.Username.Length > 50
            || !new EmailAddressAttribute().IsValid(settings.Email) || settings.Email.Length > 100
            || string.IsNullOrWhiteSpace(settings.Password) || settings.Password.Length < 12
            || settings.Password.Contains("REPLACE", StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("Enabled admin bootstrap requires BootstrapAdmin:Username, a valid Email, and a unique Password of at least 12 characters.");
        return settings;
    }
}
