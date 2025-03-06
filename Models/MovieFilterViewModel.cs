namespace MovieRecommender.Models
{
    public class MovieFilterViewModel
    {
        public int? StartYear { get; set; }
        public int? EndYear { get; set; }
        public IReadOnlyCollection<string> SelectedGenres { get; set; } = Array.Empty<string>();
        public double? MinimumRating { get; set; }
        public IReadOnlyCollection<Movie> Movies { get; set; } = Array.Empty<Movie>();
    }
}