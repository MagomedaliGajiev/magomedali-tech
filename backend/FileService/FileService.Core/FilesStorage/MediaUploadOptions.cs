namespace FileService.Core.FilesStorage;

public sealed class MediaUploadOptions
{
    public const string SECTION_NAME = "MediaUpload";

    // Enable for files that are already prepared for playback and need no processing.
    public bool DirectUpload { get; init; }
}
