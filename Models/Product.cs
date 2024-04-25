using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace SportFlex.Models
{
    public class Product
    {
        public int Id { get; set; }
        [Display(Name = "Brand")]
        public int BrandId { get; set; }
        [Display(Name = "Color")]
        public int ColorId { get; set; }
        [Display(Name = "Category")]
        public int CategoryId { get; set; }
        [Required]
        [StringLength(50)]
        public required string Name { get; set; }
        [Required]
        public required string Description { get; set; }
        [Required(ErrorMessage = "The Images are required")]
        [Display(Name = "Images")]
        public List<string> ImagePaths { get; set; } = new List<string>();

        [Display(Name = "Category")]
        public Category? Category { get; set; }
        [Display(Name = "Color")]
        public Color? Color { get; set; }
        [Display(Name = "Brand")]
        public Brand? Brand { get; set; }
    }
}
