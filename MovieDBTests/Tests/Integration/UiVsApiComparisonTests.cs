using NUnit.Framework;
using MovieDBTests.API;   // doar acesta!
using MovieDBTests.Pages;
using System.Linq;

namespace MovieDBTests.Tests.Integration
{
    [TestFixture]
    public class UiVsApiComparisonTests : BaseTest
    {
        private DiscoverPage _page;

        [SetUp]
        public void SetupPage()
        {
            _page = new DiscoverPage(Driver);
        }

        [Test]
        public void Compare_UI_And_API_Results_By_Title()
        {
            var genreId = "18"; // Drama
            var from = 1990;
            var to = 2005;

            // API
            var api = new MovieApi();
            var apiMovies = api.DiscoverAllPagesAsync(
                "primary_release_date.asc", genreId, $"{from}-01-01", $"{to}-12-31", 2).Result;

            // UI
            _page.OpenWithFilters(genreId, from, to);
            var uiMovies = _page.GetMovieTitles();

            Assert.That(uiMovies, Is.Not.Empty, "UI should return some movies");
            Assert.That(apiMovies, Is.Not.Empty, "API should return some movies");

            var overlap = uiMovies.Intersect(apiMovies).ToList();
            Assert.That(overlap.Any(), Is.True, "There should be at least one common movie");
        }
    }
}
