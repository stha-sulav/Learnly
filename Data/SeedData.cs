using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using Learnly.Models;
using Learnly.Constants;
using Microsoft.Extensions.Logging;

namespace Learnly.Data
{
    public static class SeedData
    {
        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Learnly.Data.SeedData");
            string[] roleNames = { Roles.Admin, Roles.Instructor, Roles.User };
            IdentityResult roleResult;

            foreach (var roleName in roleNames)
            {
                var roleExist = await roleManager.RoleExistsAsync(roleName);
                if (!roleExist)
                {
                    roleResult = await roleManager.CreateAsync(new IdentityRole(roleName));
                    if (roleResult.Succeeded)
                    {
                        logger.LogInformation("Role '{RoleName}' created successfully.", roleName);
                    }
                    else
                    {
                        logger.LogError("Error creating role '{RoleName}': {Errors}", roleName, string.Join(", ", roleResult.Errors.Select(e => e.Description)));
                    }
                }
            }

            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            // Seed Admin, Instructor, and User
            await EnsureUserWithRoleAsync(userManager, logger, "admin@learnly.com", "admin", "Admin", "User", "Admin@123", Roles.Admin);
            await EnsureUserWithRoleAsync(userManager, logger, "instructor@learnly.com", "instructor", "Instructor", "User", "Instructor@123", Roles.Instructor);
            await EnsureUserWithRoleAsync(userManager, logger, "user@learnly.com", "user", "Regular", "User", "User@123", Roles.User);

            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();

            // Seed Categories
            if (!context.Categories.Any())
            {
                var categories = new Category[]
                {
                    new Category { Name = "Development", Description = "Software development and programming courses" },
                    new Category { Name = "Business", Description = "Business, entrepreneurship, and management courses" },
                    new Category { Name = "Design", Description = "Graphic design, UI/UX, and creative courses" },
                    new Category { Name = "Marketing", Description = "Digital marketing, SEO, and social media courses" },
                    new Category { Name = "IT & Software", Description = "IT certifications, networking, and security courses" },
                    new Category { Name = "Personal Development", Description = "Productivity, leadership, and personal growth courses" },
                    new Category { Name = "Photography & Video", Description = "Photography, video production, and editing courses" },
                    new Category { Name = "Music", Description = "Music production, instruments, and theory courses" },
                    new Category { Name = "Health & Fitness", Description = "Fitness, nutrition, and wellness courses" },
                    new Category { Name = "Teaching & Academics", Description = "Academic subjects and teaching methodology courses" }
                };

                context.Categories.AddRange(categories);
                await context.SaveChangesAsync();
                logger.LogInformation("Seeded {Count} categories.", categories.Length);
            }

            // Look for any courses.
            if (context.Courses.Any())
            {
                logger.LogInformation("Database already seeded with courses. Skipping course seeding.");
                return;   // DB has been seeded
            }
        }

        private static async Task EnsureUserWithRoleAsync(UserManager<ApplicationUser> userManager, ILogger logger,
            string email, string userName, string firstName, string lastName, string password, string role)
        {
            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = userName,
                    Email = email,
                    FirstName = firstName,
                    LastName = lastName,
                    EmailConfirmed = true
                };
                var result = await userManager.CreateAsync(user, password);
                if (result.Succeeded)
                {
                    logger.LogInformation("User '{Email}' created successfully.", email);
                    var roleAddResult = await userManager.AddToRoleAsync(user, role);
                    if (roleAddResult.Succeeded)
                    {
                        logger.LogInformation("Assigned role '{Role}' to user '{Email}'.", role, email);
                    }
                    else
                    {
                        logger.LogError("Error assigning role '{Role}' to user '{Email}': {Errors}", role, email, string.Join(", ", roleAddResult.Errors.Select(e => e.Description)));
                    }
                }
                else
                {
                    logger.LogError("Error creating user '{Email}': {Errors}", email, string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
            else
            {
                if (!await userManager.IsInRoleAsync(user, role))
                {
                    var roleAddResult = await userManager.AddToRoleAsync(user, role);
                    if (roleAddResult.Succeeded)
                    {
                        logger.LogInformation("Assigned role '{Role}' to user '{Email}'.", role, email);
                    }
                    else
                    {
                        logger.LogError("Error assigning role '{Role}' to user '{Email}': {Errors}", role, email, string.Join(", ", roleAddResult.Errors.Select(e => e.Description)));
                    }
                }
            }
        }
    }
}
