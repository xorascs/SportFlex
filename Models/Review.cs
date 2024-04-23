using System.ComponentModel.DataAnnotations;

namespace SportFlex.Models
{
    public class Review
    {
        [Required]
        public int Id { get; set; }
        [Required]
        public int UserId { get; set; }
        [Required]
        public int ProductId { get; set; }
        [Required(ErrorMessage = "Comment is required.")]
        public required string Comment { get; set; }
        [Required]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd HH:mm:ss}", ApplyFormatInEditMode = true)]
        [Display(Name = "Created at"), DataType(DataType.Date)]
        public DateTime CreatedAt { get; set; }

        // Navigation properties
        public User? User { get; set; }
        public Product? Product { get; set; }
    }
}
