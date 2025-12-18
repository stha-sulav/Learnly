using Learnly.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Learnly.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Category> Categories { get; set; }
        public DbSet<Course> Courses { get; set; }
        public DbSet<Module> Modules { get; set; }
        public DbSet<Lesson> Lessons { get; set; }
        public DbSet<LessonProgress> LessonProgresses { get; set; }
        public DbSet<Enrollment> Enrollments { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<CommentLike> CommentLikes { get; set; } // New DbSet
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Quiz> Quizzes { get; set; }
        public DbSet<Question> Questions { get; set; }
        public DbSet<Attempt> Attempts { get; set; }
        public DbSet<Review> Reviews { get; set; }


        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure Course-Instructor relationship
            builder.Entity<Course>()
                .HasOne(c => c.Instructor)
                .WithMany(u => u.CoursesCreated) // Use the new collection
                .OnDelete(DeleteBehavior.NoAction);



            // Category relationships
            builder.Entity<Category>()
                .HasMany(c => c.SubCategories)
                .WithOne(c => c.ParentCategory)
                .HasForeignKey(c => c.ParentCategoryId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<Category>()
                .HasMany(c => c.Courses)
                .WithOne(c => c.Category)
                .HasForeignKey(c => c.CategoryId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<Course>()
                .HasMany(c => c.Modules)
                .WithOne(m => m.Course)
                .HasForeignKey(m => m.CourseId)
                .OnDelete(DeleteBehavior.NoAction);

            // Enrollment relationships
            builder.Entity<Enrollment>()
                .HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<Enrollment>()
                .HasOne(e => e.Course)
                .WithMany(c => c.Enrollments)
                .HasForeignKey(e => e.CourseId)
                .OnDelete(DeleteBehavior.NoAction);

            // LessonProgress relationships
            builder.Entity<LessonProgress>()
                .HasOne(lp => lp.User)
                .WithMany()
                .HasForeignKey(lp => lp.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<LessonProgress>()
                .HasOne(lp => lp.Lesson)
                .WithMany(l => l.LessonProgresses)
                .HasForeignKey(lp => lp.LessonId)
                .OnDelete(DeleteBehavior.NoAction);

            // Comment relationships
            builder.Entity<Comment>()
                .HasOne(com => com.User)
                .WithMany()
                .HasForeignKey(com => com.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<Comment>()
                .HasOne(com => com.Course)
                .WithMany()
                .HasForeignKey(com => com.CourseId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<Comment>()
                .HasOne(com => com.Lesson)
                .WithMany()
                .HasForeignKey(com => com.LessonId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<Comment>()
                .HasMany(com => com.Replies)
                .WithOne(com => com.ParentComment)
                .HasForeignKey(com => com.ParentCommentId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.NoAction);

            // CommentLike relationships
            builder.Entity<CommentLike>()
                .HasOne(cl => cl.Comment)
                .WithMany()
                .HasForeignKey(cl => cl.CommentId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<CommentLike>()
                .HasOne(cl => cl.User)
                .WithMany()
                .HasForeignKey(cl => cl.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            // Notification relationships
            builder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            // Quiz relationships
            builder.Entity<Quiz>()
                .HasOne(q => q.Lesson)
                .WithMany()
                .HasForeignKey(q => q.LessonId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Quiz>()
                .HasMany(q => q.Questions)
                .WithOne(ques => ques.Quiz)
                .HasForeignKey(ques => ques.QuizId)
                .OnDelete(DeleteBehavior.Cascade);

            // Attempt relationships
            builder.Entity<Attempt>()
                .HasOne(a => a.User)
                .WithMany()
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<Attempt>()
                .HasOne(a => a.Quiz)
                .WithMany(q => q.Attempts)
                .HasForeignKey(a => a.QuizId)
                .OnDelete(DeleteBehavior.NoAction);

            // Review relationships
            builder.Entity<Review>()
                .HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<Review>()
                .HasOne(r => r.Course)
                .WithMany(c => c.Reviews)
                .HasForeignKey(r => r.CourseId)
                .OnDelete(DeleteBehavior.NoAction);

            // Ensure one review per user per course
            builder.Entity<Review>()
                .HasIndex(r => new { r.UserId, r.CourseId })
                .IsUnique();
        }
    }
}
