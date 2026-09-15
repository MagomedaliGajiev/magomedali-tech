using FileService.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FileService.Infrastructure.Postgres.Configurations;

internal static class StorageKeyConfiguration
{
    public static void Configure<TOwner>(OwnedNavigationBuilder<TOwner, StorageKey> builder, string prefix)
        where TOwner : class
    {
        builder.Property(key => key.Location).HasColumnName($"{prefix}_location").HasMaxLength(128).IsRequired();
        builder.Property(key => key.Prefix).HasColumnName($"{prefix}_prefix").HasMaxLength(512).IsRequired();
        builder.Property(key => key.Key).HasColumnName($"{prefix}_key").HasMaxLength(512).IsRequired();
        builder.Ignore(key => key.Value);
        builder.Ignore(key => key.FullPath);
    }
}
