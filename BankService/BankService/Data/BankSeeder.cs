using BankService.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BankService.Data
{
    // Member 3: Seeds a single administrator without committing a password to source control.
    public static class BankSeeder
    {
        public static async Task SeedAsync(
            BankContext context,
            IPasswordHasher<User> passwordHasher,
            IConfiguration configuration,
            ILogger logger)
        {
            const string adminUsername = "admin";

            if (await context.Users.AnyAsync(user => user.Username == adminUsername))
            {
                return;
            }

            var adminPassword = configuration["SeedAdmin:Password"];

            if (string.IsNullOrWhiteSpace(adminPassword))
            {
                logger.LogWarning(
                    "Seed administrator was not created because SeedAdmin:Password is not configured.");
                return;
            }

            var admin = new User
            {
                Username = adminUsername,
                Name = "System Administrator",
                Email = "admin@bankservice.local",
                Address = "System",
                Phone = "N/A",
                PicturePath = string.Empty,
                PasswordSalt = string.Empty,
                Role = "Admin",
                IsActive = true
            };

            admin.PasswordHash = passwordHasher.HashPassword(admin, adminPassword);

            context.Users.Add(admin);
            await context.SaveChangesAsync();

            logger.LogInformation("Seed administrator account created.");
        }
    }
}
