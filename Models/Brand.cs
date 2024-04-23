using System.ComponentModel.DataAnnotations;

namespace SportFlex.Models
{
    public class Brand
    {
        public int Id { get; set; }
        [Required]
        public required string Name { get; set; }
    }
}
