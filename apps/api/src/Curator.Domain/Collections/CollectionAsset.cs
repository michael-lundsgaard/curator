namespace Curator.Domain.Collections;

/// <summary>
/// A clip added to a collection. A clip can be in several collections, and is only in the ones it was added to:
/// showing sub-collections' clips too is a way of listing, not more rows.
/// </summary>
public class CollectionAsset
{
    public CollectionAsset(Guid collectionId, Guid assetId, Guid addedBy, DateTimeOffset addedAt)
    {
        CollectionId = collectionId;
        AssetId = assetId;
        AddedBy = addedBy;
        AddedAt = addedAt;
    }

    public Guid CollectionId { get; private set; }
    public Guid AssetId { get; private set; }
    public Guid AddedBy { get; private set; }
    public DateTimeOffset AddedAt { get; private set; }
}
