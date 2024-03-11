namespace Assignment3.Models;

public class MovieWikiVM
{
    public MovieDetailsVM oldModel { get; set; }
    public List<string> pages { get; set; }
    public List<double> sentiments { get; set; }
}