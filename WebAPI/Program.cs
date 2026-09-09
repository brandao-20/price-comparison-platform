using WebAPI.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.EntityFrameworkCore;

using WebAPI.Context;
using WebAPI.Factories;
using WebAPI.Hubs;
using WebAPI.Observers;
using WebAPI.Repositories;
using WebAPI.Services;

var builder = WebApplication.CreateBuilder(args);
var jwtSettings = JwtSettings.Load(builder.Configuration);
var connectionString = ApplicationSettings.GetConnectionString(builder.Configuration);
var bootstrapAdmin = BootstrapAdminSettings.Load(builder.Configuration);
var frontendUrl = ApplicationSettings.GetFrontendUrl(builder.Configuration, builder.Environment.IsDevelopment());
builder.Services.AddSingleton(jwtSettings);
builder.Services.AddSingleton(frontendUrl);
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<GoogleLoginCodeStore>();
// Request URLs may carry OAuth codes or SignalR access tokens.
builder.Logging.AddFilter("Microsoft.AspNetCore.Hosting", LogLevel.Warning);

// Configure database access.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// Register application services.
builder.Services.AddScoped<IRepositoryFactory, RepositoryFactory>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddSignalR();
builder.Services.AddScoped<ChatService>();
builder.Services.AddScoped<IMessageObserverRegistry, MessageObserverRegistry>();

// Register repositories.
builder.Services.AddScoped<IUtilizadorRepository>(sp =>
    sp.GetRequiredService<IRepositoryFactory>().CreateUtilizadorRepository());
builder.Services.AddScoped<IProdutoRepository>(sp =>
    sp.GetRequiredService<IRepositoryFactory>().CreateProdutoRepository());
builder.Services.AddScoped<ICategoriaRepository>(sp =>
    sp.GetRequiredService<IRepositoryFactory>().CreateCategoriaRepository());
builder.Services.AddScoped<ILojaRepository>(sp =>
    sp.GetRequiredService<IRepositoryFactory>().CreateLojaRepository());
builder.Services.AddScoped<ILocalizacaoRepository>(sp =>
    sp.GetRequiredService<IRepositoryFactory>().CreateLocalizacaoRepository());
builder.Services.AddScoped<IRegistosPrecoRepository>(sp =>
    sp.GetRequiredService<IRepositoryFactory>().CreateRegistosPrecoRepository());
builder.Services.AddScoped<ITipoAcaoRepository>(sp =>
    sp.GetRequiredService<IRepositoryFactory>().CreateTipoAcaoRepository());
builder.Services.AddScoped<ITipoUtilizadorRepository>(sp =>
    sp.GetRequiredService<IRepositoryFactory>().CreateTipoUtilizadorRepository());
builder.Services.AddScoped<IMensagemRepository>(sp =>
    sp.GetRequiredService<IRepositoryFactory>().CreateMensagemRepository());

// Configure controllers, Swagger, and the frontend origin.
builder.Services.AddControllers().AddJsonOptions(options => ApiJson.Configure(options.JsonSerializerOptions));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    options.AddPolicy("DevCorsPolicy", policy =>
    {
        policy.WithOrigins(frontendUrl.GetLeftPart(UriPartial.Authority))
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials()
              .WithExposedHeaders("X-Total-Count");
    });
});

var authentication = builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = "ExternalCookies";
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = jwtSettings.ValidationParameters;
    options.IncludeErrorDetails = false;
    options.Events = new JwtBearerEvents
    {
        // Browser WebSocket/SSE transports cannot set an Authorization header.
        OnMessageReceived = context =>
        {
            if (context.Request.Path.StartsWithSegments("/chathub")
                && !context.Request.Headers.ContainsKey("Authorization"))
                context.Token = context.Request.Query["access_token"];
            return Task.CompletedTask;
        }
    };
})
.AddCookie("ExternalCookies", options =>
{
    options.ExpireTimeSpan = TimeSpan.FromMinutes(5);
    options.SlidingExpiration = false;
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
});

var googleId = builder.Configuration["Authentication:Google:ClientId"];
var googleSecret = builder.Configuration["Authentication:Google:ClientSecret"];
if (!string.IsNullOrWhiteSpace(googleId) || !string.IsNullOrWhiteSpace(googleSecret))
{
    if (string.IsNullOrWhiteSpace(googleId) || string.IsNullOrWhiteSpace(googleSecret)
        || googleId.Contains("REPLACE", StringComparison.OrdinalIgnoreCase)
        || googleSecret.Contains("REPLACE", StringComparison.OrdinalIgnoreCase))
        throw new InvalidOperationException("Google authentication requires both ClientId and ClientSecret. Leave both empty to disable the optional integration.");
    authentication.AddGoogle(options =>
    {
        options.SignInScheme = "ExternalCookies";
        options.ClientId = googleId;
        options.ClientSecret = googleSecret;
        options.SaveTokens = false;
        options.Events.OnRemoteFailure = context =>
        {
            context.HandleResponse();
            context.Response.Redirect(new Uri(frontendUrl, "login?error=GoogleLoginFailed").AbsoluteUri);
            return Task.CompletedTask;
        };
    });
}

builder.Services.AddAuthorization();

var app = builder.Build();

// Seed required roles and optionally bootstrap an administrator. Fail clearly if the database is unavailable.
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        dbContext.SeedInitialData(bootstrapAdmin);
    }
    catch (Exception)
    {
        app.Logger.LogCritical("Database initialization failed. Check the PostgreSQL connection and apply the EF Core migrations before starting the API.");
        throw new InvalidOperationException("Database initialization failed. Check the connection and apply the documented migrations.");
    }
}

// Return actual JSON without exposing internal exception details.
app.UseExceptionHandler(errorApp => errorApp.Run(async context =>
{
    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
    await context.Response.WriteAsJsonAsync(new
    {
        Success = false,
        StatusCode = StatusCodes.Status500InternalServerError,
        Message = "An internal error occurred. Please try again later."
    });
}));

// Configure the HTTP pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();
app.UseCors("DevCorsPolicy");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<ChatHub>("/chathub").RequireAuthorization();

app.Run();

public partial class Program { }
