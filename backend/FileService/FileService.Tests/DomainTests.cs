using FileService.Domain;
using FileService.Domain.Assets;
using Xunit;

namespace FileService.Tests;

public sealed class DomainTests
{
    [Fact]
    public void StorageKeyCreatePreservesNormalizedPrefix()
    {
        var result = StorageKey.Create("videos", @" raw\source ", "file-id");

        Assert.True(result.IsSuccess);
        Assert.Equal("raw/source", result.Value.Prefix);
        Assert.Equal("raw/source/file-id", result.Value.Value);
        Assert.Equal("videos/raw/source/file-id", result.Value.FullPath);
    }

    [Fact]
    public void PreviewAssetAcceptsImageAndStoresOwner()
    {
        FileName fileName = FileName.Create("preview.png").Value;
        ContentType contentType = ContentType.Create("image/png").Value;
        MediaData mediaData = MediaData.Create(fileName, contentType, 1024, 1).Value;
        MediaOwner owner = MediaOwner.ForLesson(Guid.NewGuid()).Value;

        var result = PreviewAsset.CreateForUpload(Guid.NewGuid(), mediaData, owner);

        Assert.True(result.IsSuccess);
        Assert.Equal(owner, result.Value.Owner);
        Assert.Equal(PreviewAsset.RAW_PREFIX, result.Value.Key.Prefix);
        Assert.Equal(PreviewAsset.LOCATION, result.Value.Key.Location);
    }
}