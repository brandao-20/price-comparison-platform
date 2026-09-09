using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using System.Linq;
using System;

namespace WebApp.UITests
{
    [TestFixture]
    public class ComparisonTests : BaseUITest
    {
        [SetUp]
        public void Login()
        {
            var username = "comparison" + Guid.NewGuid().ToString("N")[..12];
            var password = Guid.NewGuid().ToString("N");
            Driver.Navigate().GoToUrl(AppUrl + "/register");
            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(15));
            wait.Until(d => d.FindElements(By.CssSelector("input.form-control")).Count >= 3);
            var registration = Driver.FindElements(By.CssSelector("input.form-control"));
            registration[0].SendKeys(username);
            registration[1].SendKeys(username + "@example.com");
            registration[2].SendKeys(password);
            Driver.FindElement(By.CssSelector("button.btn.btn-primary")).Click();
            wait.Until(d => d.Url.Contains("/login"));
            Driver.Navigate().GoToUrl(AppUrl + "/login");

            new WebDriverWait(Driver, TimeSpan.FromSeconds(15))
                .Until(d => d.FindElements(By.CssSelector("input.form-control")).Count >= 2);

            var inputs = Driver.FindElements(By.CssSelector("input.form-control"));
            inputs[0].SendKeys(username);
            inputs[1].SendKeys(password);
            Driver.FindElement(By.CssSelector("button.btn.btn-primary")).Click();

            new WebDriverWait(Driver, TimeSpan.FromSeconds(15))
                .Until(d => !d.Url.Contains("/login"));
        }

        [Test]
        public void Compare_Prices_Page_Shows_Default_And_All_Stores_If_Available()
        {
            Driver.Navigate().GoToUrl(AppUrl + "/comparar-precos");

            // Aguarda o dropdown de produtos
            new WebDriverWait(Driver, TimeSpan.FromSeconds(15))
                .Until(d => d.FindElement(By.CssSelector("select.form-control")));

            var select = new SelectElement(Driver.FindElement(By.CssSelector("select.form-control")));
            var options = select.Options;

            // Deve conter ao menos a opção padrão
            Assert.IsTrue(options.Any(opt => opt.Text.Contains("Escolha um produto")), 
                "Dropdown deve conter a opção padrão.");

            // Se existir pelo menos 1 produto além do padrão, tenta comparar
            if (options.Count > 1)
            {
                select.SelectByIndex(1);

                // Aguarda pelo menos 3 dropdowns: produto, loja1 e loja2
                new WebDriverWait(Driver, TimeSpan.FromSeconds(15))
                    .Until(d => d.FindElements(By.CssSelector("select.form-control")).Count >= 3);

                var selects = Driver.FindElements(By.CssSelector("select.form-control"));
                Assert.IsTrue(selects.Count >= 3, 
                    "Devem existir dropdowns de loja quando um produto válido for selecionado.");

                // Seleciona duas lojas diferentes
                new SelectElement(selects[1]).SelectByIndex(1);
                new SelectElement(selects[2]).SelectByIndex(2);

                // Aguarda a tabela de preços ou mensagem de ausência
                new WebDriverWait(Driver, TimeSpan.FromSeconds(15))
                    .Until(d => d.FindElements(By.CssSelector("table.table-striped tbody tr")).Count > 0
                                || d.PageSource.Contains("Sem preços registados"));

                var rows = Driver.FindElements(By.CssSelector("table.table-striped tbody tr"));
                Assert.IsTrue(rows.Count > 0 || Driver.PageSource.Contains("Sem preços registados"),
                    "Deve exibir preços atuais ou mensagem de ausência de preços.");
            }
        }
    }
}
