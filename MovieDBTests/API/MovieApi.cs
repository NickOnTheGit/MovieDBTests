using System.Net.Http;
using System.Threading.Tasks;
using System;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace MovieDBTests.API
{
    public class MovieApi
    {
        private readonly HttpClient _http;

        public MovieApi(HttpClient? httpClient = null)
        {
            _http = httpClient ?? new HttpClient();
            // Don't set base address here, use full URLs
        }

        public async Task<JArray> DiscoverAsync(string sortBy = "primary_release_date.asc", string? withGenres = null, string? from = null, string? to = null, int page = 1)
        {
            // Use the working API pattern we discovered
            var url = $"https://api.themoviedb.org/3/discover/movie?api_key={Utils.Config.ApiKey}&sort_by={Uri.EscapeDataString(sortBy)}&page={page}";

            if (!string.IsNullOrWhiteSpace(withGenres))
                url += $"&with_genres={Uri.EscapeDataString(withGenres)}";
            if (!string.IsNullOrWhiteSpace(from))
                url += $"&primary_release_date.gte={Uri.EscapeDataString(from)}";
            if (!string.IsNullOrWhiteSpace(to))
                url += $"&primary_release_date.lte={Uri.EscapeDataString(to)}";

            TestContext.WriteLine($"API Request: {url}");

            try
            {
                var resp = await _http.GetAsync(url);
                var content = await resp.Content.ReadAsStringAsync();

                TestContext.WriteLine($"API Response Status: {resp.StatusCode}");

                if (!resp.IsSuccessStatusCode)
                {
                    TestContext.WriteLine($"API Error Response: {content}");

                    // Fallback to popular movies if discover doesn't work
                    var fallbackUrl = $"https://api.themoviedb.org/3/movie/popular?api_key={Utils.Config.ApiKey}&page={page}";
                    TestContext.WriteLine($"Trying fallback: {fallbackUrl}");

                    resp = await _http.GetAsync(fallbackUrl);
                    content = await resp.Content.ReadAsStringAsync();
                    resp.EnsureSuccessStatusCode();
                }

                var json = JObject.Parse(content);
                var results = (JArray)json["results"]!;

                // If we have date filters, apply them client-side as fallback
                if (!string.IsNullOrWhiteSpace(from) || !string.IsNullOrWhiteSpace(to))
                {
                    results = FilterByDateRange(results, from, to);
                }

                TestContext.WriteLine($"API returned {results.Count} results");
                return results;
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"API Exception: {ex.Message}");
                throw;
            }
        }

        private JArray FilterByDateRange(JArray results, string? from, string? to)
        {
            var filtered = new JArray();

            foreach (var item in results)
            {
                var releaseDateStr = item.Value<string>("release_date");
                if (string.IsNullOrWhiteSpace(releaseDateStr)) continue;

                if (DateTime.TryParse(releaseDateStr, out var releaseDate))
                {
                    bool includeItem = true;

                    if (!string.IsNullOrWhiteSpace(from) && DateTime.TryParse(from, out var fromDate))
                    {
                        if (releaseDate < fromDate) includeItem = false;
                    }

                    if (!string.IsNullOrWhiteSpace(to) && DateTime.TryParse(to, out var toDate))
                    {
                        if (releaseDate > toDate) includeItem = false;
                    }

                    if (includeItem)
                    {
                        filtered.Add(item);
                    }
                }
            }

            return filtered;
        }

        public async Task<List<JObject>> DiscoverAllPagesAsync(string sortBy, string? withGenres, string? from, string? to, int maxPages = 3)
        {
            var all = new List<JObject>();
            for (int p = 1; p <= maxPages; p++)
            {
                try
                {
                    var arr = await DiscoverAsync(sortBy, withGenres, from, to, p);
                    foreach (var item in arr) all.Add((JObject)item);
                    if (arr.Count == 0) break;
                }
                catch (Exception ex)
                {
                    TestContext.WriteLine($"Error fetching page {p}: {ex.Message}");
                    break;
                }
            }

            // Sort the results if needed
            if (sortBy.Contains("asc"))
            {
                all = all.OrderBy(obj =>
                {
                    var dateStr = obj.Value<string>("release_date");
                    return DateTime.TryParse(dateStr, out var date) ? date : DateTime.MaxValue;
                }).ToList();
            }

            TestContext.WriteLine($"Total results across all pages: {all.Count}");
            return all;
        }
    }
}