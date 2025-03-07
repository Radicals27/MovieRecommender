using System.Text.Json;
using Microsoft.Extensions.Options;
using TMDbLib.Client;
using TMDbLib.Objects.Search;
using MovieRecommender.Models;
using MovieRecommender.Config;
using Newtonsoft.Json;

namespace MovieRecommender.Services
{
    public class TMDBMovieService : IMovieService
    {
        private readonly TMDbClient _client;
        private readonly HttpClient _httpClient;
        private Dictionary<int, string>? _genreMap;

        private bool _isInitialized = false;

        public TMDBMovieService(IOptions<MovieDbSettings> config)
        {
            _client = new TMDbClient(config.Value.ApiKey);
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
                }
            }

            return genreMap;
        }

        public class TmdbResponse
        {
            [JsonProperty("results")]
            public required List<TmdbMovie> Results { get; set; }
        }

        public class TmdbMovie
        {
            [JsonProperty("id")]
            public int Id { get; set; }

            [JsonProperty("title")]
            public required string Title { get; set; }

            [JsonProperty("overview")]
            public required string Overview { get; set; }

            [JsonProperty("poster_path")]
            public required string PosterPath { get; set; }

            [JsonProperty("release_date")]
            public required string ReleaseDate { get; set; }

            [JsonProperty("vote_average")]
            public double VoteAverage { get; set; }

            [JsonProperty("genre_ids")]
            public required List<int> GenreIds { get; set; }

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

        public async Task<IReadOnlyCollection<Movie>> DiscoverMoviesAsync(MovieFilterViewModel filters)
        {
            await InitializeAsync();

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
                }

                // Get movies from different sources to ensure comprehensive results
                for (int page = 1; page <= 5; page++)
                {
                    //Build discover API URL with all parameters
                    var url = $"{MovieDbSettings.BaseAPIUrl}{_client.ApiKey}&";

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

                        var tmdbResponse = JsonConvert.DeserializeObject<TmdbResponse>(content);
                        if (tmdbResponse?.Results != null)
                        {
                            var movies = tmdbResponse.Results.Select(m => m.ToSearchMovie()).ToList();
                            var sampleMovie = movies.FirstOrDefault();

                            allMovies.AddRange(movies);
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Error in DiscoverMoviesAsync: {ratingResponse.ReasonPhrase}");
                    }
                }

                // Remove duplicates first
                var dedupedMovies = allMovies
                    .GroupBy(m => m.Id)
                    .Select(g => g.First())
                    .Where(m => m != null)
                    .ToList();

                // Skip date filtering since it's already handled in the API call
                var ratingFiltered = dedupedMovies
                    .Where(m => !filters.MinimumRating.HasValue || m.VoteAverage >= filters.MinimumRating)
                    .ToList();

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

                return result.Select(sm => new Movie
                {
                    Id = sm.Id,
                    Title = sm.Title,
                    Overview = sm.Overview,
                    PosterPath = sm.PosterPath,
                    ReleaseDate = DateOnly.FromDateTime(sm.ReleaseDate ?? DateTime.MinValue),
                    VoteAverage = sm.VoteAverage,
                    Genres = sm.GenreIds?.Select(id => _genreMap.GetValueOrDefault(id, "Unknown")).ToList() ?? new List<string>()
                }).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in DiscoverMoviesAsync: {ex}");
                return new List<Movie>();
            }
        }

        private async Task InitializeAsync()
        {
            if (!_isInitialized)
            {
                _genreMap = await InitializeGenreMapAsync();
                _isInitialized = true;
            }
        }
    }
}