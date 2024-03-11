using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Assignment3.Data;
using Assignment3.Models;
using System.Numerics;
using System.Text.Json;
using VaderSharp2;

namespace Assignment3.Controllers
{
    public class MoviesController : Controller
    {
        public async Task<IActionResult> GetMoviePhoto(int id)
        {
            var movie = await _context.Movie
                .FirstOrDefaultAsync(m => m.Id == id);
            if (movie == null)
            {
                return NotFound();
            }
            var imageData = movie.MovieImage;
            return File(imageData, "image/jpg");
        }

        private readonly ApplicationDbContext _context;

        public MoviesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Movies
        public async Task<IActionResult> Index()
        {
            return View(await _context.Movie.ToListAsync());
        }
        // START NEW CODE
        public static readonly HttpClient client = new HttpClient();
       
        public static async Task<List<string>> SearchWikipediaAsync(string searchQuery)
        {
            string baseUrl = "https://en.wikipedia.org/w/api.php";
            string url = $"{baseUrl}?action=query&list=search&srlimit=100&srsearch={Uri.EscapeDataString(searchQuery)}&format=json";
            List<string> textToExamine = new List<string>();
            try
            {
                //Ask WikiPedia for a list of pages that relate to the query
                HttpResponseMessage response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();
                var jsonDocument = JsonDocument.Parse(responseBody);
                var searchResults = jsonDocument.RootElement.GetProperty("query").GetProperty("search");
                foreach (var item in searchResults.EnumerateArray())
                {
                    var pageId = item.GetProperty("pageid").ToString();
                    //Ask WikiPedia for the text of each page in the query results
                    string pageUrl = $"{baseUrl}?action=query&pageids={pageId}&prop=extracts&explaintext&format=json";
                    HttpResponseMessage pageResponse = await client.GetAsync(pageUrl);
                    pageResponse.EnsureSuccessStatusCode();
                    string pageResponseBody = await pageResponse.Content.ReadAsStringAsync();
                    var jsonPageDocument = JsonDocument.Parse(pageResponseBody);
                    var pageContent = jsonPageDocument.RootElement.GetProperty("query").GetProperty("pages").GetProperty(pageId).GetProperty("extract").GetString();
                    textToExamine.Add(pageContent);
                }
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine("\nException Caught!");
                Console.WriteLine("Message :{0} ", e.Message);
            }
            return textToExamine;
        }
        // END NEW CODE

        // GET: Movies/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var movie = await _context.Movie
                .FirstOrDefaultAsync(m => m.Id == id);
            if (movie == null)
            {
                return NotFound();
            }
            

            // var queryText = movie.Description;
            // var json = "";
            // using (WebClient wc = new WebClient())
            // {
            //     wc.Headers.Add("user-agent", "Mozilla/4.0 (compatible; MSIE 6.0; Windows");
            //     json = wc.DownloadString("https://www.reddit.com/search.json?limit=100"); // add reddit api here!!
            // }
            //
            // var textToExamine = new List<string>();
            // JsonDocument doc = JsonDocument.Parse(json);
            // JsonElement dataElement = doc.RootElement.GetProperty("data");
            // JsonElement childrenElement = dataElement.GetProperty("children");
            //
            // foreach (JsonElement child in childrenElement.EnumerateArray())
            // {
            //     if (child.TryGetProperty("data", out JsonElement data))
            //     {
            //         if (data.TryGetProperty("selftext", out JsonElement selftext))
            //         {
            //             string selftextValue = selftext.GetString();
            //             if (!string.IsNullOrEmpty(selftextValue))
            //             {
            //                 textToExamine.Add((selftextValue));
            //             } else if (data.TryGetProperty("title", out JsonElement title))
            //             {
            //                 string titleValue = title.GetString();
            //                 if (!string.IsNullOrEmpty(titleValue)) {textToExamine.Add(titleValue);}
            //             }
            //         }
            //     }
            // }
            //
            
            // RECENTLY DELETED
            MovieDetailsVM adVM = new MovieDetailsVM();
            adVM.movie = movie;
            
            MovieWikiVM wikiVm = new MovieWikiVM();
            wikiVm.oldModel = adVM;

            PostRating newPost = new PostRating();
            
            var actors = new List<Actor>();
            //Get the text from WikiPedia related to the Pet description
            List<string> textToExamine = await SearchWikipediaAsync(movie.Description);
            newPost.text = textToExamine;
            
            
             // = await SearchWikipediaAsync((movie.Description));
            
            var analyzer = new SentimentIntensityAnalyzer();
            int validResults = 0;
            double resultsTotal = 0;
            List<double> tempList = new List<double>();
            
            foreach (string textValue in textToExamine)
            {
                var results = analyzer.PolarityScores(textValue);
                
                if (results.Compound != 0)
                {
                    tempList.Add(results.Compound);
                    resultsTotal += results.Compound;
                    validResults++;
                }
            }

            newPost.sentiment = tempList;
            
            double avgResult = Math.Round(resultsTotal / validResults, 2);
            adVM.sentiment = avgResult.ToString() + ", " + CategorizeSentiment(avgResult);

            actors = await (from at in _context.Actor
                            join am in _context.ActorMovie on at.Id equals am.Id
                            where am.Id == id
                            select at).ToListAsync();
            adVM.actors = actors;

            adVM.postRatings = newPost;


            return View(adVM);
        }

        public static string CategorizeSentiment(double sentiment)
        {
            if (sentiment >= -1 && sentiment < -0.6)
            {
                return "Extremely Negative";
            } else if (sentiment >= -.7 && sentiment < -0.2)
            {
                return "Very Negative";
            } else if (sentiment >= -.2 && sentiment < 0)
            {
                return "Slightly Negative";
            }  else if (sentiment >= 0 && sentiment < 0.2)
            {
                return "Slightly Positive";
            }  else if (sentiment >= 0.2 && sentiment < 0.6)
            {
                return "Very Positive";
            }  else if (sentiment >= 0.6 && sentiment < 1)
            {
                return "Extremely Positive";
            }
            else
            {
                return "Invalid Sentiment Value";
            }
        }

        // GET: Movies/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Movies/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Title,Description,Director,Genre,ReleaseDate,Hyperlink,MovieImage")] Movie movie, IFormFile MovieImage)
        {
            ModelState.Remove(nameof(movie.MovieImage));
            if (ModelState.IsValid)
            {
                if (MovieImage != null && MovieImage.Length > 0)
                {
                    var memoryStream = new MemoryStream();
                    await MovieImage.CopyToAsync(memoryStream);
                    movie.MovieImage = memoryStream.ToArray();
                }
                else
                {
                    movie.MovieImage = new byte[0];
                }

                _context.Add(movie);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(movie);
        }

        // GET: Movies/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var movie = await _context.Movie.FindAsync(id);
            if (movie == null)
            {
                return NotFound();
            }
            return View(movie);
        }

        // POST: Movies/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Description,Director,Genre,ReleaseDate,Hyperlink,MovieImage")] Movie movie, IFormFile MovieImage)
        {
            if (id != movie.Id)
            {
                return NotFound();
            }

            ModelState.Remove(nameof(movie.MovieImage));
            Movie existingMovie = _context.Movie.AsNoTracking().FirstOrDefault(movie => movie.Id == id);

            if(MovieImage != null && MovieImage.Length > 0)
            {
                var memoryStream = new MemoryStream();
                await MovieImage.CopyToAsync(memoryStream);
                movie.MovieImage = memoryStream.ToArray();
            } else if (existingMovie != null)
            {
                movie.MovieImage = existingMovie.MovieImage;
            }
            else
            {
                movie.MovieImage = new byte[0];
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(movie);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MovieExists(movie.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(movie);
        }

        // GET: Movies/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var movie = await _context.Movie
                .FirstOrDefaultAsync(m => m.Id == id);
            if (movie == null)
            {
                return NotFound();
            }

            return View(movie);
        }

        // POST: Movies/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var movie = await _context.Movie.FindAsync(id);
            if (movie != null)
            {
                _context.Movie.Remove(movie);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool MovieExists(int id)
        {
            return _context.Movie.Any(e => e.Id == id);
        }
    }
}
