using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System;

namespace WebApp.UITests
{
    public class BaseUITest
    {
        protected IWebDriver Driver = null!;

        // Ajusta a porta se a tua app arrancar noutro URL
        protected static string AppUrl => Environment.GetEnvironmentVariable("UI_TESTS_APP_URL") ?? "http://localhost:5116";

        [SetUp]
        public void SetUp()
        {
            var options = new ChromeOptions();

            /* ─────────────────────────────────────────────
             *  HEADLESS × VISUAL
             *
             *  - por defeito corre em headless
             *  - define a variável de ambiente UI_TESTS_HEADLESS=false
             *    para ver o browser a executar os testes
             * ───────────────────────────────────────────── */
            var headless =
                !string.Equals(
                    Environment.GetEnvironmentVariable("UI_TESTS_HEADLESS"),
                    "false",
                    StringComparison.OrdinalIgnoreCase);

            if (headless)
            {
                // Chrome 109+ tem o modo «--headless=new» (mais estável)
                options.AddArgument("--headless=new");
            }
            else
            {
                options.AddArgument("--start-maximized");
            }

            // Opcional: desliga o banner “Chrome está a ser controlado…”
            options.AddExcludedArgument("enable-automation");
            options.AddAdditionalChromeOption("useAutomationExtension", false);

            Driver = new ChromeDriver(options);
            Driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(5);
        }

        [TearDown]
        public void TearDown()
        {
            /*  Para gravares o vídeo talvez queiras manter o browser
             *  aberto no final. Define UI_TESTS_KEEP_OPEN=true
             *  e o Quit() é ignorado.                               */
            var keepOpen = string.Equals(
                Environment.GetEnvironmentVariable("UI_TESTS_KEEP_OPEN"),
                "true",
                StringComparison.OrdinalIgnoreCase);

            if (!keepOpen)
            {
                Driver?.Quit();
                Driver?.Dispose();
            }
        }

        protected void WaitForPopupToClose(int timeoutSeconds = 10)
        {
            new WebDriverWait(Driver, TimeSpan.FromSeconds(timeoutSeconds))
                .Until(d => d.FindElements(By.CssSelector(".popup-overlay.show")).Count == 0);
        }
    }
}
