using NUnit.Framework;
using MovieDBTests.Pages;

namespace MovieDBTests.Tests.UI
{
    [TestFixture]
    public class DiscoverFiltersTests : BaseTest
    {
        private DiscoverPage _page;

        [SetUp]
        public void SetupPage()
        {
            _page = new DiscoverPage(Driver);
        }

        [Test]
        public void Can_Open_Discover_With_Filters()
        {
            var genreId = "28"; // Action
            var from = 2000;
            var to = 2010;

            _page.OpenWithFilters(genreId, from, to);
            var results = _page.GetMovieTitles();

            Assert.That(results, Is.Not.Empty, "Should load results with filters applied");
        }

        [Test]
        public void Can_Load_Drama_90s()
        {
            var genreId = "18"; // Drama
            _page.OpenWithFilters(genreId, 1990, 1999);
            var results = _page.GetMovieTitles();

            Assert.That(results, Is.Not.Empty, "Should load Drama movies from the 90s");
        }
    }
}
