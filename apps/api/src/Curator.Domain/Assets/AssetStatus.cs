namespace Curator.Domain.Assets;

/// <summary>Upload and processing lifecycle of an asset. Stored as text.</summary>
public enum AssetStatus
{
    Pending,
    Uploaded,
    Processing,
    Ready,
    Failed,
}
