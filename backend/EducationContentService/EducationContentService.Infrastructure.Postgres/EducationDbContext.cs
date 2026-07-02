using EducationContentService.Domain.Lessons;
using Microsoft.EntityFrameworkCore;

namespace EducationContentService.Infrastructure.Postgres;

public class EducationDbContext : DbContext
{
    public EducationDbContext(DbContextOptions<EducationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Lesson> Lessons => Set<Lesson>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EducationDbContext).Assembly);
    }
}