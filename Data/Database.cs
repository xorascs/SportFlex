using SportFlex.Models;
using Microsoft.IdentityModel.Tokens;
using SportFlex.Data;

namespace SportFlex.Data
{
    public class Database
    {
        public static void Create(DataContext context)
        {
            if (context.Users.Any())
            {
                return;
            }

            var users = new User[]
            {
                new User { Role = Role.Admin, Login = "Andrejs", Password = "123", Email = "andrejs@example.com" }
            };
            foreach (var user in users)
            {
                context.Add(user);
            }
            context.SaveChanges();
        }
    }
}