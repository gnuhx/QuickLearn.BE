using Microsoft.EntityFrameworkCore;
using QuickLearn.BE.Models;

namespace QuickLearn.BE.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Subject> Subjects { get; set; }
    public DbSet<Grade> Grades { get; set; }
    public DbSet<GradeSubject> GradeSubjects { get; set; }
    public DbSet<Test> Tests { get; set; }
    public DbSet<Question> Questions { get; set; }
    public DbSet<Answer> Answers { get; set; }
    public DbSet<TestResult> TestResults { get; set; }
    public DbSet<UserAnswer> UserAnswers { get; set; }
    public DbSet<User> Users { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure GradeSubject many-to-many relationship
        modelBuilder.Entity<GradeSubject>()
            .HasKey(gs => new { gs.GradeId, gs.SubjectId });

        modelBuilder.Entity<GradeSubject>()
            .HasOne(gs => gs.Grade)
            .WithMany(g => g.GradeSubjects)
            .HasForeignKey(gs => gs.GradeId);

        modelBuilder.Entity<GradeSubject>()
            .HasOne(gs => gs.Subject)
            .WithMany(s => s.GradeSubjects)
            .HasForeignKey(gs => gs.SubjectId);

        // Configure UserAnswer relationships
        modelBuilder.Entity<UserAnswer>()
            .HasOne(ua => ua.Question)
            .WithMany(q => q.UserAnswers)
            .HasForeignKey(ua => ua.QuestionId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<UserAnswer>()
            .HasOne(ua => ua.TestResult)
            .WithMany(tr => tr.UserAnswers)
            .HasForeignKey(ua => ua.TestResultId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure UserAnswer many-to-many relationship with Answer
        modelBuilder.Entity<UserAnswer>()
            .HasMany(ua => ua.SelectedAnswers)
            .WithMany(a => a.UserAnswers)
            .UsingEntity(
                "UserAnswerAnswers",
                l => l.HasOne(typeof(Answer)).WithMany().HasForeignKey("SelectedAnswersId").OnDelete(DeleteBehavior.NoAction),
                r => r.HasOne(typeof(UserAnswer)).WithMany().HasForeignKey("UserAnswersId").OnDelete(DeleteBehavior.NoAction),
                j => j.HasKey("SelectedAnswersId", "UserAnswersId")
            );

        // Configure decimal precision for TestResult.TotalScore
        modelBuilder.Entity<TestResult>()
            .Property(tr => tr.TotalScore)
            .HasPrecision(5, 2);
    }
} 