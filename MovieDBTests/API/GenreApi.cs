using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace MovieDBTests.API
{
    public class GenreApi
    {
        private readonly HttpClient _http;

        public GenreApi(HttpClient? client = null)
        {
            _http = client ?? new HttpClient { BaseAddress = new Uri(Utils.Config.ApiBaseUrl) };
        }

        public async Task<Dictionary<int, string>> GetGenresAsync()
        {
            var url = $"/genre/movie/list?api_key={Utils.Config.ApiKey}";
            var resp = await _http.GetAsync(url);
            resp.EnsureSuccessStatusCode();
            var json = JObject.Parse(await resp.Content.ReadAsStringAsync());
            var genres = new Dictionary<int, string>();
            foreach (var g in json["genres"]!)
            {
                genres[g.Value<int>("id")] = g.Value<string>("name")!;
            }
            return genres;
        }
    }
}
