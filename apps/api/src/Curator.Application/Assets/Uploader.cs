namespace Curator.Application.Assets;

/// <summary>Who uploaded a clip, as shown next to it.</summary>
/// <param name="Id">The uploader's profile.</param>
/// <param name="DisplayName">The uploader's name.</param>
public sealed record Uploader(Guid Id, string DisplayName);
