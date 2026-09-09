using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using System;

namespace WebApp.UITests
{
    [TestFixture]
    public class UserAuthTests : BaseUITest
    {
        [Test]
        public void Register_Login_Chat_And_Logout_PreserveTheSessionBoundary()
        {
            Driver.Navigate().GoToUrl(AppUrl + "/register");

            // Aguarda o formulário de registo
            new WebDriverWait(Driver, TimeSpan.FromSeconds(15))
                .Until(d => d.FindElements(By.CssSelector("input.form-control")).Count >= 3);

            // Preenche o formulário de registo
            var inputs = Driver.FindElements(By.CssSelector("input.form-control"));
            string uniqueUsername = "tests" + DateTime.Now.Ticks;
            inputs[0].SendKeys(uniqueUsername);          // Username
            inputs[1].SendKeys(uniqueUsername + "@example.com"); // Email
            inputs[2].SendKeys("Tests123!");            // Password
            Driver.FindElement(By.CssSelector("button.btn.btn-primary")).Click();

            // Aguarda redirecionamento após registo
            new WebDriverWait(Driver, TimeSpan.FromSeconds(15))
                .Until(d => d.Url.Contains("/login"));

            // Faz login
            Driver.Navigate().GoToUrl(AppUrl + "/login");
            new WebDriverWait(Driver, TimeSpan.FromSeconds(15))
                .Until(d => d.FindElements(By.CssSelector("input.form-control")).Count >= 2);

            inputs = Driver.FindElements(By.CssSelector("input.form-control"));
            inputs[0].SendKeys(uniqueUsername);
            inputs[1].SendKeys("Tests123!");
            Driver.FindElement(By.CssSelector("button.btn.btn-primary")).Click();

            // Aguarda o redirecionamento após login
            new WebDriverWait(Driver, TimeSpan.FromSeconds(15))
                .Until(d => d.FindElement(By.TagName("h1")).Text.Contains("Bem-vindo"));

            Assert.IsTrue(Driver.FindElement(By.TagName("h1")).Text.Contains("Bem-vindo"), "Login deve ser bem-sucedido.");
            Driver.FindElement(By.CssSelector("a[href='/chat']")).Click();
            new WebDriverWait(Driver, TimeSpan.FromSeconds(10))
                .Until(d => d.FindElements(By.CssSelector("table tbody button")).Count > 0);
            Driver.FindElement(By.CssSelector("table tbody button")).Click();
            new WebDriverWait(Driver, TimeSpan.FromSeconds(15))
                .Until(d => d.FindElements(By.CssSelector(".chat-container input")).Count > 0);
            var message = "Session validation " + Guid.NewGuid().ToString("N");
            Driver.FindElement(By.CssSelector(".chat-container input")).SendKeys(message);
            Driver.FindElement(By.CssSelector(".chat-container button")).Click();
            new WebDriverWait(Driver, TimeSpan.FromSeconds(10))
                .Until(d => d.FindElement(By.CssSelector(".messages")).Text.Contains(message));
            Driver.FindElement(By.CssSelector("button.btn-outline-secondary")).Click();
            new WebDriverWait(Driver, TimeSpan.FromSeconds(10))
                .Until(d => d.FindElements(By.CssSelector("button.btn-outline-success")).Count > 0);
            Assert.That(((IJavaScriptExecutor)Driver).ExecuteScript("return localStorage.getItem('authToken');"), Is.Null);
        }

        [Test]
        public void GoogleCallback_WithoutVerifier_ClearsTheUrlAndDoesNotLogIn()
        {
            Driver.Navigate().GoToUrl(AppUrl + "/login?code=" + new string('a', 43));
            new WebDriverWait(Driver, TimeSpan.FromSeconds(15))
                .Until(d => d.FindElement(By.TagName("body")).Text.Contains("Google login could not be completed"));
            Assert.That(new Uri(Driver.Url).Query, Is.Empty);
            Assert.That(((IJavaScriptExecutor)Driver).ExecuteScript("return localStorage.getItem('authToken');"), Is.Null);
        }
    }
}
