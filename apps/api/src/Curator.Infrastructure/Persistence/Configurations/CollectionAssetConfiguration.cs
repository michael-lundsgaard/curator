using Curator.Domain.Assets;
using Curator.Domain.Collections;
using Curator.Domain.Profiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Curator.Infrastructure.Persistence.Configurations;

internal sealed class CollectionAssetConfiguration : IEntityTypeConfiguration<CollectionAsset>
{
    public void Configure(EntityTypeBuilder<CollectionAsset> builder)
    {
        // The key also serves counting and listing a collection's clips; the asset_id foreign key gets its own index.
        builder.HasKey(ca => new { ca.CollectionId, ca.AssetId });

        // Deleting a collection removes its clips from it, never the clips themselves (data-model.md).
        builder.HasOne<Collection>()
            .WithMany()
            .HasForeignKey(ca => ca.CollectionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Assets are soft deleted, so this only cascades when storage cleanup removes the row for good.
        builder.HasOne<Asset>()
            .WithMany()
            .HasForeignKey(ca => ca.AssetId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Profile>()
            .WithMany()
            .HasForeignKey(ca => ca.AddedBy)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
