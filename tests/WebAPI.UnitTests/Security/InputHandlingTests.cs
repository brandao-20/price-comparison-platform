using System.IdentityModel.Tokens.Jwt;
using WebAPI.ExportStrategies;
using WebAPI.Helpers;
using WebApp.Services;

namespace WebAPI.UnitTests.Security;

public class InputHandlingTests
{
    [TestCase(null)]
    [TestCase("")]
    [TestCase("invalid:base64!")]
    [TestCase("AA==:AA==")]
    public void InvalidStoredPasswordHash_IsRejectedWithoutThrowing(string? hash) =>
        Assert.That(PasswordHelper.VerifyPassword("test-input", hash), Is.False);

    [Test]
    public void MissingExpiryOrToken_ClearsClientIdentity()
    {
        var auth = new AuthService { UserName = "stale-user", Role = "Admin", UserId = 1,
            Token = new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken()) };
        Assert.That(auth.ValidateToken(), Is.False);
        Assert.That(auth.UserId, Is.Zero);
        Assert.That(auth.IsAdmin, Is.False);
        auth.UserName = "stale-user";
        Assert.That(auth.ValidateToken(), Is.False);
        Assert.That(auth.UserName, Is.Empty);
    }

    [Test]
    public void Csv_EscapesDelimitersQuotesAndFormulaPrefixes()
    {
        Assert.That(CsvField.Escape("Milk, large"), Is.EqualTo("\"Milk, large\""));
        Assert.That(CsvField.Escape("Shop \"A\""), Is.EqualTo("\"Shop \"\"A\"\"\""));
        Assert.That(CsvField.Escape("=1+1"), Is.EqualTo("'=1+1"));
        Assert.That(CsvField.Escape(" @SUM(A1)"), Is.EqualTo("' @SUM(A1)"));
        Assert.That(CsvField.Escape("one;two", ';'), Is.EqualTo("\"one;two\""));
    }
}
