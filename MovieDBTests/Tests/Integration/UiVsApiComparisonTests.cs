using NUnit.Framework;
using OpenQA.Selenium;
using System.Linq;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;

using MovieDBTests.Drivers;
using MovieDBTests.Pages;
using MovieDBTests.API;

namespace MovieDBTests.Tests.Integration
{
    [TestFixture]
    public class UiVsApiComparisonTests
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
        public async Task Compare_UI_And_API_Results_By_Title()
        {
            // UI actions
            _page.SortByReleaseDateAscending();
            _page.SelectGenres("Drama");
            _page.SetReleaseDateRange("1990", "2005", useCalendar: true);
            var uiResults = _page.ReadResults();
            var uiTitles = uiResults.Select(r => r.Title).Where(t => !string.IsNullOrWhiteSpace(t)).ToHashSet(StringComparer.OrdinalIgnoreCase);

            // API actions
            var api = new MovieApi();
            var genreApi = new GenreApi();
            var genres = await genreApi.GetGenresAsync();
            // pick Drama id
            var dramaId = genres.FirstOrDefault(kvp => kvp.Value.Equals("Drama", StringComparison.OrdinalIgnoreCase)).Key.ToString();
            var apiResults = await api.DiscoverAllPagesAsync("primary_release_date.asc", withGenres: dramaId, from: "1990-01-01", to: "2005-12-31", maxPages: 2);
            var apiTitles = apiResults.Select(j => j.Value<string>("title") ?? "").Where(t => !string.IsNullOrWhiteSpace(t)).ToHashSet(StringComparer.OrdinalIgnoreCase);

            // Comparison
            var intersection = uiTitles.Intersect(apiTitles, StringComparer.OrdinalIgnoreCase).ToList();
            Assert.That(intersection.Count, Is.GreaterThan(0), "At least some movies should overlap between UI and API.");

            TestContext.WriteLine($"UI count: {uiTitles.Count}, API count: {apiTitles.Count}, Intersection: {intersection.Count}");
        }

        [TearDown]
        public void Teardown()
        {
            try { _driver.Quit(); } catch { }
        }
    }
}
