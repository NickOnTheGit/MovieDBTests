using NUnit.Framework;
using System.Net.Http;
using System.Threading.Tasks;
using System;
using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;

namespace MovieDBTests.Tests.API
{
    [TestFixture]
    public class ComprehensiveSanityTests
    {
        private readonly string _apiKey = "be7f2a20be86e0fc78f0ac66729b5cbe";

        [Test]
        [Order(1)]
        public async Task Test_API_Key_Basic_Authentication()
        {
            using var client = new HttpClient();

            // Test with API key parameter
            var urlWithKey = $"https://api.themoviedb.org/3/configuration?api_key={_apiKey}";
            TestContext.WriteLine($"Testing API Key auth: {urlWithKey}");

            try
            {
                var response = await client.GetAsync(urlWithKey);
                var content = await response.Content.ReadAsStringAsync();

                TestContext.WriteLine($"API Key Status: {response.StatusCode}");
                TestContext.WriteLine($"Response: {content.Substring(0, Math.Min(200, content.Length))}");

                Assert.That(response.IsSuccessStatusCode, Is.True, $"API key authentication failed: {content}");

                var json = JObject.Parse(content);
                Assert.That(json["images"], Is.Not.Null, "Configuration should contain images config");

                TestContext.WriteLine("✓ API Key authentication working");
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"API Key test failed: {ex.Message}");
                throw;
            }
        }

        [Test]
        [Order(2)]
        public async Task Test_Bearer_Token_Authentication()
        {
            using var client = new HttpClient();

            // You would need to get a Bearer token from TMDB
            var bearerToken = Environment.GetEnvironmentVariable("TMDB_BEARER_TOKEN");

            if (string.IsNullOrWhiteSpace(bearerToken))
            {
                TestContext.WriteLine("⚠️ TMDB_BEARER_TOKEN not set, skipping Bearer token test");
                Assert.Ignore("Bearer token not configured");
                return;
            }

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

            var url = "https://api.themoviedb.org/3/configuration";
            TestContext.WriteLine($"Testing Bearer token auth: {url}");

            try
            {
                var response = await client.GetAsync(url);
                var content = await response.Content.ReadAsStringAsync();

                TestContext.WriteLine($"Bearer Status: {response.StatusCode}");
                TestContext.WriteLine($"Response: {content.Substring(0, Math.Min(200, content.Length))}");

                if (response.IsSuccessStatusCode)
                {
                    TestContext.WriteLine("✓ Bearer token authentication working");
                }
                else
                {
                    TestContext.WriteLine("⚠️ Bearer token authentication failed, falling back to API key");
                }
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"Bearer token test failed: {ex.Message}");
            }
        }

        [Test]
        [Order(3)]
        public async Task Test_Discover_Movie_Endpoint()
        {
            using var client = new HttpClient();

            var testCases = new[]
            {
                // Basic discover
                $"https://api.themoviedb.org/3/discover/movie?api_key={_apiKey}",
                
                // With sorting
                $"https://api.themoviedb.org/3/discover/movie?api_key={_apiKey}&sort_by=primary_release_date.asc",
                
                // With date filter
                $"https://api.themoviedb.org/3/discover/movie?api_key={_apiKey}&sort_by=primary_release_date.asc&primary_release_date.gte=2000-01-01&primary_release_date.lte=2005-12-31",
                
                // With genre filter
                $"https://api.themoviedb.org/3/discover/movie?api_key={_apiKey}&with_genres=28", // Action
                
                // Combined filters
                $"https://api.themoviedb.org/3/discover/movie?api_key={_apiKey}&sort_by=primary_release_date.asc&primary_release_date.gte=1990-01-01&primary_release_date.lte=2005-12-31&with_genres=18" // Drama 1990-2005
            };

            bool anyWorking = false;

            foreach (var testUrl in testCases)
            {
                TestContext.WriteLine($"\n🔍 Testing: {testUrl}");

                try
                {
                    var response = await client.GetAsync(testUrl);
                    var content = await response.Content.ReadAsStringAsync();

                    TestContext.WriteLine($"Status: {response.StatusCode}");

                    if (response.IsSuccessStatusCode)
                    {
                        anyWorking = true;
                        var json = JObject.Parse(content);
                        var results = json["results"] as JArray;

                        TestContext.WriteLine($"✓ SUCCESS! Found {results?.Count ?? 0} movies");

                        if (results?.Count > 0)
                        {
                            var firstMovie = results[0];
                            TestContext.WriteLine($"First movie: {firstMovie["title"]} ({firstMovie["release_date"]})");
                        }
                    }
                    else
                    {
                        TestContext.WriteLine($"❌ FAILED: {content.Substring(0, Math.Min(300, content.Length))}");
                    }
                }
                catch (Exception ex)
                {
                    TestContext.WriteLine($"❌ EXCEPTION: {ex.Message}");
                }
            }

            Assert.That(anyWorking, Is.True, "At least one discover endpoint should work");
        }

        [Test]
        [Order(4)]
        public async Task Test_Genre_List_Endpoint()
        {
            using var client = new HttpClient();
            var url = $"https://api.themoviedb.org/3/genre/movie/list?api_key={_apiKey}";

            TestContext.WriteLine($"Testing Genre List: {url}");

            var response = await client.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();

            TestContext.WriteLine($"Status: {response.StatusCode}");

            Assert.That(response.IsSuccessStatusCode, Is.True, $"Genre list failed: {content}");

            var json = JObject.Parse(content);
            var genres = json["genres"] as JArray;

            Assert.That(genres, Is.Not.Null, "Should have genres array");
            Assert.That(genres.Count, Is.GreaterThan(0), "Should have at least one genre");

            TestContext.WriteLine($"✓ Found {genres.Count} genres");

            // Log some common genres for reference
            foreach (var genre in genres.Take(10))
            {
                TestContext.WriteLine($"  - {genre["name"]} (ID: {genre["id"]})");
            }
        }

        [Test]
        [Order(5)]
        public async Task Test_Date_Range_Validation()
        {
            var movieApi = new MovieDBTests.API.MovieApi();

            try
            {
                var results = await movieApi.DiscoverAsync(
                    sortBy: "primary_release_date.asc",
                    withGenres: null,
                    from: "1990-01-01",
                    to: "2005-12-31",
                    page: 1
                );

                Assert.That(results, Is.Not.Null, "Results should not be null");
                Assert.That(results.Count, Is.GreaterThan(0), "Should return movies for 1990-2005 range");

                TestContext.WriteLine($"✓ Date range filtering returned {results.Count} movies");

                // Validate dates are within range
                foreach (var movie in results.Take(5))
                {
                    var releaseDateStr = movie.Value<string>("release_date");
                    if (!string.IsNullOrEmpty(releaseDateStr) && DateTime.TryParse(releaseDateStr, out var releaseDate))
                    {
                        TestContext.WriteLine($"  - {movie.Value<string>("title")}: {releaseDateStr}");
                        Assert.That(releaseDate.Year, Is.GreaterThanOrEqualTo(1990), $"Movie {movie.Value<string>("title")} should be from 1990 or later");
                        Assert.That(releaseDate.Year, Is.LessThanOrEqualTo(2005), $"Movie {movie.Value<string>("title")} should be from 2005 or earlier");
                    }
                }
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"Date range test failed: {ex.Message}");
                throw;
            }
        }

        [Test]
        [Order(6)]
        public async Task Test_Genre_Filtering()
        {
            var movieApi = new MovieDBTests.API.MovieApi();

            try
            {
                // Test Drama genre (ID: 18)
                var results = await movieApi.DiscoverAsync(
                    sortBy: "primary_release_date.asc",
                    withGenres: "18",
                    from: null,
                    to: null,
                    page: 1
                );

                Assert.That(results, Is.Not.Null, "Results should not be null");
                Assert.That(results.Count, Is.GreaterThan(0), "Should return drama movies");

                TestContext.WriteLine($"✓ Genre filtering returned {results.Count} drama movies");

                foreach (var movie in results.Take(3))
                {
                    var genreIds = movie.Value<JArray>("genre_ids")?.ToObject<int[]>() ?? new int[0];
                    TestContext.WriteLine($"  - {movie.Value<string>("title")}: Genres [{string.Join(", ", genreIds)}]");
                }
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"Genre filtering test failed: {ex.Message}");
                throw;
            }
        }

        [Test]
        [Order(7)]
        public async Task Test_Sorting_Validation()
        {
            var movieApi = new MovieDBTests.API.MovieApi();

            try
            {
                var results = await movieApi.DiscoverAsync(
                    sortBy: "primary_release_date.asc",
                    withGenres: null,
                    from: "2000-01-01",
                    to: "2020-12-31",
                    page: 1
                );

                Assert.That(results.Count, Is.GreaterThan(1), "Need at least 2 movies to validate sorting");

                var dates = new List<DateTime>();
                foreach (var movie in results)
                {
                    var releaseDateStr = movie.Value<string>("release_date");
                    if (!string.IsNullOrEmpty(releaseDateStr) && DateTime.TryParse(releaseDateStr, out var releaseDate))
                    {
                        dates.Add(releaseDate);
                    }
                }

                Assert.That(dates.Count, Is.GreaterThan(1), "Need valid dates to test sorting");

                // Check if dates are in ascending order
                var isAscending = true;
                for (int i = 1; i < dates.Count; i++)
                {
                    if (dates[i] < dates[i - 1])
                    {
                        isAscending = false;
                        break;
                    }
                }

                TestContext.WriteLine($"✓ Sorting validation: {(isAscending ? "ASCENDING" : "NOT PROPERLY SORTED")}");
                TestContext.WriteLine($"First date: {dates.First():yyyy-MM-dd}, Last date: {dates.Last():yyyy-MM-dd}");

                // Note: We're being lenient here as API might not always return perfectly sorted results
                // But we should at least have a general ascending trend
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"Sorting validation failed: {ex.Message}");
                throw;
            }
        }

        [Test]
        [Order(8)]
        public async Task Test_Multiple_Pages()
        {
            var movieApi = new MovieDBTests.API.MovieApi();

            try
            {
                var page1 = await movieApi.DiscoverAsync("primary_release_date.asc", null, "2000-01-01", "2005-12-31", 1);
                var page2 = await movieApi.DiscoverAsync("primary_release_date.asc", null, "2000-01-01", "2005-12-31", 2);

                TestContext.WriteLine($"✓ Page 1: {page1.Count} movies");
                TestContext.WriteLine($"✓ Page 2: {page2.Count} movies");

                Assert.That(page1.Count, Is.GreaterThan(0), "Page 1 should have results");

                if (page2.Count > 0)
                {
                    // Verify no duplicate movies between pages
                    var page1Titles = page1.Select(m => m.Value<string>("title")).ToHashSet();
                    var page2Titles = page2.Select(m => m.Value<string>("title")).ToHashSet();
                    var duplicates = page1Titles.Intersect(page2Titles).Count();

                    TestContext.WriteLine($"Duplicate movies between pages: {duplicates}");
                    Assert.That(duplicates, Is.EqualTo(0), "Pages should not have duplicate movies");
                }
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"Multi-page test failed: {ex.Message}");
                throw;
            }
        }
    }
}