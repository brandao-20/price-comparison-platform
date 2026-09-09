using System.Drawing;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace WebApp.UITests;

[TestFixture]
[Explicit("Requires a disposable database populated by scripts/Seed-DemoData.ps1 and explicit demo credentials.")]
public class PortfolioScreenshotTests : BaseUITest
{
    [Test]
    public void Capture_Comparison_And_Catalogue_WithFictionalDemoData()
    {
        var username = Environment.GetEnvironmentVariable("PORTFOLIO_DEMO_USERNAME");
        var password = Environment.GetEnvironmentVariable("PORTFOLIO_DEMO_PASSWORD");
        var outputDirectory = Environment.GetEnvironmentVariable("PORTFOLIO_CAPTURE_DIR");
        Assert.That(username, Is.Not.Null.And.Not.Empty, "Set PORTFOLIO_DEMO_USERNAME for the disposable administrator.");
        Assert.That(!string.IsNullOrEmpty(password), Is.True, "Set PORTFOLIO_DEMO_PASSWORD without committing it.");
        Assert.That(outputDirectory, Is.Not.Null.And.Not.Empty, "Set PORTFOLIO_CAPTURE_DIR to the screenshot output directory.");
        Directory.CreateDirectory(outputDirectory!);

        Driver.Manage().Window.Size = new Size(1440, 950);
        Driver.Navigate().GoToUrl(AppUrl + "/login");
        var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(20));
        wait.Until(d => d.FindElements(By.CssSelector("input.form-control")).Count >= 2);
        var inputs = Driver.FindElements(By.CssSelector("input.form-control"));
        inputs[0].SendKeys(username!);
        inputs[1].SendKeys(password!);
        Driver.FindElement(By.CssSelector("button[type='submit']")).Click();
        wait.Until(d => d.Url.EndsWith("/homeadmin", StringComparison.OrdinalIgnoreCase));

        // Follow application links so the existing in-memory session is preserved.
        Driver.FindElement(By.CssSelector("a[href='/comparar-precos']")).Click();
        wait.Until(d => d.FindElements(By.CssSelector("select.form-control option")).Count > 1);
        new SelectElement(Driver.FindElement(By.CssSelector("select.form-control")))
            .SelectByText("Whole Milk 1 L (Demo Dairy)");
        wait.Until(d => d.FindElements(By.CssSelector("select.form-control")).Count == 3);
        var selectors = Driver.FindElements(By.CssSelector("select.form-control"));
        new SelectElement(selectors[1]).SelectByText("Demo Market Central");
        new SelectElement(selectors[2]).SelectByText("Demo Market Riverside");
        wait.Until(d => d.FindElements(By.CssSelector("table tbody tr")).Count == 2);
        wait.Until(d => ((IJavaScriptExecutor)d).ExecuteScript(
            "return !!window.activeChart && window.activeChart.data.datasets.length === 2 " +
            "&& window.activeChart.data.datasets.every(series => series.data.length === 7);") is true);
        var prices = Driver.FindElements(By.CssSelector("table tbody td:nth-child(2)"))
            .Select(cell => decimal.Parse(cell.Text.Replace(',', '.'), System.Globalization.CultureInfo.InvariantCulture));
        Assert.That(prices, Is.EquivalentTo(new[] { 1.19m, 1.25m }));
        // Let the chart's default one-second animation finish before the native capture.
        Thread.Sleep(TimeSpan.FromMilliseconds(1200));
        Driver.FindElement(By.TagName("h3")).Click();
        Assert.That(Driver.FindElement(By.Id("blazor-error-ui")).Displayed, Is.False);
        SaveScreenshot(outputDirectory!, "price-comparison.png");

        Driver.FindElement(By.CssSelector("a[href='/products']")).Click();
        wait.Until(d => d.FindElements(By.CssSelector("table tbody tr")).Count == 5);
        Assert.That(Driver.FindElement(By.CssSelector("table")).Text,
            Does.Contain("Whole Milk 1 L").And.Contain("Olive Oil 750 ml").And.Contain("Pantry"));
        Driver.Manage().Window.Size = new Size(1440, 780);
        Driver.FindElement(By.TagName("h3")).Click();
        Assert.That(Driver.FindElement(By.Id("blazor-error-ui")).Displayed, Is.False);
        SaveScreenshot(outputDirectory!, "product-catalogue.png");
    }

    private void SaveScreenshot(string directory, string filename)
    {
        var path = Path.Combine(directory, filename);
        ((ITakesScreenshot)Driver).GetScreenshot().SaveAsFile(path);
        TestContext.AddTestAttachment(path, "Unedited screenshot of the running application with fictional demo data.");
    }
}
