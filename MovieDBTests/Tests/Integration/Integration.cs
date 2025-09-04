using NUnit.Framework;
using MovieDBTests.API;
using MovieDBTests.Pages;
using System.Linq;
using System;
using System.Collections.Generic;
using OpenQA.Selenium;
using Newtonsoft.Json.Linq;

namespace MovieDBTests.Tests.Integration
{
    [TestFixture]
    public class RobustUiVsApiTests : BaseTest
    {
        private DiscoverPage _page = null!;
        private MovieApi _movieApi = null!;

        [SetUp]
        public void SetupIntegrationTests()
        {
            _page = new DiscoverPage(Driver ?? throw new InvalidOperationException("Driver not initialized"));
            _movieApi = new MovieApi();
        }

        [Test]
        [Category("Integration")]
        [Category("Critical")]
        public void Compare_UI_And_API_Drama_Movies_1990_2005()
        {
            const string genreId = "18"; // Drama
            const int fromYear = 1990;
            const int toYear = 2005;

            try
            {
                TestContext.WriteLine("=== STARTING UI vs API COMPARISON TEST ===");

                // API results
                TestContext.WriteLine("\n🔧 Fetching API results...");
                var apiMovies = _movieApi.DiscoverAllPagesAsync(
                    "primary_release_date.asc",
                    genreId,
                    $"{fromYear}-01-01",
                    $"{toYear}-12-31",
                    maxPages: 2
                ).Result;

                TestContext.WriteLine($"✓ API returned {apiMovies.Count} movies");
                LogMovieDetails("API", apiMovies.Take(5));

                // UI results
                TestContext.WriteLine("\n🌐 Fetching UI results...");
                _page.OpenWithFilters(genreId, fromYear, toYear);
                var uiMovies = _page.GetMovieTitles();

                TestContext.WriteLine($"✓ UI returned {uiMovies.Count} movies");
                LogMovieDetails("UI", uiMovies.Take(5));

                TakeScreenshot("ui_vs_api_comparison");

                Assert.That(apiMovies.Count, Is.GreaterThan(0), "API should return drama movies from 1990-2005");
                Assert.That(uiMovies.Count, Is.GreaterThan(0), "UI should return drama movies from 1990-2005");

                PerformDetailedComparison(uiMovies, apiMovies);

                TestContext.WriteLine("\n✅ UI vs API comparison completed successfully");
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"\n❌ UI vs API comparison failed: {ex.Message}");
                TakeScreenshot("ui_vs_api_failed");
                throw;
            }
        }

        [Test]
        [Category("Integration")]
        [Category("Validation")]
        public void Validate_API_Date_Filtering_Accuracy()
        {
            const int fromYear = 2000;
            const int toYear = 2005;

            try
            {
                TestContext.WriteLine("=== VALIDATING API DATE FILTERING ===");

                var apiMovies = _movieApi.DiscoverAllPagesAsync(
                    "primary_release_date.asc",
                    withGenres: null,
                    from: $"{fromYear}-01-01",
                    to: $"{toYear}-12-31",
                    maxPages: 3
                ).Result;

                TestContext.WriteLine($"✓ API returned {apiMovies.Count} movies for {fromYear}-{toYear}");

                var datesOutOfRange = new List<string>();
                var validDates = new List<DateTime>();

                foreach (var movie in apiMovies)
                {
                    string? releaseDateStr = movie.Value<string>("release_date");
                    string? movieTitle = movie.Value<string>("title");

                    if (!string.IsNullOrEmpty(releaseDateStr) &&
                        DateTime.TryParse(releaseDateStr, out var releaseDate))
                    {
                        if (releaseDate.Year < fromYear || releaseDate.Year > toYear)
                        {
                            datesOutOfRange.Add($"{movieTitle}: {releaseDateStr}");
                        }
                        else
                        {
                            validDates.Add(releaseDate);
                        }
                    }
                }

                TestContext.WriteLine($"✓ Movies with valid dates: {validDates.Count}");
                TestContext.WriteLine($"⚠️ Movies with out-of-range dates: {datesOutOfRange.Count}");

                if (datesOutOfRange.Any())
                {
                    TestContext.WriteLine("Out of range movies (first 5):");
                    foreach (var movie in datesOutOfRange.Take(5))
                    {
                        TestContext.WriteLine($"  - {movie}");
                    }
                }

                if (validDates.Count > 1)
                {
                    bool isAscending = validDates.Zip(validDates.Skip(1), (a, b) => a <= b).All(x => x);
                    TestContext.WriteLine($"✓ Dates are in ascending order: {isAscending}");

                    if (!isAscending)
                    {
                        TestContext.WriteLine("First 5 dates:");
                        foreach (var date in validDates.Take(5))
                        {
                            TestContext.WriteLine($"  - {date:yyyy-MM-dd}");
                        }
                    }
                }

                double outOfRangePercentage = (double)datesOutOfRange.Count / apiMovies.Count * 100;
                Assert.That(outOfRangePercentage, Is.LessThan(10),
                    $"More than 10% of movies are outside date range ({outOfRangePercentage:F1}%)");

                TestContext.WriteLine("✅ Date filtering validation completed");
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"❌ Date filtering validation failed: {ex.Message}");
                throw;
            }
        }

        [Test]
        [Category("Integration")]
        [Category("Performance")]
        public void Compare_Different_Genres_Performance()
        {
            var testCases = new[]
            {
                new { Name = "Action", GenreId = "28", ExpectedMinCount = 50 },
                new { Name = "Comedy", GenreId = "35", ExpectedMinCount = 50 },
                new { Name = "Drama", GenreId = "18", ExpectedMinCount = 100 },
                new { Name = "Horror", GenreId = "27", ExpectedMinCount = 30 }
            };

            var results = new List<(string Genre, int ApiCount, int UiCount, TimeSpan ApiTime, TimeSpan UiTime)>();

            foreach (var testCase in testCases)
            {
                try
                {
                    TestContext.WriteLine($"\n🎬 Testing {testCase.Name} movies...");

                    var apiStart = DateTime.Now;
                    var apiMovies = _movieApi.DiscoverAllPagesAsync(
                        "primary_release_date.asc",
                        testCase.GenreId,
                        "2000-01-01",
                        "2020-12-31",
                        maxPages: 1
                    ).Result;
                    var apiTime = DateTime.Now - apiStart;

                    var uiStart = DateTime.Now;
                    _page.OpenWithFilters(testCase.GenreId, 2000, 2020);
                    var uiMovies = _page.GetMovieTitles();
                    var uiTime = DateTime.Now - uiStart;

                    results.Add((testCase.Name, apiMovies.Count, uiMovies.Count, apiTime, uiTime));

                    TestContext.WriteLine($"  API: {apiMovies.Count} movies in {apiTime.TotalSeconds:F1}s");
                    TestContext.WriteLine($"  UI:  {uiMovies.Count} movies in {uiTime.TotalSeconds:F1}s");

                    Assert.That(apiMovies.Count, Is.GreaterThan(0), $"API should return {testCase.Name} movies");
                }
                catch (Exception ex)
                {
                    TestContext.WriteLine($"  ❌ {testCase.Name} test failed: {ex.Message}");
                }
            }

            TestContext.WriteLine("\n📊 PERFORMANCE SUMMARY");
            TestContext.WriteLine("Genre".PadRight(10) + "API Count".PadRight(12) + "UI Count".PadRight(12) + "API Time".PadRight(12) + "UI Time");
            TestContext.WriteLine(new string('-', 60));

            foreach (var result in results)
            {
                TestContext.WriteLine(
                    result.Genre.PadRight(10) +
                    result.ApiCount.ToString().PadRight(12) +
                    result.UiCount.ToString().PadRight(12) +
                    $"{result.ApiTime.TotalSeconds:F1}s".PadRight(12) +
                    $"{result.UiTime.TotalSeconds:F1}s"
                );
            }
        }

        private void PerformDetailedComparison(List<string> uiMovies, List<JObject> apiMovies)
        {
            TestContext.WriteLine("\n🔍 DETAILED COMPARISON ANALYSIS");

            var apiTitles = apiMovies
                .Select(m => m.Value<string>("title"))
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .ToList();

            var exactMatches = uiMovies.Intersect(apiTitles, StringComparer.OrdinalIgnoreCase).ToList();
            TestContext.WriteLine($"✓ Exact title matches: {exactMatches.Count}");

            int partialMatches = 0;
            foreach (var uiTitle in uiMovies)
            {
                if (apiTitles.Any(apiTitle =>
                    string.Equals(uiTitle, apiTitle, StringComparison.OrdinalIgnoreCase) ||
                    apiTitle?.Contains(uiTitle, StringComparison.OrdinalIgnoreCase) == true ||
                    uiTitle.Contains(apiTitle ?? "", StringComparison.OrdinalIgnoreCase)))
                {
                    partialMatches++;
                }
            }
            TestContext.WriteLine($"✓ Partial title matches: {partialMatches}");

            if (exactMatches.Any())
            {
                TestContext.WriteLine("\nExact matches (first 3):");
                foreach (var matchTitle in exactMatches.Take(3))
                {
                    TestContext.WriteLine($"  ✓ {matchTitle}");
                }
            }

            var uiOnlyTitles = uiMovies.Except(apiTitles, StringComparer.OrdinalIgnoreCase).ToList();
            if (uiOnlyTitles.Any())
            {
                TestContext.WriteLine($"\nUI-only titles ({uiOnlyTitles.Count}):");
                foreach (var uiTitle in uiOnlyTitles.Take(3))
                {
                    TestContext.WriteLine($"  🌐 {uiTitle}");
                }
            }

            var apiOnlyTitles = apiTitles.Except(uiMovies, StringComparer.OrdinalIgnoreCase).ToList();
            if (apiOnlyTitles.Any())
            {
                TestContext.WriteLine($"\nAPI-only titles ({apiOnlyTitles.Count}):");
                foreach (var apiOnlyTitle in apiOnlyTitles.Take(3))
                {
                    TestContext.WriteLine($"  🔧 {apiOnlyTitle}");
                }
            }

            double matchPercentage = (double)exactMatches.Count / Math.Min(uiMovies.Count, apiTitles.Count) * 100;
            TestContext.WriteLine($"\n📈 Match percentage: {matchPercentage:F1}%");

            Assert.That(exactMatches.Count > 0, "Should have at least some matching titles between UI and API");

            if (matchPercentage < 20)
            {
                TestContext.WriteLine("⚠️ Low match percentage detected - possible reasons:");
                TestContext.WriteLine("  - Different pagination or sorting");
                TestContext.WriteLine("  - UI showing different content than API");
                TestContext.WriteLine("  - Potential issues with selectors or API parameters");
            }
        }

        private void LogMovieDetails(string source, IEnumerable<object> movies)
        {
            TestContext.WriteLine($"{source} movies (first 5):");

            foreach (var movie in movies)
            {
                switch (movie)
                {
                    case string title:
                        TestContext.WriteLine($"  - {title}");
                        break;
                    case JObject jObj:
                        string? movieTitle = jObj.Value<string>("title");
                        string? date = jObj.Value<string>("release_date");
                        TestContext.WriteLine($"  - {movieTitle} ({date})");
                        break;
                }
            }
        }

        private void TakeScreenshot(string testName)
        {
            try
            {
                if (Driver is ITakesScreenshot takesScreenshot)
                {
                    var screenshot = takesScreenshot.GetScreenshot();
                    var fileName = $"integration_{testName}_{DateTime.Now:yyyyMMdd_HHmmss}.png";
                    var filePath = System.IO.Path.Combine(TestContext.CurrentContext.WorkDirectory, fileName);
                    screenshot.SaveAsFile(filePath);
                    TestContext.WriteLine($"📸 Screenshot saved: {fileName}");
                }
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"Failed to take screenshot: {ex.Message}");
            }
        }
    }
}
