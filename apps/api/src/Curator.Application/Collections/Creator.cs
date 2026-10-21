namespace Curator.Application.Collections;

/// <summary>Who created a collection, as shown next to it.</summary>
public sealed record Creator(Guid Id, string DisplayName);
