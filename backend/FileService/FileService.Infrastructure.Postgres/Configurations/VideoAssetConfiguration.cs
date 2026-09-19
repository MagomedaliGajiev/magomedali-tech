using FileService.Domain.Assets;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FileService.Infrastructure.Postgres.Configurations;

public sealed class VideoAssetConfiguration : IEntityTypeConfiguration<VideoAsset>
{
    public void Configure(EntityTypeBuilder<VideoAsset> builder)
    {
        builder.OwnsOne(asset => asset.PreviewKey, storageKey =>
            StorageKeyConfiguration.Configure(storageKey, "preview_storage"));
        builder.Navigation(asset => asset.PreviewKey).IsRequired(false);

        builder.OwnsOne(asset => asset.Metadata, metadata =>
        {
            metadata.Property(value => value.Duration).HasColumnName("video_duration").IsRequired();
            metadata.Property(value => value.Width).HasColumnName("video_width").IsRequired();
            metadata.Property(value => value.Height).HasColumnName("video_height").IsRequired();
        });
        builder.Navigation(asset => asset.Metadata).IsRequired(false);
    }
}
