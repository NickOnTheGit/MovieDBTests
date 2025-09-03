using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;

namespace MovieDBTests.Drivers
{
    public static class WebDriverManager
    {
        public static IWebDriver Create()
        {
            var options = new ChromeOptions();
            if (Utils.Config.Headless)
            {
                options.AddArgument("--headless=new");
            }
            options.AddArgument("--start-maximized");
            options.AddArgument("--disable-gpu");
            options.AddArgument("--no-sandbox");
            options.AddArgument("--disable-dev-shm-usage");
            return new ChromeDriver(options);
        }
    }
}
