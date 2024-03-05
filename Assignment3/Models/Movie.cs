using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace Assignment3.Models
{
    public class Movie
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string? Description { get; set; }
        public string? Director { get; set; }
        public string? Genre { get; set; }
        [Display(Name = "Release Year")]
        public string? ReleaseDate { get; set; }
        public string? Hyperlink { get; set; }

        [DataType(DataType.Upload)]
        [DisplayName("Movie Image")]
        public byte[]? MovieImage { get; set; }

    }
}
