using System.Collections.Generic;

namespace MovieRecommender.Models
{
    public class MovieFilterViewModel
    {
        public int? StartYear { get; set; }
        public int? EndYear { get; set; }
        public List<string> SelectedGenres { get; set; }
        public double? MinimumRating { get; set; }
        public List<Movie> Movies { get; set; }

        public MovieFilterViewModel()
        {
            SelectedGenres = new List<string>();
            Movies = new List<Movie>();
        }

        public static readonly List<string> AllGenres = new List<string>
        {
            "Action", "Adventure", "Animation", "Comedy", "Crime",
            "Documentary", "Drama", "Family", "Fantasy", "History",
            "Horror", "Music", "Mystery", "Romance", "Science Fiction",
            "Thriller", "War", "Western"
        };
    }
}