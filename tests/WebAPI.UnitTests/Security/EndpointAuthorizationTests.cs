using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using WebAPI.Controllers;

namespace WebAPI.UnitTests.Security;

public class EndpointAuthorizationTests
{
    [TestCase(typeof(UtilizadoresController), nameof(UtilizadoresController.Update))]
    [TestCase(typeof(LojasController), nameof(LojasController.Create))]
    [TestCase(typeof(LocalizacoesController), nameof(LocalizacoesController.Delete))]
    [TestCase(typeof(TipoUtilizadoresController), nameof(TipoUtilizadoresController.Update))]
    [TestCase(typeof(TipoAcaosController), nameof(TipoAcaosController.Create))]
    [TestCase(typeof(CategoriasController), nameof(CategoriasController.Create))]
    public async Task ProtectedWrites_RejectAnonymousAndRegularUsersButAcceptAdministrator(Type controller, string action)
    {
        using var services = new ServiceCollection().AddLogging().AddAuthorization().BuildServiceProvider();
        var attributes = controller.GetCustomAttributes<AuthorizeAttribute>()
            .Concat(controller.GetMethod(action)!.GetCustomAttributes<AuthorizeAttribute>());
        var policy = await AuthorizationPolicy.CombineAsync(services.GetRequiredService<IAuthorizationPolicyProvider>(), attributes);
        Assert.That(policy, Is.Not.Null, "The endpoint must have a server-side authorization policy.");
        var authorization = services.GetRequiredService<IAuthorizationService>();
        var anonymous = new ClaimsPrincipal(new ClaimsIdentity());
        var regular = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, "User") }, "test"));
        var admin = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, "Admin") }, "test"));
        Assert.That((await authorization.AuthorizeAsync(anonymous, null, policy!)).Succeeded, Is.False);
        Assert.That((await authorization.AuthorizeAsync(regular, null, policy!)).Succeeded, Is.False);
        Assert.That((await authorization.AuthorizeAsync(admin, null, policy!)).Succeeded, Is.True);
    }
}
