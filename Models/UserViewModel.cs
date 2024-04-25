namespace SportFlex.Models
{
    public class UserViewModel
    {
        public User? User { get; set; }
        public IEnumerable<Review> Reviews { get; set; } = new List<Review>();
    }
}
