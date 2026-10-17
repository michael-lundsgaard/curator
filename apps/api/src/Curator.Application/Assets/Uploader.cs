namespace Curator.Application.Assets;

/// <summary>Who uploaded a clip, as shown next to it.</summary>
public sealed record Uploader(Guid Id, string DisplayName);
