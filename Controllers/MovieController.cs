using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MovieRecommender.Models;
using MovieRecommender.Services;

namespace MovieRecommender.Controllers
{
    public class MovieController : Controller
    {
        private readonly IMovieService _movieService;

        public MovieController(IMovieService movieService)
        {
            _movieService = movieService;
        }

        public IActionResult Index()
        {
            var viewModel = new MovieFilterViewModel();
            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Index(MovieFilterViewModel filters)
        {
            if (ModelState.IsValid)
            {
                filters.Movies = await _movieService.DiscoverMoviesAsync(filters);
            }
            return View(filters);
        }
    }
}