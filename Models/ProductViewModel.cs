namespace SportFlex.Models
{
    public class ProductViewModel
    {
        public Product? Product { get; set; }
        public IEnumerable<Review> Reviews { get; set; } = new List<Review>();
    }
}
