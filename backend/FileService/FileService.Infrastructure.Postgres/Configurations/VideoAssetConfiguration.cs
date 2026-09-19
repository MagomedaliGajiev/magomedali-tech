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
    }
}
