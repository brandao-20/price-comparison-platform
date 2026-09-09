using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using WebAPI.Configuration;

namespace WebAPI.UnitTests.Security;

public class ConfigurationTests
{
    private static IConfiguration Configuration(params (string Key, string? Value)[] values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values.ToDictionary(v => v.Key, v => v.Value)).Build();

    [TestCase(null)]
    [TestCase("")]
    [TestCase("too-short")]
    [TestCase("REPLACE_WITH_A_RANDOM_SECRET_OF_AT_LEAST_32_BYTES")]
    public void JwtSettings_RejectMissingWeakOrExampleKeys(string? key)
    {
        var exception = Assert.Throws<InvalidOperationException>(() => JwtSettings.Load(Configuration(
            ("Jwt:Key", key), ("Jwt:Issuer", "test-issuer"), ("Jwt:Audience", "test-client"))));
        if (!string.IsNullOrEmpty(key)) Assert.That(exception!.Message, Does.Not.Contain(key));
    }

    [Test]
    public void JwtValidation_RejectsWrongIssuerAudienceSignatureAndExpiredTokens()
    {
        var settings = JwtSettings.Load(Configuration(("Jwt:Key", Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))),
            ("Jwt:Issuer", "test-issuer"), ("Jwt:Audience", "test-client")));
        var handler = new JwtSecurityTokenHandler();
        string Token(string issuer, string audience, DateTime expires, SecurityKey key) => handler.WriteToken(
            new JwtSecurityToken(issuer, audience, expires: expires,
                signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)));
        Assert.DoesNotThrow(() => handler.ValidateToken(Token(settings.Issuer, settings.Audience, DateTime.UtcNow.AddMinutes(5), settings.SigningKey), settings.ValidationParameters, out _));
        Assert.Throws<SecurityTokenInvalidIssuerException>(() => handler.ValidateToken(Token("wrong", settings.Audience, DateTime.UtcNow.AddMinutes(5), settings.SigningKey), settings.ValidationParameters, out _));
        Assert.Throws<SecurityTokenInvalidAudienceException>(() => handler.ValidateToken(Token(settings.Issuer, "wrong", DateTime.UtcNow.AddMinutes(5), settings.SigningKey), settings.ValidationParameters, out _));
        Assert.Throws<SecurityTokenExpiredException>(() => handler.ValidateToken(Token(settings.Issuer, settings.Audience, DateTime.UtcNow.AddMinutes(-10), settings.SigningKey), settings.ValidationParameters, out _));
        Assert.Catch<SecurityTokenException>(() => handler.ValidateToken(Token(settings.Issuer, settings.Audience, DateTime.UtcNow.AddMinutes(5), new SymmetricSecurityKey(RandomNumberGenerator.GetBytes(32))), settings.ValidationParameters, out _));
    }

    [Test]
    public void Bootstrap_IsDisabledByDefaultAndRequiresExplicitCredentialsWhenEnabled()
    {
        Assert.That(BootstrapAdminSettings.Load(Configuration()).Enabled, Is.False);
        Assert.Throws<InvalidOperationException>(() => BootstrapAdminSettings.Load(Configuration(("BootstrapAdmin:Enabled", "true"))));
    }

    [Test]
    public void ProductionFrontend_RejectsHttpAndRedirectPayloads()
    {
        foreach (var value in new[] { "http://localhost:5116", "https://user@example.com", "https://example.com/?next=evil", "https://example.com/#token" })
            Assert.Throws<InvalidOperationException>(() => ApplicationSettings.GetFrontendUrl(Configuration(("Frontend:BaseUrl", value)), false));
        Assert.That(ApplicationSettings.GetFrontendUrl(Configuration(("Frontend:BaseUrl", "https://example.com")), false).Host, Is.EqualTo("example.com"));
    }

    [Test]
    public void DatabaseSettings_RejectMissingAndMalformedValuesWithoutDisclosingInput()
    {
        Assert.Throws<InvalidOperationException>(() => ApplicationSettings.GetConnectionString(Configuration()));
        var exception = Assert.Throws<InvalidOperationException>(() => ApplicationSettings.GetConnectionString(Configuration(("ConnectionStrings:DefaultConnection", "not-a-connection-string"))));
        Assert.That(exception!.Message, Does.Not.Contain("not-a-connection-string"));
    }
}
