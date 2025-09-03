using System;
using System.IO;
using Newtonsoft.Json.Linq;

namespace MovieDBTests.Utils
{
    public static class Config
    {
        private static readonly JObject _config;

        static Config()
        {
            // Prefer environment variables when present
            var apiKeyEnv = Environment.GetEnvironmentVariable("TMDB_API_KEY");

            var path = Path.Combine(AppContext.BaseDirectory, "Config", "appsettings.json");
            if (!File.Exists(path))
                throw new FileNotFoundException($"Config file not found at {path}");

            var json = JObject.Parse(File.ReadAllText(path));

            if (!string.IsNullOrWhiteSpace(apiKeyEnv))
            {
                json["API"]!["ApiKey"] = apiKeyEnv;
            }

            _config = json;
        }

        public static string DiscoverUrl => _config["UI"]!["DiscoverUrl"]!.ToString();
        public static string ApiBaseUrl => _config["API"]!["BaseUrl"]!.ToString();
        public static string ApiKey => _config["API"]!["ApiKey"]!.ToString();
        public static bool Headless => bool.TryParse(_config["Browser"]!["Headless"]!.ToString(), out var h) && h;
    }
}
