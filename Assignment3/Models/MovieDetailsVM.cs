using System.ComponentModel.DataAnnotations;

namespace Assignment3.Models
{
    public class MovieDetailsVM
    {
        public Movie movie { get; set; }
        public string sentiment { get; set; }
        public List<Actor> actors { get; set; }
        // public  List<MovieWikiVM> posts { get; set; }
    }
}
