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

            // Look for any courses.
            if (context.Courses.Any())
            {
                logger.LogInformation("Database already seeded with courses. Skipping course seeding.");
                return;   // DB has been seeded
            }

            // Ensure instructor exists
            var instructorUser = await userManager.FindByEmailAsync("instructor@learnly.com");
            if (instructorUser == null)
            {
                logger.LogError("Instructor user not found. Cannot seed courses without an instructor.");
                return;
            }

            var course = new Course
            {
                Title = "Intro to MVC",
                Slug = "intro-to-mvc",
                Description = "A beginner-friendly introduction to the Model-View-Controller architectural pattern in web development.",
                InstructorId = instructorUser.Id,
                Price = 19.99m,
                ThumbnailPath = "/img/course-thumbnails/mvc-thumb.jpg", // Placeholder thumbnail
                CreatedAt = DateTime.UtcNow,
                IsPublished = true
            };
            context.Courses.Add(course);
            await context.SaveChangesAsync();
            logger.LogInformation("Course '{CourseTitle}' seeded.", course.Title);


            var module1 = new Module
            {
                CourseId = course.Id,
                Title = "Module 1: Understanding MVC",
                OrderIndex = 1
            };
            var module2 = new Module
            {
                CourseId = course.Id,
                Title = "Module 2: Building Your First MVC App",
                OrderIndex = 2
            };
            context.Modules.AddRange(module1, module2);
            await context.SaveChangesAsync();
            logger.LogInformation("Modules for course '{CourseTitle}' seeded.", course.Title);

            var lessonsModule1 = new Lesson[]
            {
                new Lesson { ModuleId = module1.Id, Title = "What is MVC?", ContentType = ContentType.Video, VideoPath = "/videos/placeholder.mp4", DurationSeconds = 300, OrderIndex = 1 },
                new Lesson { ModuleId = module1.Id, Title = "The Model Layer", ContentType = ContentType.Video, VideoPath = "/videos/placeholder.mp4", DurationSeconds = 450, OrderIndex = 2 },
                new Lesson { ModuleId = module1.Id, Title = "The View Layer", ContentType = ContentType.Video, VideoPath = "/videos/placeholder.mp4", DurationSeconds = 380, OrderIndex = 3 }
            };
            var lessonsModule2 = new Lesson[]
            {
                new Lesson { ModuleId = module2.Id, Title = "The Controller Layer", ContentType = ContentType.Video, VideoPath = "/videos/placeholder.mp4", DurationSeconds = 400, OrderIndex = 1 },
                new Lesson { ModuleId = module2.Id, Title = "Routing in MVC", ContentType = ContentType.Video, VideoPath = "/videos/placeholder.mp4", DurationSeconds = 520, OrderIndex = 2 },
                new Lesson { ModuleId = module2.Id, Title = "Putting It All Together", ContentType = ContentType.Video, VideoPath = "/videos/placeholder.mp4", DurationSeconds = 600, OrderIndex = 3 }
            };
            context.Lessons.AddRange(lessonsModule1);
            context.Lessons.AddRange(lessonsModule2);
            await context.SaveChangesAsync();
            logger.LogInformation("Lessons for course '{CourseTitle}' seeded.", course.Title);
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
