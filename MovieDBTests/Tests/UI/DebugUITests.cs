using NUnit.Framework;
using MovieDBTests.Pages;

namespace MovieDBTests.Tests.UI
{
    [TestFixture]
    public class DebugUITests : BaseTest
    {
        private DiscoverPage _page;

        [SetUp]
        public void SetupPage()
        {
            _page = new DiscoverPage(Driver);
        }

        [Test]
        public void Debug_Open_With_Action_Films()
        {
            _page.OpenWithFilters("28", 2010, 2020); // Action
            var results = _page.GetMovieTitles();

            TestContext.WriteLine($"Movies found: {string.Join(", ", results)}");
            Assert.That(results, Is.Not.Empty);
        }

        [Test]
        public void Debug_Open_With_Comedy_Films()
        {
            _page.OpenWithFilters("35", 2000, 2005); // Comedy
            var results = _page.GetMovieTitles();

            TestContext.WriteLine($"Movies found: {string.Join(", ", results)}");
            Assert.That(results, Is.Not.Empty);
        }
    }
}
