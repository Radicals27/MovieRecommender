using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text.Json;
using Microsoft.Extensions.Options;
using TMDbLib.Client;
using TMDbLib.Objects.General;
using TMDbLib.Objects.Search;
using MovieRecommender.Models;
using MovieRecommender.Config;

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
                }
            }

            return genreMap;
        }

        public async Task<List<Models.Movie>> DiscoverMoviesAsync(MovieFilterViewModel filters)
        {
            try
            {
                var allMovies = new List<SearchMovie>();
                var selectedGenreIds = new List<int>();

                // if (filters.SelectedGenres.Any())
                // {
                //     var selectedGenres = filters.SelectedGenres.Select(g => g).ToList();
                //     selectedGenreIds = _genreMap
                //         .Where(kvp => selectedGenres.Contains(kvp.Value))
                //         .Select(kvp => kvp.Key)
                //         .ToList();

                //     Console.WriteLine($"Selected genres: {string.Join(", ", selectedGenres)}");
                //     Console.WriteLine($"Selected genre IDs: {string.Join(", ", selectedGenreIds)}");
                // }

                // Get movies from different sources to ensure comprehensive results
                // for (int page = 1; page <= 5; page++)
                // {
                // Build discover API URL with all parameters
                var url = $"{BaseUrl}/discover/movie?api_key={_client.ApiKey}&";
                // url += $"page={page}&";
                // url += "include_adult=false&";
                // url += "vote_count.gte=100&";

                // if (filters.StartYear.HasValue)
                // {
                //     url += $"primary_release_date.gte={filters.StartYear}-01-01&";
                // }
                // if (filters.EndYear.HasValue)
                // {
                //     url += $"primary_release_date.lte={filters.EndYear}-12-31&";
                // }
                // if (selectedGenreIds.Any())
                // {
                //     url += $"with_genres={string.Join(",", selectedGenreIds)}&";
                // }
                // if (filters.MinimumRating.HasValue)
                // {
                //     url += $"vote_average.gte={filters.MinimumRating}&";
                // }

                // // Try rating sort
                // var ratingUrl = url + "sort_by=vote_average.desc";

                // Get movies sorted by rating
                var ratingResponse = await _httpClient.GetAsync(url);
                if (ratingResponse.IsSuccessStatusCode)
                {
                    var content = await ratingResponse.Content.ReadAsStringAsync();
                    var ratedMovies = JsonSerializer.Deserialize<SearchContainer<SearchMovie>>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (ratedMovies?.Results != null)
                    {
                        allMovies.AddRange(ratedMovies.Results);
                        Console.WriteLine($"Found {ratedMovies.Results.Count} movies from discover (rating sort)");
                    }
                }
                else
                {
                    Console.WriteLine($"Error in DiscoverMoviesAsync: {ratingResponse.ReasonPhrase}");
                }
                //}

                foreach (var movie in allMovies)
                {
                    Console.WriteLine($"- {movie.Title}");
                }

                Console.WriteLine($"Found {allMovies.Count} total movies before filtering");

                // Remove duplicates and apply filters
                var filteredMovies = allMovies
                    .GroupBy(m => m.Id)
                    .Select(g => g.First())
                    .Where(m => m != null)
                    .Where(m =>
                        (!filters.StartYear.HasValue || (m.ReleaseDate?.Year ?? 0) >= filters.StartYear) &&
                        (!filters.EndYear.HasValue || (m.ReleaseDate?.Year ?? 0) <= filters.EndYear) &&
                        (!filters.MinimumRating.HasValue || m.VoteAverage >= filters.MinimumRating) &&
                        (!selectedGenreIds.Any() ||
                         (m.GenreIds != null && selectedGenreIds.All(id => m.GenreIds.Contains(id)))));

                Console.WriteLine($"Found {filteredMovies.Count()} movies after filtering");

                // // Convert to our movie model and sort
                var result = filteredMovies
                    .Select(m => CreateMovie(m))
                    .OrderByDescending(m => m.VoteAverage)
                    .Take(50)
                    .ToList();

                Console.WriteLine($"Returning {result.Count} movies after processing");
                foreach (var movie in result)
                {
                    Console.WriteLine($"- {movie.Title} ({movie.ReleaseDate.Year}) - Rating: {movie.VoteAverage} - Genres: {string.Join(", ", movie.Genres)}");
                }

                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in DiscoverMoviesAsync: {ex}");
                return new List<Models.Movie>();
            }
        }

        private Models.Movie CreateMovie(SearchMovie m)
        {
            var genres = (m.GenreIds ?? Enumerable.Empty<int>())
                .Where(id => _genreMap.ContainsKey(id))
                .Select(id => _genreMap[id])
                .ToList();

            return new Models.Movie
            {
                Id = m.Id,
                Title = m.Title ?? "",
                Overview = m.Overview ?? "",
                PosterPath = m.PosterPath ?? "",
                ReleaseDate = m.ReleaseDate ?? DateTime.MinValue,
                VoteAverage = m.VoteAverage,
                Genres = genres
            };
        }
    }
}