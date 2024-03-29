﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Assignment3.Data;
using Assignment3.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;
using System.Text.Json;
using VaderSharp2;

namespace TheMovieDB.Controllers
{
    public class ActorsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public async Task<IActionResult> GetActorPhoto(int id)
        {
            var actor = await _context.Actor
                .FirstOrDefaultAsync(m => m.Id == id);
            if (actor == null)
            {
                return NotFound();
            }
            var imageData = actor.ActorImage;

            return File(imageData, "image/jpg");
        }

        public ActorsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Actors
        public async Task<IActionResult> Index()
        {
            return _context.Actor != null ?
                        View(await _context.Actor.ToListAsync()) :
                        Problem("Entity set 'ApplicationDbContext.Actor'  is null.");
        }
        
        // START NEW CODE
        public static readonly HttpClient client = new HttpClient();
       
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

        // GET: Actors/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.Actor == null)
            {
                return NotFound();
            }

            var actor = await _context.Actor
                .FirstOrDefaultAsync(m => m.Id == id);
            if (actor == null)
            {
                return NotFound();
            }
            ActorDetailsVM ad = new ActorDetailsVM();
            ad.actor = actor;
            PostRating newPost = new PostRating();
            
            var movies = new List<Movie>();
            
            List<string> textToExamine = await SearchWikipediaAsync(actor.Name);
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

            newPost.num = validResults;

            newPost.sentiment = tempList;
            
            double avgResult = Math.Round(resultsTotal / validResults, 2);
            ad.sentiment = avgResult.ToString() + ", " + CategorizeSentiment(avgResult);

            
            movies = await (from mt in _context.Movie
                join am in _context.ActorMovie on mt.Id equals am.Id
                where am.Id == id
                select mt).ToListAsync();
            ad.movies = movies;

            ad.postRatings = newPost;
 
            return View(ad);
        }

        // GET: Actors/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Actors/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,Age,Gender,Hyperlink")] Actor actor, IFormFile ActorImage)
        {
            ModelState.Remove(nameof(actor.ActorImage));

            if (ModelState.IsValid)
            {
                if (ActorImage != null && ActorImage.Length > 0)
                {
                    var memoryStream = new MemoryStream();
                    await ActorImage.CopyToAsync(memoryStream);
                    actor.ActorImage = memoryStream.ToArray();
                }
                else
                {
                    actor.ActorImage = new byte[0];
                }
                _context.Add(actor);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(actor);
        }

        //[Authorize(Roles = Constants.AdministratorsRole + "," + Constants.ManagersRole)]
        //[Authorize(Roles = "AdministratorRole,ManagerRole")] //this is an OR condition!!!
        //[Authorize(Roles = Constants.AdministratorsRole)]
        //[Authorize(Policy = Constants.ManagerAndAdministrator)]
        //[Authorize(Roles = Constants.Administrator + "," + Constants.Manager)]
        // GET: Actors/Edit/5
  
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Actor == null)
            {
                return NotFound();
            }

            var actor = await _context.Actor.FindAsync(id);
            if (actor == null)
            {
                return NotFound();
            }
            return View(actor);
        }

        // POST: Actors/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Age,Gender,Hyperlink")] Actor actor, IFormFile ActorImage)
        {
            if (id != actor.Id)
            {
                return NotFound();
            }

            ModelState.Remove(nameof(actor.ActorImage));

            Actor existingActor = _context.Actor.AsNoTracking().FirstOrDefault(m => m.Id == id);

            if (ActorImage != null && ActorImage.Length > 0)
            {
                var memoryStream = new MemoryStream();
                await ActorImage.CopyToAsync(memoryStream);
                actor.ActorImage = memoryStream.ToArray();
            }
            //grab EXISTING photo from DB in case user didn't upload a new one. Otherwise, the actor will have the photo overwritten with empty
            else if (existingActor != null)
            {
                actor.ActorImage = existingActor.ActorImage;
            }
            else
            {
                actor.ActorImage = new byte[0];
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(actor);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ActorExists(actor.Id))
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
            return View(actor);
        }

        // GET: Actors/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Actor == null)
            {
                return NotFound();
            }

            var actor = await _context.Actor
                .FirstOrDefaultAsync(m => m.Id == id);
            if (actor == null)
            {
                return NotFound();
            }

            return View(actor);
        }

        // POST: Actors/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Actor == null)
            {
                return Problem("Entity set 'ApplicationDbContext.Actor'  is null.");
            }
            var actor = await _context.Actor.FindAsync(id);
            if (actor != null)
            {
                _context.Actor.Remove(actor);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ActorExists(int id)
        {
            return (_context.Actor?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}