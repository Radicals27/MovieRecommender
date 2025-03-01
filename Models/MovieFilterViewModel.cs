using System.Collections.Generic;
using TMDbLib.Objects.Search;

namespace MovieRecommender.Models
{
    public class MovieFilterViewModel
    {
        public int? StartYear { get; set; }
        public int? EndYear { get; set; }
        public List<string> SelectedGenres { get; set; }
        public double? MinimumRating { get; set; }
        public List<SearchMovie> Movies { get; set; }

        public MovieFilterViewModel()
        {
            SelectedGenres = new List<string>();
            Movies = new List<SearchMovie>();
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