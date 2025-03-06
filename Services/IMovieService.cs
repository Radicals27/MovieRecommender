using MovieRecommender.Models;

namespace MovieRecommender.Services
{
    public interface IMovieService
    {
        /// <summary>
        /// Discovers movies based on the provided filters
        /// </summary>
        /// <param name="filters">The filter criteria for movies</param>
        /// <returns>A collection of movies matching the filter criteria</returns>
        Task<IReadOnlyCollection<Movie>> DiscoverMoviesAsync(MovieFilterViewModel filters);
    }
}