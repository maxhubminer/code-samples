using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MovieAdvisor.Core.Model;
using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading;
using Java.IO;
using Android.OS;

namespace MovieAdvisor.Core.Repository
{
    public class MovieAdvisorRepository
    {
        static string apiUrl = "https://api.themoviedb.org/3";
        static string apiKey = "daaaee5cacd8e582269fbb7c04bed9fc";
        static string imageUrl = "https://image.tmdb.org/t/p";
        static List<Movie> movies = new List<Movie>();
        static string language;

        public void SetLanguage(string language)
        {
            MovieAdvisorRepository.language = language;
        }

        public string GetImageUrl(string imagePath, int width)
        {
            return imageUrl + "/w" + width + imagePath;
        }

        string BuildFinalURI(string action, string additionalParams = "")
        {
            var uri = String.Format("{0}{1}?api_key={2}&language={3}{4}",
                                    apiUrl, action, apiKey, language, additionalParams);
            return uri;
        }

        public async Task<List<Movie>> GetAdvisedMovies(Movie baseMovie, CancellationToken ct)
        {            
            // / movie /{ movie_id}/ recommendations
            var uri = BuildFinalURI("/movie/" + baseMovie.Id + "/recommendations");
            var responseJsonString = await LoadDataAsync(uri, ct);
            JToken token = JObject.Parse(responseJsonString);
            JArray results = (JArray)token.SelectToken("results");
            List<Movie> movies = results.ToObject<List<Movie>>();
            return movies;
        }

        public async Task<List<Movie>> SearchMovie(string query, int page, CancellationToken ct)
        {            
            var uri = BuildFinalURI("/search/movie", "&page=" + page + "&query=" + query);
            var responseJsonString = await LoadDataAsync(uri, ct);
            if(null == responseJsonString)
            {
                return null;
            }
            JToken token = JObject.Parse(responseJsonString);
            JArray results = (JArray)token.SelectToken("results");

            //var jSerializer = JsonSerializer.CreateDefault();
            //jSerializer.Converters.Add(new Newtonsoft.Json.Converters.KeyValuePairConverter());
            //jSerializer.Converters.Add(new Newtonsoft.Json.Converters.CustomCreationConverter);

            
            List<Movie> movies; // DECLARATION MUST BE OUT OF TRY SCOPE!!! Otherwise, we'll get null on output every time
            try
            {
                var jSerializer = JsonSerializer.Create();
                jSerializer.MissingMemberHandling = MissingMemberHandling.Ignore;
                jSerializer.NullValueHandling = NullValueHandling.Ignore;
                movies = results.ToObject<List<Movie>>(jSerializer);
                //foreach (JToken result in results)
                //{
                //    Movie _movie = result.ToObject<Movie>();
                //    movies.Add(_movie);
                //}
                // hell knows why this does not work anymore...
                ////
                //List<Movie> movies = results.ToObject<List<Movie>>(jSerializer);
            }
            catch (Exception e)
            {
                movies = null;
                string errorDescription = e.Message;
            }
            return movies;
        }

        private async Task<string> LoadDataAsync(string uri, CancellationToken ct)
        {
            string responseJsonString = null;

            if (movies != null)
            {
                using (var httpClient = new HttpClient())
                {
                    try
                    {
                        Task<HttpResponseMessage> getResponse = httpClient.GetAsync(uri, ct);

                        HttpResponseMessage response = await getResponse;

                        responseJsonString = await response.Content.ReadAsStringAsync();

                        return responseJsonString;

                    }
                    catch (System.OperationCanceledException ex)
                    {
                        string message = ex.Message;
                        throw new System.OperationCanceledException();
                    }
                    catch (Exception ex)
                    {
                        //handle any errors here, not part of the sample app
                        string message = ex.Message;
                    }
                }
            }

            return responseJsonString;
        }

        public List<Movie> GetAllMovies()
        {
            IEnumerable<Movie> allMovies =
                from m in movies
                select m;
            return allMovies.ToList();
        }

        public Movie GetMovieById(int id)
        {
            return movies.Where(m => m.Id == id).SingleOrDefault<Movie>();
        }
    }
}
