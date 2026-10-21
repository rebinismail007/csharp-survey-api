using Microsoft.EntityFrameworkCore;

namespace SurveyApi;

public sealed class SurveyDb(DbContextOptions<SurveyDb> options) : DbContext(options)
{
    public DbSet<Survey> Surveys => Set<Survey>();
    public DbSet<SurveyResponse> Responses => Set<SurveyResponse>();
    public DbSet<Answer> Answers => Set<Answer>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<Survey>().Property(s => s.Title).HasMaxLength(120);
        model.Entity<Survey>().HasMany(s => s.Questions).WithOne()
            .HasForeignKey(q => q.SurveyId).OnDelete(DeleteBehavior.Cascade);
        model.Entity<Survey>().HasMany(s => s.Responses).WithOne()
            .HasForeignKey(r => r.SurveyId).OnDelete(DeleteBehavior.Cascade);
        model.Entity<Question>().HasIndex(q => new { q.SurveyId, q.Position }).IsUnique();
        model.Entity<Question>().Property(q => q.Text).HasMaxLength(200);
        model.Entity<SurveyResponse>().HasMany(r => r.Answers).WithOne(a => a.Response)
            .HasForeignKey(a => a.ResponseId).OnDelete(DeleteBehavior.Cascade);
        model.Entity<Answer>().HasOne<Question>().WithMany()
            .HasForeignKey(a => a.QuestionId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<Answer>().HasIndex(a => new { a.ResponseId, a.QuestionId }).IsUnique();
        model.Entity<Answer>().ToTable(t => t.HasCheckConstraint("CK_Answer_Score", "Score BETWEEN 1 AND 5"));
    }
}
