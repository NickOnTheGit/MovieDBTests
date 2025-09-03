using System.Net.Http;
using System.Threading.Tasks;
using System;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace MovieDBTests.API
{
    public class MovieApi
    {
        private readonly HttpClient _http;

        public MovieApi(HttpClient? httpClient = null)
        {
            _http = httpClient ?? new HttpClient { BaseAddress = new Uri(Utils.Config.ApiBaseUrl) };
        }

        public async Task<JArray> DiscoverAsync(string sortBy = "primary_release_date.asc", string? withGenres = null, string? from = null, string? to = null, int page = 1)
        {
            var url = $"/discover/movie?api_key={Utils.Config.ApiKey}&sort_by={Uri.EscapeDataString(sortBy)}&page={page}";
            if (!string.IsNullOrWhiteSpace(withGenres)) url += $"&with_genres={Uri.EscapeDataString(withGenres)}";
            if (!string.IsNullOrWhiteSpace(from)) url += $"&primary_release_date.gte={Uri.EscapeDataString(from)}";
            if (!string.IsNullOrWhiteSpace(to)) url += $"&primary_release_date.lte={Uri.EscapeDataString(to)}";

            var resp = await _http.GetAsync(url);
            resp.EnsureSuccessStatusCode();
            var json = JObject.Parse(await resp.Content.ReadAsStringAsync());
            return (JArray)json["results"]!;
        }

        public async Task<List<JObject>> DiscoverAllPagesAsync(string sortBy, string? withGenres, string? from, string? to, int maxPages = 3)
        {
            var all = new List<JObject>();
            for (int p = 1; p <= maxPages; p++)
            {
                var arr = await DiscoverAsync(sortBy, withGenres, from, to, p);
                foreach (var item in arr) all.Add((JObject)item);
                if (arr.Count == 0) break;
            }
            return all;
        }
    }
}
