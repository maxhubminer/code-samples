using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MovieAdvisor.Core.Model;
using MovieAdvisor.Core.Repository;
using System.Threading;

namespace MovieAdvisor.Core.Service
{    
    public class MovieAdvisorDataService
    {
        static MovieAdvisorRepository repo = new MovieAdvisorRepository();

        public MovieAdvisorDataService(string language)
        {
            SetLanguage(language);
        }

        public void SetLanguage(string language)
        {
            repo.SetLanguage(language);
        }

        public List<Movie> GetAllMovies()
        {
            return repo.GetAllMovies();
        }

        public Movie GetMovieById(int id)
        {
            return repo.GetMovieById(id);
        }

        public string GetImageUrl(string imagePath, int width = 154)
        {
            return repo.GetImageUrl(imagePath, width);
        }

        public async Task<List<Movie>> SearchMovie(string query, int page, CancellationToken ct)
        {
            return await repo.SearchMovie(query, page, ct);
        }

        public async Task<List<Movie>> GetAdvisedMovies(Movie baseMovie, CancellationToken ct)
        {
            return await repo.GetAdvisedMovies(baseMovie, ct);
        }
    }
}
