namespace Curator.Application.Collections;

/// <summary>Who created a collection, as shown next to it.</summary>
/// <param name="Id">The creator's profile.</param>
/// <param name="DisplayName">The creator's name.</param>
public sealed record Creator(Guid Id, string DisplayName);
