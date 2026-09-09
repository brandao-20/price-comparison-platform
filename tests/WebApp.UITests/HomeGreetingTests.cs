using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using System;

namespace WebApp.UITests
{
    [TestFixture]
    public class HomeGreetingTests : BaseUITest
    {
        private string _username = "";
        private const string Password = "Tests123!";

        [SetUp]
        public void RegisterAndLogin()
        {
            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(15));

            // 1) Register a new user
            _username = "ui" + DateTime.Now.Ticks;
            Driver.Navigate().GoToUrl(AppUrl + "/register");
            wait.Until(d => d.FindElements(By.CssSelector("input.form-control")).Count >= 3);
            var reg = Driver.FindElements(By.CssSelector("input.form-control"));
            reg[0].SendKeys(_username);
            reg[1].SendKeys($"{_username}@example.com");
            reg[2].SendKeys(Password);
            Driver.FindElement(By.CssSelector("button.btn.btn-primary")).Click();

            // 2) Wait to be redirected to /login
            wait.Until(d => d.Url.Contains("/login"));

            // 3) Login
            var loginInputs = Driver.FindElements(By.CssSelector("input.form-control"));
            loginInputs[0].SendKeys(_username);
            loginInputs[1].SendKeys(Password);
            Driver.FindElement(By.CssSelector("button.btn.btn-primary")).Click();

            // 4) Wait for the SPA redirect into /homeuser
            wait.Until(d => d.Url.EndsWith("/homeuser", StringComparison.OrdinalIgnoreCase));
            // and for the welcome headline
            wait.Until(d => d.FindElement(By.TagName("h1")).Displayed);
        }

        [Test]
        public void HomeUser_Shows_Welcome_Message()
        {
            var heading = Driver.FindElement(By.TagName("h1")).Text;
            StringAssert.Contains(
                $"Bem-vindo, {_username}!",
                heading,
                $"Esperava ‘Bem-vindo, {_username}!’, mas veio ‘{heading}’");
        }

        [Test]
        public void HomeUser_Chatbot_Button_Is_Present()
        {
            // The bold text in the help message should be present
            var bold = Driver.FindElement(By.XPath("//b[text()='Assistente de Preços']"));
            Assert.IsNotNull(bold, "O botão 'Assistente de Preços' não foi encontrado na home.");
        }
    }
}
