using System.Text.Json;
using Microsoft.Extensions.Options;
using TMDbLib.Client;
using TMDbLib.Objects.General;
using TMDbLib.Objects.Search;
using MovieRecommender.Models;
using MovieRecommender.Config;
using Newtonsoft.Json;

namespace MovieRecommender.Services
{
    public class MovieService
    {
        private readonly TMDbClient _client;
        private readonly Dictionary<int, string> _genreMap;
        private readonly HttpClient _httpClient;
        private const string BaseUrl = "https://api.themoviedb.org/3";

        public MovieService(IOptions<MovieDbSettings> config)
        {
            _client = new TMDbClient(config.Value.ApiKey);
            _genreMap = InitializeGenreMapAsync().GetAwaiter().GetResult();
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        }

        private async Task<Dictionary<int, string>> InitializeGenreMapAsync()
        {
            var genreMap = new Dictionary<int, string>();
            var genres = await _client.GetMovieGenresAsync();

            if (genres != null)
            {
                foreach (var genre in genres)
                {
                    genreMap[genre.Id] = genre.Name;
                    Console.WriteLine($"Added genre mapping: {genre.Id} -> {genre.Name}");
                }
            }

            return genreMap;
        }

        public class TmdbResponse
        {
            [JsonProperty("results")]
            public List<TmdbMovie> Results { get; set; }
        }

        public class TmdbMovie
        {
            [JsonProperty("id")]
            public int Id { get; set; }

            [JsonProperty("title")]
            public string Title { get; set; }

            [JsonProperty("overview")]
            public string Overview { get; set; }

            [JsonProperty("poster_path")]
            public string PosterPath { get; set; }

            [JsonProperty("release_date")]
            public string ReleaseDate { get; set; }

            [JsonProperty("vote_average")]
            public double VoteAverage { get; set; }

            [JsonProperty("genre_ids")]
            public List<int> GenreIds { get; set; }

            public SearchMovie ToSearchMovie()
            {
                DateTime? releaseDate = null;
                if (DateTime.TryParse(ReleaseDate, out DateTime parsedDate))
                {
                    releaseDate = parsedDate;
                }

                return new SearchMovie
                {
                    Id = Id,
                    Title = Title,
                    Overview = Overview,
                    PosterPath = PosterPath,
                    ReleaseDate = releaseDate,
                    VoteAverage = VoteAverage,
                    GenreIds = GenreIds ?? new List<int>()
                };
            }
        }

        public async Task<List<SearchMovie>> DiscoverMoviesAsync(MovieFilterViewModel filters)
        {
            try
            {
                var allMovies = new List<SearchMovie>();
                var selectedGenreIds = new List<int>();

                if (filters.SelectedGenres.Any())
                {
                    var selectedGenres = filters.SelectedGenres.Select(g => g).ToList();
                    selectedGenreIds = _genreMap
                        .Where(kvp => selectedGenres.Contains(kvp.Value))
                        .Select(kvp => kvp.Key)
                        .ToList();

                    Console.WriteLine($"Selected genres: {string.Join(", ", selectedGenres)}");
                    Console.WriteLine($"Selected genre IDs: {string.Join(", ", selectedGenreIds)}");
                }

                // Get movies from different sources to ensure comprehensive results
                for (int page = 1; page <= 5; page++)
                {
                    //Build discover API URL with all parameters
                    var url = $"{BaseUrl}/discover/movie?api_key={_client.ApiKey}&";

                    url += $"page={page}&";
                    url += "include_adult=false&";
                    url += "vote_count.gte=100&";

                    if (filters.StartYear.HasValue)
                    {
                        url += $"primary_release_date.gte={filters.StartYear}-01-01&";
                    }
                    if (filters.EndYear.HasValue)
                    {
                        url += $"primary_release_date.lte={filters.EndYear}-12-31&";
                    }
                    if (selectedGenreIds.Any())
                    {
                        url += $"with_genres={string.Join(",", selectedGenreIds)}&";
                    }
                    if (filters.MinimumRating.HasValue)
                    {
                        url += $"vote_average.gte={filters.MinimumRating}&";
                    }

                    // Try rating sort
                    var ratingUrl = url + "sort_by=vote_average.desc";

                    // Get movies sorted by rating
                    var ratingResponse = await _httpClient.GetAsync(ratingUrl);
                    if (ratingResponse.IsSuccessStatusCode)
                    {
                        var content = await ratingResponse.Content.ReadAsStringAsync();
                        Console.WriteLine($"Raw API Response: {content.Substring(0, Math.Min(500, content.Length))}...");

                        var tmdbResponse = JsonConvert.DeserializeObject<TmdbResponse>(content);
                        if (tmdbResponse?.Results != null)
                        {
                            var movies = tmdbResponse.Results.Select(m => m.ToSearchMovie()).ToList();
                            var sampleMovie = movies.FirstOrDefault();
                            if (sampleMovie != null)
                            {
                                Console.WriteLine($"Sample deserialized movie:");
                                Console.WriteLine($"- Title: {sampleMovie.Title}");
                                Console.WriteLine($"- Release Date: {sampleMovie.ReleaseDate}");
                                Console.WriteLine($"- Vote Average: {sampleMovie.VoteAverage}");
                                Console.WriteLine($"- Genre IDs: {string.Join(", ", sampleMovie.GenreIds ?? new List<int>())}");
                            }

                            allMovies.AddRange(movies);
                            Console.WriteLine($"Found {movies.Count} movies from discover (rating sort)");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Error in DiscoverMoviesAsync: {ratingResponse.ReasonPhrase}");
                    }
                }

                foreach (var movie in allMovies)
                {
                    Console.WriteLine($"- {movie.Title}");
                }

                Console.WriteLine($"Found {allMovies.Count} total movies before filtering");

                // Remove duplicates first
                var dedupedMovies = allMovies
                    .GroupBy(m => m.Id)
                    .Select(g => g.First())
                    .Where(m => m != null)
                    .ToList();

                Console.WriteLine($"Found {dedupedMovies.Count} movies after removing duplicates");

                // Skip date filtering since it's already handled in the API call
                var ratingFiltered = dedupedMovies
                    .Where(m => !filters.MinimumRating.HasValue || m.VoteAverage >= filters.MinimumRating)
                    .ToList();

                Console.WriteLine($"Found {ratingFiltered.Count} movies after rating filtering");

                // Return the filtered movies sorted by rating
                var result = ratingFiltered
                    .OrderByDescending(m => m.VoteAverage)
                    .Take(50)
                    .ToList();

                // Map genres using our genre map
                foreach (var movie in result)
                {
                    if (movie.GenreIds != null)
                    {
                        movie.GenreIds = movie.GenreIds
                            .Where(id => _genreMap.ContainsKey(id))
                            .ToList();
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in DiscoverMoviesAsync: {ex}");
                return new List<SearchMovie>();
            }
        }
    }
}