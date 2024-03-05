using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace Assignment3.Models
{
    public class Actor
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string? Age { get; set; }
        public string? Gender { get; set; }
        public string? Hyperlink { get; set; }


        [DataType(DataType.Upload)]
        [DisplayName("Actor Image")]
        public byte[]? ActorImage { get; set; }
    }
}
