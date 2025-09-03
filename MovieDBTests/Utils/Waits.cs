using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;
using System;

namespace MovieDBTests.Utils
{
    public static class Waits
    {
        public static IWebElement UntilVisible(IWebDriver driver, By by, int seconds = 10)
        {
            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(seconds));
            return wait.Until(ExpectedConditions.ElementIsVisible(by));
        }

        public static void UntilUrlContains(IWebDriver driver, string fragment, int seconds = 10)
        {
            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(seconds));
            wait.Until(d => d.Url.Contains(fragment, StringComparison.OrdinalIgnoreCase));
        }
    }
}
