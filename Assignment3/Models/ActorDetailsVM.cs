namespace Assignment3.Models
{
    public class ActorDetailsVM
    {
        public Actor actor { get; set; }
        public List<ActorPhoneNumber> phoneNumbers { get; set; }
        public List<Movie> movies { get; set; }
        public string sentiment { get; set; }
        public PostRating postRatings { get; set; }
    }
}
