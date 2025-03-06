namespace MovieRecommender.Config
{
    public class MovieDbSettings
    {
        public required string ApiKey { get; set; }
        public static string ImageBaseUrl { get; set; } = "https://image.tmdb.org/t/p/";

        public static string GetPosterUrl(string posterPath, string size = "w500")
        {
            return $"{ImageBaseUrl}{size}{posterPath}";
        }
    }
}