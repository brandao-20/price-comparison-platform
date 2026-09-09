# Local demonstration and screenshots

The images in this repository are unedited screenshots of the running Blazor application, captured with Selenium and Chrome on 2026-09-09. Stores, brands, products, and prices are fictional; they are not live retail offers. The UI retains its existing Portuguese text. No personal accounts, real retailer data, or credentials appear in the images.

## Prepare the demonstration

Use a **fresh disposable PostgreSQL database**, not an existing development or production database. Follow [configuration and local validation](configuration.md) to configure the API, apply migrations, and explicitly bootstrap an administrator named `DemoAdmin` with an address such as `demo-admin@example.com` and a unique password. Start the API and frontend, with matching `ApiBaseUrl` and `Frontend:BaseUrl` settings.

The commands below assume the default API port 5000 and frontend port 5116. Adjust both URLs if your local configuration differs.

From the repository root in PowerShell:

```powershell
$demoPassword = Read-Host 'Disposable administrator password' -AsSecureString
./scripts/Seed-DemoData.ps1 -ApiBaseUrl http://localhost:5000/ -AdminUsername DemoAdmin -AdminPassword $demoPassword
```

The script authenticates through the normal API and creates two categories, five products, two stores, and 70 price records: seven days of history for each product in each store. Dates end on the previous UTC day. It accepts only loopback API addresses and refuses to seed a non-empty catalogue. It does not delete or replace existing data. If a run is interrupted, use another fresh disposable database rather than trying to seed the partially populated one.

The seed runs only when explicitly invoked; it is not part of normal application startup or migrations.

## Try the application manually

1. Log in as the disposable administrator.
2. Open **Gerir Produtos**. Confirm that five sample products and their categories appear.
3. Open **Comparar Preços** through the sidebar. Choose **Whole Milk 1 L (Demo Dairy)**, **Demo Market Central**, and **Demo Market Riverside**. The latest prices should be EUR 1.19 and EUR 1.25, with seven points in each history series.
4. Open a product's details to inspect its price records. The optional Maps integration can remain unconfigured.
5. To check messaging, register a separate disposable regular user, log in, open **Chat**, and send a message to the demo administrator.
6. Log out and confirm the anonymous navigation returns.

Navigate through application links after logging in. The existing client currently requires login again after a full page reload because account state is not restored from local storage. See [security notes](../SECURITY.md) for this and other limitations.

## Run the automated checks

```powershell
dotnet test tests/WebAPI.UnitTests --no-restore
$env:UI_TESTS_APP_URL = 'http://localhost:5116'
dotnet test tests/WebApp.UITests --no-restore
```

The normal UI suite creates disposable accounts and exercises registration, login, profile access, navigation, comparison, chat, logout, and a rejected Google callback. Keep the demo administrator available for its chat scenario. The explicit screenshot fixture is excluded from the normal run.

## Capture the two images

The screenshot fixture uses the same application routes and normal login. It verifies the two current prices, both seven-point chart series, and the five-row catalogue before taking native browser screenshots. It does not inject HTML, change CSS, replace chart data, or edit the captured pixels.

```powershell
$env:UI_TESTS_APP_URL = 'http://localhost:5116'
$env:PORTFOLIO_DEMO_USERNAME = 'DemoAdmin'
$env:PORTFOLIO_DEMO_PASSWORD = [Net.NetworkCredential]::new('', $demoPassword).Password
$env:PORTFOLIO_CAPTURE_DIR = Join-Path $PWD 'docs/images'
try {
    dotnet test tests/WebApp.UITests --no-restore --filter FullyQualifiedName~PortfolioScreenshotTests
} finally {
    Remove-Item Env:PORTFOLIO_DEMO_PASSWORD -ErrorAction SilentlyContinue
    Remove-Variable demoPassword -ErrorAction SilentlyContinue
}
```

These commands overwrite `docs/images/price-comparison.png` and `docs/images/product-catalogue.png`. Review both images before committing them. The capture uses desktop windows of 1440 x 950 and 1440 x 780; the resulting viewport size can vary with operating-system window borders. Chrome and the existing Selenium dependencies are required; the first driver resolution may need network access.

The comparison is the primary README image. The catalogue is a supporting image in an expandable section. Keep the fictional-data captions when reusing them.

After testing, stop the temporary services, discard the disposable database, and remove temporary credentials. Never commit local `appsettings.json`, database files, or tokens.
