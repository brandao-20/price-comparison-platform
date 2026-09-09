# Configuration and local validation

The API reads standard ASP.NET Core configuration: ignored local `appsettings.json`, environment-specific settings, development user secrets, environment variables, and command-line arguments. Environment variables use `__` in place of `:`. The application does not load `.env` files automatically.

Copy `WebAPI/appsettings.example.json` to `WebAPI/appsettings.json` and replace the required placeholders. Never copy backend settings into `WebApp/wwwroot`: every frontend file is public.

| Setting / environment variable | Purpose |
| --- | --- |
| `ConnectionStrings:DefaultConnection` / `ConnectionStrings__DefaultConnection` | Required PostgreSQL connection string. Database names are case-sensitive. |
| `Jwt:Key` / `Jwt__Key` | Required, unique random signing secret of at least 32 UTF-8 bytes. Example placeholders are rejected. |
| `Jwt:Issuer` / `Jwt__Issuer` | Required token issuer, identical for issuance and validation. |
| `Jwt:Audience` / `Jwt__Audience` | Required token audience, identical for issuance and validation. |
| `Frontend:BaseUrl` / `Frontend__BaseUrl` | Frontend origin used for CORS and Google redirects. Defaults to `http://localhost:5116` only in Development; HTTPS is required otherwise. |
| `Authentication:Google:ClientId` / `Authentication__Google__ClientId` | Optional Google OAuth client ID. Leave both Google settings empty to disable Google login. |
| `Authentication:Google:ClientSecret` / `Authentication__Google__ClientSecret` | Optional Google OAuth secret, required when the client ID is configured. |
| `BootstrapAdmin:Enabled` / `BootstrapAdmin__Enabled` | Defaults to `false`. Enables one-time administrator creation if no administrator exists. |
| `BootstrapAdmin:Username`, `:Email`, `:Password` / `BootstrapAdmin__Username`, `__Email`, `__Password` | Required when bootstrap is enabled. Use a unique password of at least 12 characters. |

The API project has a `UserSecretsId`, so development secrets can also be stored with `dotnet user-secrets --project WebAPI`. For example, generate a random JWT key and store it without printing it in PowerShell:

```powershell
$bytes = New-Object byte[] 32
$rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
$rng.GetBytes($bytes)
$rng.Dispose()
dotnet user-secrets set "Jwt:Key" ([Convert]::ToBase64String($bytes)) --project WebAPI
```

User secrets are for local development. Supply production secrets through the hosting environment. The design-time EF factory loads database settings without running bootstrap or requiring JWT/Google secrets.

## Database and first administrator

Create the database with the exact name used by the connection string, for example `CREATE DATABASE "ES2";`. Install the matching EF tool version if needed:

```bash
dotnet tool install --global dotnet-ef --version 9.0.3
dotnet ef database update --project WebAPI/WebAPI.csproj
```

Migrations do not create an administrator. Public registration always creates a regular user, including on an empty database. To create the first administrator, set the four `BootstrapAdmin` values locally, start the API once, then disable bootstrap and remove its password from local configuration. Existing administrators are never overwritten. Conflicts with an existing username/email fail instead of promoting that account.

`20260907125718_RepairMissingSchema` adds objects used by the model but missing from the earlier migration operations: category hierarchy, store metadata, user points, favourites, comments, and reports. It also aligns the message primary-key name. Before upgrading a database whose schema was changed manually, back it up and compare it with the migration SQL; do not blindly apply the repair or mark migrations as applied. The repair does not rotate existing credentials or change existing administrator accounts.

```bash
dotnet ef migrations script --idempotent --project WebAPI/WebAPI.csproj --output migration-review.sql
```

Existing price/report timestamp columns keep their current database types. EF converts UTC values for those columns and reads them back as UTC; no stored timestamps are rewritten.

The API fails to start when required configuration is missing or initialization cannot reach a migrated database. Migrations are applied explicitly, not automatically at startup.

## Frontend and optional Maps

Copy `WebApp/wwwroot/appsettings.example.json` to `WebApp/wwwroot/appsettings.json`. `ApiBaseUrl` configures HTTP, SignalR, and Google login together; its development default is `http://localhost:5000/`. Use an HTTPS API URL outside Development. Blazor loads the configuration over HTTP.

`GoogleMapsApiKey` is an optional **browser key**, visible to visitors. Restrict it by HTTP referrer and by the Google APIs used by the app. An empty key leaves maps unavailable; it does not prevent the catalogue, login, or comparison pages from running.

## Google login

Register the API middleware callback URL in Google, for example `http://localhost:5000/signin-google` during local development or the HTTPS equivalent for a hosted API. `/api/ExternalLogin/GoogleCallback` is the internal completion action, not Google's registered callback.

The browser stores a random verifier in session storage and sends its SHA-256 challenge when starting login. After Google authenticates, the API reads and clears the temporary external cookie, then returns a random code rather than a JWT in the redirect. The frontend clears the callback URL and exchanges the code and verifier through `POST /api/ExternalLogin/exchange`.

Codes expire after two minutes, are single-use, and are bound to the original challenge. The bounded in-memory store supports one API instance; restarting it invalidates pending logins. Multiple API instances would require a shared store or another authentication design. Provider access/refresh tokens are not retained. The existing API JWT is still held in local storage after login.

## Tests

```bash
dotnet restore ES2_TP_ComparadorPrecos.sln
dotnet build ES2_TP_ComparadorPrecos.sln --no-restore
dotnet test tests/WebAPI.UnitTests --no-restore
dotnet restore tests/WebApp.UITests
dotnet test tests/WebApp.UITests --no-restore
dotnet list ES2_TP_ComparadorPrecos.sln package --vulnerable --include-transitive
```

Unit tests do not require PostgreSQL or Google credentials. Selenium tests require Chrome, a running frontend/API and a disposable migrated database. Selenium Manager resolves a matching ChromeDriver; the first run may require network access. UI tests create accounts, so do not point them at a production database. Set `UI_TESTS_APP_URL` to override `http://localhost:5116`; `UI_TESTS_HEADLESS=false` enables a visible browser. The comparison test exercises store selection when the database contains products and at least two stores for comparison.

The solution includes API/frontend projects and unit tests. The Selenium project is intentionally run separately because it requires running services. The session test sends a chat message to an existing administrator; bootstrap a disposable administrator first. Navigate through the app after login: account state is currently held in memory and is not restored after a full page reload.
