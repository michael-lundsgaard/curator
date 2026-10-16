using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Curator.Infrastructure.Persistence;

/// <summary>
/// Npgsql only writes <see cref="DateTimeOffset"/> with offset 0 to <c>timestamp with time zone</c>, but clients
/// send local times (e.g. <c>recordedAt</c> with +02:00). The instant is kept; values are read back as UTC.
/// </summary>
internal sealed class UtcDateTimeOffsetConverter()
    : ValueConverter<DateTimeOffset, DateTimeOffset>(v => v.ToUniversalTime(), v => v);
