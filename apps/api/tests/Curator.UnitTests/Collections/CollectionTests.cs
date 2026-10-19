using Curator.Domain.Collections;
using Curator.Domain.Profiles;

namespace Curator.UnitTests.Collections;

public class CollectionTests
{
    [Fact]
    public void New_TrimsNameAndStoresBlankDescriptionAsNone()
    {
        var collection = new Collection(Guid.NewGuid(), null, "  Screams ", "   ", DateTimeOffset.UtcNow);

        Assert.Equal("Screams", collection.Name);
        Assert.Null(collection.Description);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void New_WithoutName_Throws(string name) =>
        Assert.ThrowsAny<ArgumentException>(() => new Collection(Guid.NewGuid(), null, name, null, DateTimeOffset.UtcNow));

    [Fact]
    public void CanBeEditedBy_CreatorAndAdmins()
    {
        var creator = new Profile(Guid.NewGuid(), "creator", DateTimeOffset.UtcNow);
        var member = new Profile(Guid.NewGuid(), "member", DateTimeOffset.UtcNow);
        var admin = new Profile(Guid.NewGuid(), "admin", DateTimeOffset.UtcNow);
        admin.ChangeRole(ProfileRole.Admin);
        var collection = new Collection(creator.Id, null, "Screams", null, DateTimeOffset.UtcNow);

        Assert.True(collection.CanBeEditedBy(creator));
        Assert.False(collection.CanBeEditedBy(member));
        Assert.True(collection.CanBeEditedBy(admin));
    }

    [Fact]
    public void Edit_TrimsNameAndStoresBlankDescriptionAsNone()
    {
        var created = DateTimeOffset.UtcNow;
        var collection = new Collection(Guid.NewGuid(), null, "Screams", "Loud ones", created);

        collection.Edit(" Jumpscares ", "  ", created.AddMinutes(1));

        Assert.Equal("Jumpscares", collection.Name);
        Assert.Null(collection.Description);
        Assert.Equal(created.AddMinutes(1), collection.UpdatedAt);
    }

    [Fact]
    public void MoveTo_ChangesParent()
    {
        var created = DateTimeOffset.UtcNow;
        var parentId = Guid.NewGuid();
        var collection = new Collection(Guid.NewGuid(), null, "Screams", null, created);

        collection.MoveTo(parentId, created.AddMinutes(1));

        Assert.Equal(parentId, collection.ParentId);
        Assert.Equal(created.AddMinutes(1), collection.UpdatedAt);
    }

    [Fact]
    public void MoveTo_Itself_Throws()
    {
        var collection = new Collection(Guid.NewGuid(), null, "Screams", null, DateTimeOffset.UtcNow);

        Assert.Throws<ArgumentException>(() => collection.MoveTo(collection.Id, DateTimeOffset.UtcNow));
    }
}
