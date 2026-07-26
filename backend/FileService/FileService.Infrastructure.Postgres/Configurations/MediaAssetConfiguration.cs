using FileService.Domain;
using FileService.Domain.Assets;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FileService.Infrastructure.Postgres.Configurations;

public sealed class MediaAssetConfiguration : IEntityTypeConfiguration<MediaAsset>
{
    public void Configure(EntityTypeBuilder<MediaAsset> builder)
    {
        builder.ToTable("media_assets");

        builder.HasKey(asset => asset.Id);

        builder.Property(asset => asset.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.HasDiscriminator(asset => asset.AssetType)
            .HasValue<VideoAsset>(AssetType.VIDEO)
            .HasValue<PreviewAsset>(AssetType.PREVIEW);

        builder.Property(asset => asset.AssetType)
            .HasColumnName("asset_type")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(asset => asset.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(asset => asset.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(asset => asset.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        ConfigureMediaData(builder);
        ConfigureStorageKey(builder);
        ConfigureOwner(builder);
    }

    private static void ConfigureMediaData(EntityTypeBuilder<MediaAsset> builder)
    {
        builder.OwnsOne(asset => asset.MediaData, mediaData =>
        {
            mediaData.Property(data => data.Size)
                .HasColumnName("size")
                .IsRequired();

            mediaData.Property(data => data.ExpectedChunksCount)
                .HasColumnName("expected_chunks_count")
                .IsRequired();

            mediaData.OwnsOne(data => data.FileName, fileName =>
            {
                fileName.Property(value => value.Name)
                    .HasColumnName("file_name")
                    .HasMaxLength(512)
                    .IsRequired();

                fileName.Property(value => value.Extension)
                    .HasColumnName("file_extension")
                    .HasMaxLength(32)
                    .IsRequired();
            });
            mediaData.Navigation(data => data.FileName).IsRequired();

            mediaData.OwnsOne(data => data.ContentType, contentType =>
            {
                contentType.Property(value => value.Value)
                    .HasColumnName("content_type")
                    .HasMaxLength(255)
                    .IsRequired();

                contentType.Property(value => value.Category)
                    .HasColumnName("media_type")
                    .HasConversion<string>()
                    .HasMaxLength(20)
                    .IsRequired();
            });
            mediaData.Navigation(data => data.ContentType).IsRequired();
        });
        builder.Navigation(asset => asset.MediaData).IsRequired();
    }

    private static void ConfigureStorageKey(EntityTypeBuilder<MediaAsset> builder)
    {
        builder.OwnsOne(asset => asset.Key, storageKey =>
        {
            storageKey.Property(key => key.Location)
                .HasColumnName("storage_location")
                .HasMaxLength(128)
                .IsRequired();

            storageKey.Property(key => key.Prefix)
                .HasColumnName("storage_prefix")
                .HasMaxLength(512)
                .IsRequired();

            storageKey.Property(key => key.Key)
                .HasColumnName("storage_key")
                .HasMaxLength(512)
                .IsRequired();

            storageKey.Ignore(key => key.Value);
            storageKey.Ignore(key => key.FullPath);
        });
        builder.Navigation(asset => asset.Key).IsRequired();
    }

    private static void ConfigureOwner(EntityTypeBuilder<MediaAsset> builder)
    {
        builder.OwnsOne(asset => asset.Owner, owner =>
        {
            owner.Property(value => value.Context)
                .HasColumnName("owner_context")
                .HasMaxLength(50)
                .IsRequired();

            owner.Property(value => value.EntityId)
                .HasColumnName("owner_entity_id")
                .IsRequired();

            owner.HasIndex(value => new { value.Context, value.EntityId })
                .HasDatabaseName("ix_media_assets_owner");
        });
        builder.Navigation(asset => asset.Owner).IsRequired();
    }
}