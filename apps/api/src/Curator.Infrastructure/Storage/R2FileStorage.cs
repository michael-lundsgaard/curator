using System.Net;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Curator.Application.Storage;
using Microsoft.Extensions.Options;

namespace Curator.Infrastructure.Storage;

/// <summary>R2 through the AWS SDK, since R2 speaks the S3 API.</summary>
internal sealed class R2FileStorage(IOptions<R2Options> options) : IFileStorage, IDisposable
{
    // Created on first use, so the API starts (e.g. for /me) without R2 configured.
    private readonly Lazy<AmazonS3Client> _client = new(() => CreateClient(options.Value));

    public async Task<string> CreateUploadUrlAsync(
        string key,
        string contentType,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken)
    {
        // Presigning is computed locally; it doesn't call R2.
        return await _client.Value.GetPreSignedURLAsync(new GetPreSignedUrlRequest
        {
            BucketName = options.Value.Bucket,
            Key = key,
            Verb = HttpVerb.PUT,
            ContentType = contentType,
            Expires = expiresAt.UtcDateTime,
        });
    }

    public async Task<long?> GetSizeAsync(string key, CancellationToken cancellationToken)
    {
        try
        {
            var metadata = await _client.Value.GetObjectMetadataAsync(options.Value.Bucket, key, cancellationToken);
            return metadata.ContentLength;
        }
        catch (AmazonS3Exception e) when (e.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public void Dispose()
    {
        if (_client.IsValueCreated)
        {
            _client.Value.Dispose();
        }
    }

    private static AmazonS3Client CreateClient(R2Options r2)
    {
        if (new[] { r2.AccountId, r2.Bucket, r2.AccessKeyId, r2.SecretAccessKey }.Any(string.IsNullOrWhiteSpace))
        {
            throw new InvalidOperationException(
                $"{R2Options.SectionName} is not configured. Set AccountId, Bucket, AccessKeyId and SecretAccessKey " +
                "with 'dotnet user-secrets set'.");
        }

        return new AmazonS3Client(
            new BasicAWSCredentials(r2.AccessKeyId, r2.SecretAccessKey),
            new AmazonS3Config
            {
                ServiceURL = $"https://{r2.AccountId}.r2.cloudflarestorage.com",
                AuthenticationRegion = "auto",
                ForcePathStyle = true,

                // R2 doesn't support all of the SDK's default checksum headers.
                RequestChecksumCalculation = RequestChecksumCalculation.WHEN_REQUIRED,
                ResponseChecksumValidation = ResponseChecksumValidation.WHEN_REQUIRED,
            });
    }
}
