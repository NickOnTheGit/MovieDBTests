using System.Net.Http;
using System.Threading.Tasks;
using System;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using System.Net.Http.Headers;

namespace MovieDBTests.API
{
    public class MovieApi
    {
        private readonly HttpClient _http;

        public MovieApi(HttpClient? httpClient = null)
        {
            _http = httpClient ?? new HttpClient();

            // Set up authentication - prefer Bearer token if available
            if (!string.IsNullOrWhiteSpace(Utils.Config.BearerToken))
            {
                _http.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", Utils.Config.BearerToken);
            }
        }

        public async Task<JArray> DiscoverAsync(string sortBy = "primary_release_date.asc", string? withGenres = null, string? from = null, string? to = null, int page = 1)
        {
            var url = BuildDiscoverUrl(sortBy, withGenres, from, to, page);

            TestContext.WriteLine($"API Request: {url}");

            try
            {
                var resp = await _http.GetAsync(url);
                var content = await resp.Content.ReadAsStringAsync();

                TestContext.WriteLine($"API Response Status: {resp.StatusCode}");

                if (!resp.IsSuccessStatusCode)
                {
                    TestContext.WriteLine($"API Error Response: {content}");

                    // Try alternative endpoints if discover fails
                    return await TryAlternativeEndpoints(page);
                }

                var json = JObject.Parse(content);
                var results = (JArray)json["results"]!;

                // Apply client-side filtering if needed
                results = ApplyClientSideFiltering(results, withGenres, from, to, sortBy);

                TestContext.WriteLine($"API returned {results.Count} results");
                return results;
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"API Exception: {ex.Message}");
                throw;
            }
        }

        private string BuildDiscoverUrl(string sortBy, string? withGenres, string? from, string? to, int page)
        {
            var baseUrl = "https://api.themoviedb.org/3/discover/movie";
            var queryParams = new List<string>();

            // Add API key if Bearer token is not available
            if (string.IsNullOrWhiteSpace(Utils.Config.BearerToken))
            {
                queryParams.Add($"api_key={Utils.Config.ApiKey}");
            }

            queryParams.Add($"sort_by={Uri.EscapeDataString(sortBy)}");
            queryParams.Add($"page={page}");

            if (!string.IsNullOrWhiteSpace(withGenres))
                queryParams.Add($"with_genres={Uri.EscapeDataString(withGenres)}");

            if (!string.IsNullOrWhiteSpace(from))
                queryParams.Add($"primary_release_date.gte={Uri.EscapeDataString(from)}");

            if (!string.IsNullOrWhiteSpace(to))
                queryParams.Add($"primary_release_date.lte={Uri.EscapeDataString(to)}");

            return $"{baseUrl}?{string.Join("&", queryParams)}";
        }

        private async Task<JArray> TryAlternativeEndpoints(int page)
        {
            var fallbackUrls = new[]
            {
                BuildFallbackUrl("movie/popular", page),
                BuildFallbackUrl("movie/now_playing", page),
                BuildFallbackUrl("movie/top_rated", page)
            };

            foreach (var fallbackUrl in fallbackUrls)
            {
                try
                {
                    TestContext.WriteLine($"Trying fallback: {fallbackUrl}");
                    var resp = await _http.GetAsync(fallbackUrl);

                    if (resp.IsSuccessStatusCode)
                    {
                        var content = await resp.Content.ReadAsStringAsync();
                        var json = JObject.Parse(content);
                        return (JArray)json["results"]!;
                    }
                }
                catch (Exception ex)
                {
                    TestContext.WriteLine($"Fallback failed: {ex.Message}");
                }
            }

            throw new Exception("All API endpoints failed");
        }

        private string BuildFallbackUrl(string endpoint, int page)
        {
            var queryParams = new List<string>();

            if (string.IsNullOrWhiteSpace(Utils.Config.BearerToken))
            {
                queryParams.Add($"api_key={Utils.Config.ApiKey}");
            }

            queryParams.Add($"page={page}");

            return $"https://api.themoviedb.org/3/{endpoint}?{string.Join("&", queryParams)}";
        }

        private JArray ApplyClientSideFiltering(JArray results, string? withGenres, string? from, string? to, string sortBy)
        {
            var filtered = new List<JObject>();

            foreach (var item in results.Cast<JObject>())
            {
                if (PassesFilters(item, withGenres, from, to))
                {
                    filtered.Add(item);
                }
            }

            // Sort if needed
            if (sortBy.Contains("primary_release_date"))
            {
                var ascending = sortBy.Contains("asc");
                filtered = filtered.OrderBy(obj =>
                {
                    var dateStr = obj.Value<string>("release_date");
                    if (DateTime.TryParse(dateStr, out var date))
                        return ascending ? date : DateTime.MaxValue.Subtract(date.Subtract(DateTime.MinValue));
                    return ascending ? DateTime.MaxValue : DateTime.MinValue;
                }).ToList();
            }

            return new JArray(filtered);
        }

        private bool PassesFilters(JObject movie, string? withGenres, string? from, string? to)
        {
            // Date filtering
            var releaseDateStr = movie.Value<string>("release_date");
            if (!string.IsNullOrWhiteSpace(releaseDateStr) && DateTime.TryParse(releaseDateStr, out var releaseDate))
            {
                if (!string.IsNullOrWhiteSpace(from) && DateTime.TryParse(from, out var fromDate))
                {
                    if (releaseDate < fromDate) return false;
                }

                if (!string.IsNullOrWhiteSpace(to) && DateTime.TryParse(to, out var toDate))
                {
                    if (releaseDate > toDate) return false;
                }
            }

            // Genre filtering
            if (!string.IsNullOrWhiteSpace(withGenres))
            {
                var genreIds = withGenres.Split(',').Select(g => int.TryParse(g.Trim(), out var id) ? id : -1).Where(id => id != -1).ToList();
                var movieGenres = movie.Value<JArray>("genre_ids")?.ToObject<int[]>() ?? new int[0];

                if (genreIds.Any() && !genreIds.Any(gId => movieGenres.Contains(gId)))
                {
                    return false;
                }
            }

            return true;
        }

        public async Task<List<JObject>> DiscoverAllPagesAsync(string sortBy, string? withGenres, string? from, string? to, int maxPages = 3)
        {
            var all = new List<JObject>();

            for (int p = 1; p <= maxPages; p++)
            {
                try
                {
                    var arr = await DiscoverAsync(sortBy, withGenres, from, to, p);

                    if (arr.Count == 0) break;

                    foreach (var item in arr.Cast<JObject>())
                    {
                        all.Add(item);
                    }
                }
                catch (Exception ex)
                {
                    TestContext.WriteLine($"Error fetching page {p}: {ex.Message}");
                    break;
                }
            }

            TestContext.WriteLine($"Total results across all pages: {all.Count}");
            return all;
        }
    }
}