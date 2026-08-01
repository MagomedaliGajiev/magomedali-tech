using EducationContentService.Domain.Lessons;
using EducationContentService.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EducationContentService.Infrastructure.Postgres.Configurations;

public static class LessonIndexes
{
    public const string TITLE = "ix_lessons_title";
}

public class LessonConfiguration : IEntityTypeConfiguration<Lesson>
{
    public void Configure(EntityTypeBuilder<Lesson> builder)
    {
        builder.ToTable("lessons");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.OwnsOne(l => l.Title, title =>
        {
            title.Property(t => t.Value)
                .HasColumnName("title")
                .HasMaxLength(Title.MAX_LENGTH)
                .IsRequired();

            title.HasIndex(t => t.Value)
                .IsUnique()
                .HasDatabaseName(LessonIndexes.TITLE)
                .HasFilter("is_deleted = false");
        });
        builder.Navigation(l => l.Title).IsRequired();

        builder.Property(l => l.Description)
            .HasColumnName("description")
            .HasMaxLength(Description.MAX_LENGTH)
            .HasConversion(
                description => description.Value,
                value => Description.Create(value).Value)
            .IsRequired();

        builder.Property(l => l.VideoId)
            .HasColumnName("video_id");

        builder.Property(l => l.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(l => l.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.Property(l => l.IsDeleted)
            .HasColumnName("is_deleted")
            .IsRequired();

        builder.Property(l => l.DeletedAt)
            .HasColumnName("deleted_at");

        builder.HasQueryFilter(l => !l.IsDeleted);
    }
}