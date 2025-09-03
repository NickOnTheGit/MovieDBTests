using NUnit.Framework;
using OpenQA.Selenium;
using System;
using System.Linq;
using System.Globalization;
using MovieDBTests.Drivers;
using MovieDBTests.Pages;

namespace MovieDBTests.Tests.UI
{
    [TestFixture]
    public class DiscoverFiltersTests
    {
        private IWebDriver _driver = null!;
        private DiscoverPage _page = null!;

        [SetUp]
        public void Setup()
        {
            _driver = WebDriverManager.Create();
            _page = new DiscoverPage(_driver);
            _page.Open();
        }

        [Test]
        public void Filter_By_ReleaseDate_Ascending_And_Genres()
        {
            _page.SortByReleaseDateAscending();
            _page.SelectGenres("Drama");

            _page.SetReleaseDateRange("1990", "2005", useCalendar: true);
            _page.SetMinimumUserScore(6);

            var results = _page.ReadResults();

            // Assertions: dates ascending and within range
            var dates = results.Select(r => r.ReleaseDate).Where(d => d.HasValue).Select(d => d!.Value).ToList();
            Assert.That(dates, Is.Ordered.Ascending, "Release dates should be ascending.");
            Assert.That(dates.All(d => d.Year >= 1990 && d.Year <= 2005), "Dates must be between 1990 and 2005.");
        }

        [TearDown]
        public void Teardown()
        {
            try
            {
                var scr = ((ITakesScreenshot)_driver).GetScreenshot();
                var name = System.IO.Path.Combine(TestContext.CurrentContext.WorkDirectory, $"screenshot_{TestContext.CurrentContext.Test.Name}.png");
                scr.SaveAsFile(name);
                TestContext.AddTestAttachment(name);
            }
            catch { /* ignore */ }
            _driver.Quit();
        }
    }
}
