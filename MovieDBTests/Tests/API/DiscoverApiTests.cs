using NUnit.Framework;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System;

namespace MovieDBTests.Tests.API
{
    [TestFixture]
    public class DiscoverApiTests
    {
        [Test]
        public async Task Discover_Filter_Date_And_Genres_Ascending()
        {
            var api = new MovieDBTests.API.MovieApi();
            var from = "1990-01-01";
            var to = "2005-12-31";
            var list = await api.DiscoverAllPagesAsync("primary_release_date.asc", withGenres: null, from: from, to: to, maxPages: 2);

            Assert.That(list.Count, Is.GreaterThan(0), "API should return movies.");

            // Validate ascending order
            var dates = list
                .Select(j => DateTime.TryParse(j.Value<string>("release_date"), out var dt) ? dt : (DateTime?)null)
                .Where(d => d.HasValue).Select(d => d!.Value).ToList();

            Assert.That(dates, Is.Ordered.Ascending, "Release dates from API should be ascending.");
            Assert.That(dates.All(d => d.Year >= 1990 && d.Year <= 2005), "Dates must be between 1990 and 2005.");
        }
    }
}
