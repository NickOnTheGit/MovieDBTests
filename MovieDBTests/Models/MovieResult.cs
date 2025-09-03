using Newtonsoft.Json;

namespace MovieDBTests.Models
{
    public class MovieResult
    {
        [JsonProperty("title")]
        public string Title { get; set; } = "";
        [JsonProperty("release_date")]
        public string? ReleaseDate { get; set; }
        [JsonProperty("genre_ids")]
        public int[] GenreIds { get; set; } = new int[0];
        [JsonProperty("vote_average")]
        public double VoteAverage { get; set; }
    }
}
