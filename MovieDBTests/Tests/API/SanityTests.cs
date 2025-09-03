// Complete SanityTests.cs file - replace your entire file with this
using NUnit.Framework;
using System.Net.Http;
using System.Threading.Tasks;
using System;
using Newtonsoft.Json.Linq;

namespace MovieDBTests.Tests.API
{
    [TestFixture]
    public class SanityTests
    {
        [Test]
        public async Task Test_API_Key_And_Connection()
        {
            using var client = new HttpClient();
            var url = $"https://api.themoviedb.org/3/genre/movie/list?api_key=be7f2a20be86e0fc78f0ac66729b5cbe";

            TestContext.WriteLine($"Testing URL: {url}");

            var response = await client.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();

            TestContext.WriteLine($"Status: {response.StatusCode}");
            TestContext.WriteLine($"Response: {content}");

            Assert.That(response.IsSuccessStatusCode, Is.True, $"API call failed: {content}");
        }

        [Test]
        public async Task Test_Discover_With_Date_Filter()
        {
            using var client = new HttpClient();
            var url = $"https://api.themoviedb.org/3/discover/movie?api_key={Utils.Config.ApiKey}&sort_by=primary_release_date.asc&primary_release_date.gte=1990-01-01&primary_release_date.lte=2005-12-31";

            var response = await client.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();

            TestContext.WriteLine($"Status: {response.StatusCode}");
            TestContext.WriteLine($"Response: {content.Substring(0, Math.Min(300, content.Length))}");

            Assert.That(response.IsSuccessStatusCode, Is.True, "Discover endpoint with filters should work");
        }

        [Test]
        public async Task Test_API_Key_With_Simple_Endpoint()
        {
            using var client = new HttpClient();

            var simpleUrl = $"https://api.themoviedb.org/3/configuration?api_key={Utils.Config.ApiKey}";
            TestContext.WriteLine($"Testing simple URL: {simpleUrl}");

            var response = await client.GetAsync(simpleUrl);
            var content = await response.Content.ReadAsStringAsync();

            TestContext.WriteLine($"Status: {response.StatusCode}");
            TestContext.WriteLine($"Response: {content.Substring(0, Math.Min(300, content.Length))}");

            if (!response.IsSuccessStatusCode)
            {
                var genreUrl = $"https://api.themoviedb.org/3/genre/movie/list?api_key={Utils.Config.ApiKey}";
                TestContext.WriteLine($"\nTrying genres URL: {genreUrl}");

                var genreResponse = await client.GetAsync(genreUrl);
                var genreContent = await genreResponse.Content.ReadAsStringAsync();

                TestContext.WriteLine($"Genre Status: {genreResponse.StatusCode}");
                TestContext.WriteLine($"Genre Response: {genreContent}");

                Assert.That(genreResponse.IsSuccessStatusCode, Is.True, $"API key seems invalid: {genreContent}");
            }
        }

        [Test]
        public async Task Test_Discover_Endpoint_Variants()
        {
            using var client = new HttpClient();

            var testUrls = new[]
            {
                $"https://api.themoviedb.org/3/discover/movie?api_key={Utils.Config.ApiKey}",
                $"https://api.themoviedb.org/3/movie/popular?api_key={Utils.Config.ApiKey}",
                $"https://api.themoviedb.org/3/movie/now_playing?api_key={Utils.Config.ApiKey}",
                $"https://api.themoviedb.org/3/discover/movie?api_key={Utils.Config.ApiKey}&sort_by=release_date.asc",
            };

            foreach (var testUrl in testUrls)
            {
                TestContext.WriteLine($"\nTesting: {testUrl}");

                var response = await client.GetAsync(testUrl);
                var content = await response.Content.ReadAsStringAsync();

                TestContext.WriteLine($"Status: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    TestContext.WriteLine($"SUCCESS! Response length: {content.Length}");
                    TestContext.WriteLine($"First 200 chars: {content.Substring(0, Math.Min(200, content.Length))}");
                }
                else
                {
                    TestContext.WriteLine($"FAILED: {content.Substring(0, Math.Min(200, content.Length))}");
                }
            }

            // At least one should work
            bool hasWorkingEndpoint = false;
            foreach (var testUrl in testUrls)
            {
                var response = await client.GetAsync(testUrl);
                if (response.IsSuccessStatusCode)
                {
                    hasWorkingEndpoint = true;
                    break;
                }
            }

            Assert.That(hasWorkingEndpoint, Is.True, "At least one API endpoint should work");
        }
    } // End of SanityTests class
} // End of namespace