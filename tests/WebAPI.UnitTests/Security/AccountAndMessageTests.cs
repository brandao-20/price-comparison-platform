using System.Linq.Expressions;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using WebAPI.Configuration;
using WebAPI.Controllers;
using WebAPI.Entities;
using WebAPI.Repositories;

namespace WebAPI.UnitTests.Security;

public class AccountAndMessageTests
{
    [TestCase(true)]
    [TestCase(false)]
    public async Task ProductWrites_DoNotPersistInjectedAccounts(bool create)
    {
        var products = new Mock<IProdutoRepository>();
        var categories = new Mock<ICategoriaRepository>();
        categories.Setup(repository => repository.ExistsAsync(1)).ReturnsAsync(true);
        products.Setup(repository => repository.AddAsync(It.IsAny<Produto>())).Returns(Task.CompletedTask);
        products.Setup(repository => repository.UpdateAsync(It.IsAny<Produto>())).Returns(Task.CompletedTask);
        using var context = new WebAPI.Context.AppDbContext();
        var controller = new ProdutosController(products.Object, categories.Object,
            Mock.Of<IRegistosPrecoRepository>(), Microsoft.Extensions.Logging.Abstractions.NullLogger<ProdutosController>.Instance, context);
        var product = new Produto
        {
            ProdutoId = 1, Nome = "Test product", Marca = "Test brand", CategoriaId = 1,
            Categoria = new Categoria { CategoriaId = 1, Nome = "Injected category" },
            Favoritos = new() { new Favorito { Utilizador = new Utilizador { TipoUtilizadorId = 1, Username = "injected" } } }
        };

        if (create) await controller.Create(product);
        else await controller.Update(1, product);

        if (create)
            products.Verify(repository => repository.AddAsync(It.Is<Produto>(p => p.Favoritos.Count == 0 && p.Categoria == null && p.Nome == "Test product")), Times.Once);
        else
            products.Verify(repository => repository.UpdateAsync(It.Is<Produto>(p => p.Favoritos.Count == 0 && p.Categoria == null && p.Nome == "Test product")), Times.Once);
    }

    private static ControllerContext Context(int userId) => new()
    {
        HttpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("utilizadorId", userId.ToString()), new Claim(ClaimTypes.Role, "User") }, "test"))
        }
    };

    [Test]
    public async Task Registration_DoesNotPromoteFirstUserOrAcceptInjectedAccountFields()
    {
        var users = new Mock<IUtilizadorRepository>();
        var roles = new Mock<ITipoUtilizadorRepository>();
        users.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Utilizador, bool>>>())).ReturnsAsync(Array.Empty<Utilizador>());
        roles.Setup(r => r.FindAsync(It.IsAny<Expression<Func<TipoUtilizador, bool>>>())).ReturnsAsync(new[] { new TipoUtilizador { TipoUtilizadorId = 2, Tipo = "USER" } });
        Utilizador? saved = null;
        users.Setup(r => r.AddAsync(It.IsAny<Utilizador>())).Callback<Utilizador>(user => saved = user).Returns(Task.CompletedTask);
        var controller = new UtilizadoresController(users.Object, roles.Object) { ControllerContext = Context(1) };
        await controller.Register(new Utilizador
        {
            Username = "test-user", Email = "test@example.com", Password = Guid.NewGuid().ToString(),
            UtilizadorId = 999, Pontos = 9999, GoogleId = "injected", TipoUtilizador = new TipoUtilizador { Tipo = "ADMIN" }
        });
        Assert.That(saved, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(saved!.TipoUtilizadorId, Is.EqualTo(2));
            Assert.That(saved.TipoUtilizador, Is.Null);
            Assert.That(saved.UtilizadorId, Is.Zero);
            Assert.That(saved.Pontos, Is.Zero);
            Assert.That(saved.GoogleId, Is.Null);
        });
    }

    [Test]
    public async Task Messages_DenyUnrelatedReadersAndIgnoreSpoofedSender()
    {
        var repository = new Mock<IMensagemRepository>();
        repository.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(new Mensagem { RemetenteId = 2, DestinatarioId = 3 });
        var controller = new MensagensController(repository.Object) { ControllerContext = Context(1) };
        Assert.That((await controller.GetMessagesByUser(2)).Result, Is.TypeOf<ForbidResult>());
        Assert.That((await controller.GetMessage(10)).Result, Is.TypeOf<ForbidResult>());
        await controller.CreateMessage(new Mensagem { RemetenteId = 999, DestinatarioId = 2, Conteudo = "Hello", Remetente = new Utilizador() });
        repository.Verify(r => r.AddAsync(It.Is<Mensagem>(message => message.RemetenteId == 1 && message.Remetente == null && message.DataEnvio > DateTime.UtcNow.AddMinutes(-1))), Times.Once);
    }

    [Test]
    public void ResponseSerialization_ExcludesNestedCredentialsWhileRequestBindingStillAcceptsPasswords()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        ApiJson.Configure(options);
        var message = new Mensagem { Remetente = new Utilizador { Username = "test", Password = "test-hash", GoogleToken = "test-oauth-token", GoogleId = "test-provider-id" } };
        var json = JsonSerializer.Serialize(message, options);
        Assert.That(json, Does.Not.Contain("test-hash").And.Not.Contain("test-oauth-token").And.Not.Contain("test-provider-id"));
        Assert.That(JsonSerializer.Deserialize<Utilizador>("{\"password\":\"test-input\"}", options)!.Password, Is.EqualTo("test-input"));
    }
}
