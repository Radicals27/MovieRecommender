using MovieRecommender.Config;

namespace MovieRecommender.Models
{
    public class Movie
    {
        public int Id { get; set; }
        public required string Title { get; set; }
        public required string Overview { get; set; }
        public required string PosterPath { get; set; }
        public DateTime ReleaseDate { get; set; }
        public double VoteAverage { get; set; }
        public List<string> Genres { get; set; } = new();
        public string FullPosterPath => MovieDbSettings.GetPosterUrl(PosterPath);
    }
}