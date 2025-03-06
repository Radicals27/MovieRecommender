namespace MovieRecommender.Config
{
    public class MovieDbSettings
    {
        public required string ApiKey { get; set; }
        public static string BaseAPIUrl = "https://api.themoviedb.org/3/discover/movie?api_key=";

        public static string ImageBaseUrl { get; set; } = "https://image.tmdb.org/t/p/";

        public static string GetPosterUrl(string posterPath, string size = "w500")
        {
            return $"{ImageBaseUrl}{size}{posterPath}";
        }

        public static IReadOnlyCollection<string> AllGenres = new[]
        {
            "Action", "Adventure", "Animation", "Comedy", "Crime",
            "Documentary", "Drama", "Family", "Fantasy", "History",
            "Horror", "Music", "Mystery", "Romance", "Science Fiction",
            "Thriller", "War", "Western"
        };
    }
}