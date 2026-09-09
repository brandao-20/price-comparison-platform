using System.IdentityModel.Tokens.Jwt;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using WebAPI.Configuration;
using WebAPI.Controllers;
using WebAPI.Entities;
using WebAPI.Repositories;
using WebAPI.Services;

namespace WebAPI.UnitTests.Security;

public class GoogleCallbackTests
{
    [TestCase(true)]
    [TestCase(false)]
    public async Task Callback_ClearsExternalCookieAndRequiresTheBrowserChallenge(bool hasChallenge)
    {
        var verifier = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        var challenge = WebEncoders.Base64UrlEncode(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));
        var properties = new AuthenticationProperties();
        if (hasChallenge) properties.Items["code_challenge"] = challenge;
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "test-google-id"),
            new Claim(ClaimTypes.Email, "test@example.com"),
            new Claim(ClaimTypes.Name, "Test User")
        }, "ExternalCookies"));
        var authentication = new Mock<IAuthenticationService>();
        authentication.Setup(service => service.AuthenticateAsync(It.IsAny<HttpContext>(), "ExternalCookies"))
            .ReturnsAsync(AuthenticateResult.Success(new AuthenticationTicket(principal, properties, "ExternalCookies")));
        authentication.Setup(service => service.SignOutAsync(It.IsAny<HttpContext>(), "ExternalCookies", It.IsAny<AuthenticationProperties>()))
            .Returns(Task.CompletedTask);
        using var services = new ServiceCollection().AddSingleton(authentication.Object).BuildServiceProvider();
        var users = new Mock<IUtilizadorRepository>();
        users.Setup(repository => repository.FindAsync(It.IsAny<Expression<Func<Utilizador, bool>>>()))
            .ReturnsAsync(new[] { new Utilizador { UtilizadorId = 7, TipoUtilizador = new TipoUtilizador { Tipo = "USER" } } });
        users.Setup(repository => repository.UpdateAsync(It.IsAny<Utilizador>())).Returns(Task.CompletedTask);
        var settings = JwtSettings.Load(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Key"] = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)),
            ["Jwt:Issuer"] = "test-issuer", ["Jwt:Audience"] = "test-client"
        }).Build());
        var codes = new GoogleLoginCodeStore(TimeProvider.System);
        var controller = new ExternalLoginController(settings, new Uri("https://client.example"), codes,
            Mock.Of<IAuthenticationSchemeProvider>(), users.Object, Mock.Of<ITipoUtilizadorRepository>(), new RoleService())
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { RequestServices = services } }
        };

        var redirect = (RedirectResult)await controller.GoogleCallback();

        authentication.Verify(service => service.SignOutAsync(It.IsAny<HttpContext>(), "ExternalCookies", It.IsAny<AuthenticationProperties>()), Times.Once);
        Assert.That(controller.Response.Headers.CacheControl.ToString(), Is.EqualTo("no-store"));
        var target = new Uri(redirect.Url!);
        Assert.That(target.GetLeftPart(UriPartial.Path), Is.EqualTo("https://client.example/login"));
        var query = QueryHelpers.ParseQuery(target.Query);
        if (!hasChallenge)
        {
            Assert.That(query.ContainsKey("code"), Is.False);
            users.Verify(repository => repository.UpdateAsync(It.IsAny<Utilizador>()), Times.Never);
            return;
        }
        Assert.That(query.Keys, Is.EquivalentTo(new[] { "code" }));
        var token = codes.Redeem(query["code"], verifier);
        var claims = new JwtSecurityTokenHandler().ValidateToken(token, settings.ValidationParameters, out _);
        Assert.That(claims.FindFirst("utilizadorId")?.Value, Is.EqualTo("7"));
        Assert.That(codes.Redeem(query["code"], verifier), Is.Null);
    }
}
