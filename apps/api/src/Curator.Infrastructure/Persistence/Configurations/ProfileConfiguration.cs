using Curator.Domain.Profiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Curator.Infrastructure.Persistence.Configurations;

internal sealed class ProfileConfiguration : IEntityTypeConfiguration<Profile>
{
    public void Configure(EntityTypeBuilder<Profile> builder)
    {
        // The ID comes from the JWT, never from the database.
        builder.Property(p => p.Id).ValueGeneratedNever();
        builder.Property(p => p.Role).HasConversion<string>();
    }
}
