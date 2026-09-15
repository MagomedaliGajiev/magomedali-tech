using FileService.Domain.Processing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FileService.Infrastructure.Postgres.Configurations;

public sealed class VideoProcessConfiguration : IEntityTypeConfiguration<VideoProcess>
{
    public void Configure(EntityTypeBuilder<VideoProcess> builder)
    {
        builder.ToTable("video_processes");
        builder.HasKey(process => process.Id);
        builder.Property(process => process.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(process => process.VideoAssetId).HasColumnName("video_asset_id");
        builder.Property(process => process.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20);
        builder.Property(process => process.Progress).HasColumnName("progress").HasPrecision(5, 2);
        builder.Property(process => process.CreatedAt).HasColumnName("created_at");
        builder.Property(process => process.UpdatedAt).HasColumnName("updated_at");
        builder.Property(process => process.CompletedAt).HasColumnName("completed_at");
        builder.Property(process => process.ErrorMessage).HasColumnName("error_message");
        builder.Property(process => process.Version).HasColumnName("version").IsConcurrencyToken();
        builder.Ignore(process => process.CurrentStep);

        builder.HasOne(process => process.VideoAsset)
            .WithOne(asset => asset.Process)
            .HasForeignKey<VideoProcess>(process => process.VideoAssetId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(process => process.Status);

        builder.OwnsMany(process => process.Steps, steps =>
        {
            steps.ToTable("processing_steps");
            steps.WithOwner().HasForeignKey("VideoProcessId");
            steps.Property<Guid>("VideoProcessId").HasColumnName("video_process_id");
            steps.HasKey(step => step.Id);
            steps.Property(step => step.Id).HasColumnName("id").ValueGeneratedNever();
            steps.Property(step => step.AttemptId).HasColumnName("attempt_id");
            steps.Property(step => step.Type).HasColumnName("step_type").HasConversion<string>().HasMaxLength(32);
            steps.Property(step => step.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20);
            steps.Property(step => step.Order).HasColumnName("step_order");
            steps.Property(step => step.Weight).HasColumnName("weight");
            steps.Property(step => step.ResultData).HasColumnName("result_data");
            steps.Property(step => step.ErrorMessage).HasColumnName("error_message");
            steps.Property(step => step.StartedAt).HasColumnName("started_at");
            steps.Property(step => step.CompletedAt).HasColumnName("completed_at");
            steps.Property(step => step.RetryCount).HasColumnName("retry_count");
            steps.Property(step => step.NextRetryAt).HasColumnName("next_retry_at");
            steps.Property(step => step.IsCriticalFailure).HasColumnName("is_critical_failure");
            steps.Ignore(step => step.CanRetry);
            steps.HasIndex("VideoProcessId", nameof(ProcessingStep.Order)).IsUnique();
            steps.HasIndex(step => new { step.Status, step.NextRetryAt });
        });
        builder.Navigation(process => process.Steps).HasField("_steps").UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
