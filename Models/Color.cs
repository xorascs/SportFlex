using System.ComponentModel.DataAnnotations;

namespace SportFlex.Models
{
    public class Color
    {
        public int Id { get; set; }
        [Required]
        public required string Name { get; set; }
    }
}
