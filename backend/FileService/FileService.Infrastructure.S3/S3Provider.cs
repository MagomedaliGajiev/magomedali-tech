using Amazon.S3;
using Amazon.S3.Model;
using FileService.Core.Features;
using Microsoft.Extensions.Options;

namespace FileService.Infrastructure.S3;

public class S3Provider : IS3Provider
{
    private readonly IAmazonS3 _s3Client;
    private readonly S3Options _s3options;

    public S3Provider(IAmazonS3 s3client, IOptions<S3Options> s3Options)
    {
        _s3Client = s3client;
        _s3options = s3Options.Value;
    }

    public async Task UploadFileAsync(
        Stream stream,
        string bucketName,
        string key,
        string contentType,
        CancellationToken cancellationToken)
    {
        var request = new PutObjectRequest
        {
            BucketName = bucketName,
            Key = key,
            InputStream = stream,
            ContentType = contentType
        };

        await _s3Client.PutObjectAsync(request, cancellationToken);
    }

    public async Task<string> GenerateDownloadUrlAsync(string bucketName, string key)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = bucketName,
            Key = key,
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.AddHours(_s3options.DownloadUrlExpirationsHours),
            Protocol = _s3options.WithSsl ? Protocol.HTTPS : Protocol.HTTP
        };

        return await _s3Client.GetPreSignedURLAsync(request);
    }
}