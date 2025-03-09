using System.Text.Json;
using Microsoft.Extensions.Options;
using TMDbLib.Client;
using TMDbLib.Objects.Search;
using MovieRecommender.Models;
using MovieRecommender.Config;
using Newtonsoft.Json;
using Microsoft.AspNetCore.WebUtilities;

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

        private static class TmdbQueryParams
        {
            public const string Page = "page";
            public const string IncludeAdult = "include_adult";
            public const string VoteCountMin = "vote_count.gte";
            public const string ReleaseDateMin = "primary_release_date.gte";
            public const string ReleaseDateMax = "primary_release_date.lte";
            public const string Genres = "with_genres";
            public const string VoteAverageMin = "vote_average.gte";
            public const string SortBy = "sort_by";
            public const string SortByRating = "vote_average.desc";
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

                    Dictionary<string, string> queryParams = new Dictionary<string, string>
                    {
                        [TmdbQueryParams.Page] = page.ToString(),
                        [TmdbQueryParams.IncludeAdult] = "false",
                        [TmdbQueryParams.VoteCountMin] = "100"
                    };

                    //Build discover API URL with all parameters
                    if (filters.StartYear.HasValue)
                    {
                        queryParams[TmdbQueryParams.ReleaseDateMin] = $"{filters.StartYear}-01-01";
                    }
                    if (filters.EndYear.HasValue)
                    {
                        queryParams[TmdbQueryParams.ReleaseDateMax] = $"{filters.EndYear}-12-31";
                    }
                    if (selectedGenreIds.Any())
                    {
                        queryParams[TmdbQueryParams.Genres] = string.Join(",", selectedGenreIds);
                    }
                    if (filters.MinimumRating.HasValue)
                    {
                        queryParams[TmdbQueryParams.VoteAverageMin] = filters.MinimumRating.ToString();
                    }

                    var baseUrl = $"{MovieDbSettings.BaseAPIUrl}{_client.ApiKey}";
                    var url = QueryHelpers.AddQueryString(baseUrl, queryParams);
                    url = QueryHelpers.AddQueryString(url, TmdbQueryParams.SortBy, TmdbQueryParams.SortByRating);

                    // Get movies sorted by rating
                    var ratingResponse = await _httpClient.GetAsync(url);
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
                var result = allMovies
                    .GroupBy(m => m.Id)
                    .Select(g => g.First())
                    .Where(m => !filters.MinimumRating.HasValue || m.VoteAverage >= filters.MinimumRating)
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