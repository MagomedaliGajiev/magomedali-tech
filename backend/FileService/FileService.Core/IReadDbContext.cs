using FileService.Domain.Assets;

using FileService.Domain.Processing;

namespace FileService.Core;

public interface IReadDbContext
{
    IQueryable<MediaAsset> MediaAssetsQuery { get;  }

    IQueryable<VideoProcess> VideoProcessesQuery { get; }
}
