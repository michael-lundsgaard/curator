using System.ComponentModel.DataAnnotations;
using Curator.Application.Persistence;
using Curator.Application.Storage;
using Curator.Domain.Assets;
using Curator.Domain.Profiles;

namespace Curator.Application.Assets;

/// <summary>
/// Starts an upload: creates a <see cref="AssetStatus.Pending"/> asset and returns a presigned URL the browser
/// uploads the original to.
/// </summary>
public static class CreateUpload
{
    /// <summary>Largest file a single presigned <c>PUT</c> can upload to R2. Larger files need multipart.</summary>
    public const long MaxSizeBytes = 5_000_000_000;

    public static readonly TimeSpan UploadUrlLifetime = TimeSpan.FromMinutes(15);

    /// <param name="Filename">Name of the file on the user's device. The title defaults to it.</param>
    /// <param name="ContentType">MIME type of the file, e.g. <c>video/mp4</c>. The upload must send it.</param>
    /// <param name="SizeBytes">Size of the file. The upload is rejected if the file has another size.</param>
    /// <param name="RecordedAt">
    /// When the clip was captured, e.g. the browser's <c>File.lastModified</c>. Leave it out if unknown.
    /// </param>
    public sealed record Command(
        [Required, MaxLength(255)] string Filename,
        [Required, MaxLength(255), RegularExpression(@"^video/\S+$", ErrorMessage = "Only videos can be uploaded.")]
        string ContentType,
        [Range(1, MaxSizeBytes)] long SizeBytes,
        DateTimeOffset? RecordedAt = null);

    /// <param name="UploadUrl">Upload the file here with <c>PUT</c> and the same <c>Content-Type</c>.</param>
    /// <param name="UploadUrlExpiresAt">Start the upload before this.</param>
    public sealed record Response(Guid AssetId, string UploadUrl, DateTimeOffset UploadUrlExpiresAt);

    public sealed class Handler(ICuratorDbContext db, IFileStorage storage, TimeProvider time)
    {
        public async Task<Response> HandleAsync(Command command, Profile user, CancellationToken cancellationToken)
        {
            var now = time.GetUtcNow();
            var asset = new Asset(
                user.Id, command.Filename, command.ContentType, command.SizeBytes, command.RecordedAt, now);

            // Before saving, so a storage failure doesn't leave an asset nobody can upload to.
            var expiresAt = now + UploadUrlLifetime;
            var uploadUrl = await storage.CreateUploadUrlAsync(
                asset.SourceKey!, asset.ContentType, asset.SourceSizeBytes, expiresAt, cancellationToken);

            db.Assets.Add(asset);
            await db.SaveChangesAsync(cancellationToken);

            return new Response(asset.Id, uploadUrl, expiresAt);
        }
    }
}
