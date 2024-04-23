using SportFlex.Models;
using Microsoft.IdentityModel.Tokens;
using SportFlex.Data;

namespace SportFlex.Data
{
    public class DbInitializer
    {
        public static void Initialize(DataContext context)
        {
            // Check if any data exists in the database
            if (context.Users.Any())
            {
                // Database has been seeded
                return;
            }

            // Fill the database with data
            SeedData(context);
        }

        public static void SeedData(DataContext context)
        {
            // Add brands
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