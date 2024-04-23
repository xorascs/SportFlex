using System.ComponentModel.DataAnnotations;

namespace SportFlex.Models
{
    public class User
    {
        public int Id { get; set; }
        [Required, Display(Name = "Role")]
        public Role Role { get; set; }
        [Required]
        public required string Login { get; set; }
        [Required]
        [DataType(DataType.Password)]
        public required string Password { get; set; }
        [Required]
        [DataType(DataType.EmailAddress)]
        public required string Email { get; set; }
    }

    public enum Role
    {
        User,
        Admin
    }
}
