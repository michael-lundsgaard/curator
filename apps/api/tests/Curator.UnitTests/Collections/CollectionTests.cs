using Curator.Domain.Collections;

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
}
