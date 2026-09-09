using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.JSInterop;
using WebApp;
using WebApp.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// WebAssemblyHostBuilder loads wwwroot/appsettings.json through HTTP.
var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5000/";
if (!Uri.TryCreate(apiBaseUrl, UriKind.Absolute, out var apiUri)
    || !string.IsNullOrEmpty(apiUri.UserInfo) || !string.IsNullOrEmpty(apiUri.Query)
    || !string.IsNullOrEmpty(apiUri.Fragment)
    || (apiUri.Scheme != Uri.UriSchemeHttps && !(builder.HostEnvironment.IsDevelopment() && apiUri.IsLoopback && apiUri.Scheme == Uri.UriSchemeHttp)))
    throw new InvalidOperationException("ApiBaseUrl must be an HTTPS URL. HTTP loopback is allowed in Development.");
apiUri = new Uri(apiUri.AbsoluteUri.TrimEnd('/') + "/");

builder.Services.AddBlazoredLocalStorage();
builder.Services.AddScoped<AuthService>();
builder.Services.AddTransient<AuthMessageHandler>();

// Registar HttpClient com o AuthMessageHandler
builder.Services.AddScoped(sp =>
{
    var handler = sp.GetRequiredService<AuthMessageHandler>();
    return new HttpClient(handler)
    {
        BaseAddress = apiUri
    };
});

// Registar o HubConnection como singleton, mas criando um escopo no AccessTokenProvider
builder.Services.AddSingleton(sp =>
{
    var hubConnection = new HubConnectionBuilder()
        .WithUrl(new Uri(apiUri, "chathub"), options =>
        {
            options.AccessTokenProvider = async () =>
            {
                using (var scope = sp.CreateScope())
                {
                    var localStorage = scope.ServiceProvider.GetRequiredService<ILocalStorageService>();
                    var token = await localStorage.GetItemAsync<string>("authToken");
                    return token;
                }
            };
        })
        .WithAutomaticReconnect()
        .Build();

    // Listener para notificações de mudança de preço
    hubConnection.On<int, decimal>("PriceChanged", async (produtoId, preco) =>
    {
        using (var scope = sp.CreateScope())
        {
            var jsRuntime = scope.ServiceProvider.GetRequiredService<IJSRuntime>();
            await jsRuntime.InvokeVoidAsync("alert", $"Preço do produto {produtoId} mudou para {preco:C}!");
        }
    });

    return hubConnection;
});

await builder.Build().RunAsync();
