# Hardening and validation record

This record explains the portfolio cleanup completed on 2026-09-08 and its demonstration validation on 2026-09-09. It documents observed results, not production certification. The .NET 8 / ASP.NET Core / Blazor / PostgreSQL architecture, domain identifiers, existing features, and academic attribution were preserved. The existing README received targeted corrections and two real application screenshots.

## What changed and why

| Area | Change and reason | Main files |
| --- | --- | --- |
| JWT and database configuration | Removed signing-key and connection-string fallbacks. Validate required settings at startup and use the same issuer, audience, and signing key for both login methods and bearer validation. | `WebAPI/Configuration`, `WebAPI/Program.cs`, `AuthController.cs`, `AppDbContext.cs` |
| Administrator creation | Removed predictable administrator credentials from runtime seeding, migration SQL, and the client popup. Public registration always creates a regular user. Bootstrap is explicit, disabled by default, and never overwrites or promotes an existing conflicting account. | `BootstrapAdminSettings.cs`, `UtilizadoresController.cs`, `InitialCreateWithAdmin.cs`, `HomeAdmin.razor` |
| Google login | Replaced JWT query-string redirects with random, single-use, two-minute codes tied to a browser-generated SHA-256 challenge. The browser redeems its code and verifier through POST. Read and clear the temporary external cookie; do not retain provider tokens. | `ExternalLoginController.cs`, `GoogleLoginCodeStore.cs`, `oauthLogin.js`, `Login.razor` |
| API authorization | Added missing server-side write permissions, aligned role names, restricted account management, enforced message ownership, derived senders from JWT claims, and rejected client control of protected account/price fields and nested account graphs. | API controllers, `ChatHub.cs` |
| Sensitive responses and diagnostics | Exclude password hashes and Google credentials from user serialization, including nested users, while retaining existing password request binding. Removed sensitive/noisy logs and internal error details from responses. Hosting request URL logs are suppressed because SignalR can use query tokens. | `ApiJson.cs`, controllers, observers, client pages, `Program.cs` |
| Browser session and configuration | Centralized API URLs for HTTP, chat, and Google login. Logout removes the stored JWT and closes connections. Disposed chat connections and removed stale authorization headers. Missing optional Maps configuration is explicit. | `WebApp/Program.cs`, services, `MainLayout.razor`, `ChatRoom.razor`, map components |
| Input and export safety | User chat-widget text and map labels are rendered as text. CSV fields escape delimiters, quotes, and newlines and neutralize formula-like text. Malformed stored password hashes return a failed check. | `Chatbot.razor`, `mapsInterop.js`, `CsvField.cs`, export endpoints/strategy, `PasswordHelper.cs` |
| Database installation | Repaired missing category hierarchy, store metadata, user points, comments, favourites, and reports migration operations; aligned the message primary-key name. Kept existing timestamp column types and converted UTC values correctly for them. Added an EF design-time factory independent of runtime JWT/bootstrap configuration. | `RepairMissingSchema.cs`, its designer, `AppDbContext.cs`, `AppDbContextFactory.cs` |
| Dependencies | Aligned ASP.NET authentication packages with .NET 8 and EF relational versions with the existing provider. Removed unused BouncyCastle.NetCore and updated vulnerable BouncyCastle.Cryptography/Microsoft.Bcl.Memory versions without a framework migration. Selenium Manager replaces a fixed ChromeDriver version. | Project files |
| Tests and repository hygiene | Replaced empty placeholder tests with focused security coverage, removed personal UI test credentials, fixed setup shadowing, documented test prerequisites, and ignored local settings and temporary validation files. | `tests`, `.gitignore`, configuration examples |
| Demonstration | Added an explicit local seed script and screenshot fixture. Captured the actual comparison/chart and catalogue with fictional data. | `scripts/Seed-DemoData.ps1`, `PortfolioScreenshotTests.cs`, `docs/images`, `docs/demo.md` |

## Validation evidence

Validation used Windows, .NET SDK 8.0.303, PostgreSQL 17 in an isolated temporary cluster on port 55432, API port 5018, frontend port 5126, and headless Chrome/ChromeDriver 152.0.7977.82. No existing user database was migrated or populated.

| Check | Result |
| --- | --- |
| Full solution rebuild | Passed with zero errors. The full rebuild reports 38 existing nullable, async, and member-hiding warnings; incremental builds may report zero warnings. |
| API/unit suite | 44 passed. Includes JWT validation, bootstrap settings, Google callback/code binding/expiry/replay/concurrency, response credential filtering, authorization, mass assignment, malformed hashes, CSV safety, and migration/UTC checks. |
| Normal Selenium suite | 7 passed. The screenshot fixture is explicitly opt-in and is not counted as a normal UI test. |
| Explicit screenshot scenario | 1 passed. Verified both latest prices, two seven-point chart series, and all five catalogue rows. Both PNGs were visually inspected after capture. |
| Fresh PostgreSQL installation | All three migrations applied successfully to an empty disposable database, followed by successful API startup and demo seeding through HTTP. |
| Migration consistency | No pending model changes. Generated idempotent SQL was successfully replayed against the temporary migrated database during the hardening pass. |
| API smoke checks | Registration and login, denied anonymous/regular-user writes, message sender/reader boundaries, product/store/price creation, comparison, and invalid Google exchange were exercised against the temporary API. Missing JWT settings failed before database access. |
| Dependency audit | NuGet reported no known vulnerable direct or transitive packages in the solution and the separate UI test project at the time checked. |
| JavaScript and Git diff | Syntax checks passed for OAuth, Maps, and chart scripts; `git diff --check` passed. |

Principal commands, run from the repository root with local test configuration supplied separately:

```powershell
dotnet restore ES2_TP_ComparadorPrecos.sln
dotnet restore tests/WebApp.UITests
dotnet build ES2_TP_ComparadorPrecos.sln --no-restore -t:Rebuild --verbosity minimal
dotnet test tests/WebAPI.UnitTests --no-restore --verbosity minimal
$env:UI_TESTS_APP_URL = 'http://localhost:5126'
dotnet test tests/WebApp.UITests --no-restore --verbosity minimal
dotnet test tests/WebApp.UITests --no-restore --filter FullyQualifiedName~PortfolioScreenshotTests --verbosity minimal
dotnet ef database update --project WebAPI --no-build
dotnet ef migrations has-pending-model-changes --project WebAPI --no-build
dotnet ef migrations script --idempotent --project WebAPI --no-build --output .tmp-hardening/migrations.sql
dotnet list ES2_TP_ComparadorPrecos.sln package --vulnerable --include-transitive
dotnet list tests/WebApp.UITests package --vulnerable --include-transitive
node --check WebApp/wwwroot/js/oauthLogin.js
node --check WebApp/wwwroot/js/mapsInterop.js
node --check WebApp/wwwroot/js/charts.js
git diff --check
```

The screenshot command additionally requires the explicit demo credentials and output directory described in [the demo guide](demo.md). The normal comparison test permits an empty catalogue; the explicit capture scenario validates populated comparison data and a rendered chart.

## Limits and upgrade precautions

- Real Google-provider authentication and Google Maps were not exercised because external credentials were not configured. Automated callback tests use a simulated external authentication ticket; the code exchange and browser rejection paths are covered separately.
- Removing old credentials does not erase Git history, existing accounts, or old logs. Rotate any reused values and replace or disable the old administrator account.
- Review the repair SQL and reconcile manually changed databases before upgrading. Existing timestamps and administrator credentials are not rewritten by the repair.
- Password hashes still use the legacy PBKDF2 work factor. Login throttling, account lockout, JWT revocation, and price-confirmation abuse controls remain future work.
- JWTs remain in local storage, full page reloads lose the client account state, and the Google code store supports one API process. HTTPS and appropriate hosting/proxy configuration remain deployment responsibilities.
- Compiler warnings remain. No deployment infrastructure, project license, real email delivery, AI service, or live retailer price source has been added or claimed.

See [security notes](../SECURITY.md) and [configuration](configuration.md) for the detailed operational limits and setup requirements.
