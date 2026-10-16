using Curator.Domain.Assets;
using Curator.Domain.Profiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Curator.Infrastructure.Persistence.Configurations;

internal sealed class AssetConfiguration : IEntityTypeConfiguration<Asset>
{
    public void Configure(EntityTypeBuilder<Asset> builder)
    {
        builder.Property(a => a.Id).ValueGeneratedNever();
        builder.Property(a => a.Status).HasConversion<string>();

        builder.HasOne<Profile>()
            .WithMany()
            .HasForeignKey(a => a.UploadedBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(a => a.DeletedAt == null);
    }
}
