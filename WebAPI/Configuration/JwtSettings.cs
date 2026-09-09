using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace WebAPI.Configuration;

public sealed class JwtSettings
{
    public string Key { get; init; } = "";
    public string Issuer { get; init; } = "";
    public string Audience { get; init; } = "";

    public static JwtSettings Load(IConfiguration configuration)
    {
        var settings = configuration.GetSection("Jwt").Get<JwtSettings>() ?? new();
        if (string.IsNullOrWhiteSpace(settings.Key) || Encoding.UTF8.GetByteCount(settings.Key) < 32
            || settings.Key.Contains("REPLACE", StringComparison.OrdinalIgnoreCase)
            || settings.Key.Contains("INSIRA", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Jwt:Key must be a unique secret of at least 32 UTF-8 bytes. Configure it using user secrets, environment variables, or ignored local settings.");
        if (string.IsNullOrWhiteSpace(settings.Issuer) || string.IsNullOrWhiteSpace(settings.Audience))
            throw new InvalidOperationException("Jwt:Issuer and Jwt:Audience must be configured.");
        return settings;
    }

    public SymmetricSecurityKey SigningKey => new(Encoding.UTF8.GetBytes(Key));

    public TokenValidationParameters ValidationParameters => new()
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = Issuer,
        ValidAudience = Audience,
        IssuerSigningKey = SigningKey,
        ValidAlgorithms = new[] { SecurityAlgorithms.HmacSha256 }
    };
}
