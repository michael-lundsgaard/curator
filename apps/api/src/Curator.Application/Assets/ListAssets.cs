using System.Buffers.Text;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Curator.Application.Common;
using Curator.Application.Persistence;
using Curator.Domain.Assets;
using Microsoft.EntityFrameworkCore;

namespace Curator.Application.Assets;

/// <summary>
/// Lists the shared library a page at a time, continuing after the last asset of the previous page (keyset
/// pagination). <see cref="AssetStatus.Pending"/> assets aren't listed. See ADR 0005.
/// </summary>
public static class ListAssets
{
    public const int DefaultLimit = 50;
    public const int MaxLimit = 100;

    public enum AssetSort
    {
        /// <summary>When the clip was recorded, or uploaded if that's unknown. Newest first by default.</summary>
        Recorded,

        /// <summary>When the clip was uploaded. Newest first by default.</summary>
        Added,

        /// <summary>Title, ignoring case. A to Z by default.</summary>
        Title,
    }

    public enum SortOrder
    {
        Asc,
        Desc,
    }

    /// <param name="Q">Words that must all appear in the title or description, e.g. <c>funny scream</c>.</param>
    /// <param name="Sort">What to sort by. Defaults to <c>Recorded</c>.</param>
    /// <param name="Order">Sort direction. Defaults to <c>Desc</c> for dates and <c>Asc</c> for titles.</param>
    /// <param name="UploadedBy">Only assets uploaded by this profile.</param>
    /// <param name="RecordedFrom">Only assets recorded at or after this time.</param>
    /// <param name="RecordedTo">Only assets recorded before this time.</param>
    /// <param name="Limit">Assets per page, at most 100. Defaults to 50.</param>
    /// <param name="Cursor">
    /// <c>nextCursor</c> of the previous page. Send the same other parameters as for that page.
    /// </param>
    public sealed record Query(
        [MaxLength(200)] string? Q = null,
        AssetSort Sort = AssetSort.Recorded,
        SortOrder? Order = null,
        Guid? UploadedBy = null,
        DateTimeOffset? RecordedFrom = null,
        DateTimeOffset? RecordedTo = null,
        [Range(1, MaxLimit)] int Limit = DefaultLimit,
        [MaxLength(1000)] string? Cursor = null);

    /// <param name="Items">The assets on this page.</param>
    /// <param name="NextCursor">Pass as <c>cursor</c> to get the next page. <c>null</c> on the last page.</param>
    public sealed record Response(IReadOnlyList<Item> Items, string? NextCursor);

    /// <param name="UploadedBy">Who uploaded the clip.</param>
    /// <param name="RecordedAt">When the clip was captured, if known.</param>
    /// <param name="CreatedAt">When the clip was uploaded.</param>
    public sealed record Item(
        Guid Id,
        string Title,
        string? Description,
        AssetStatus Status,
        Uploader UploadedBy,
        DateTimeOffset? RecordedAt,
        DateTimeOffset CreatedAt,
        long? DurationMs,
        int? Width,
        int? Height);

    public sealed class Handler(ICuratorDbContext db)
    {
        public async Task<Result<Response>> HandleAsync(Query query, CancellationToken cancellationToken)
        {
            var parameters = Parameters.From(query);

            Position? after = null;
            if (query.Cursor is not null)
            {
                var cursor = Cursor.Decode(query.Cursor);
                if (cursor is null || cursor.Parameters != parameters)
                {
                    return Error.Invalid(
                        "The cursor is invalid, or belongs to a query with other parameters. Start from the first page.");
                }

                after = cursor.After;
            }

            var assets = Filter(db.Assets.Where(a => a.Status != AssetStatus.Pending), parameters);
            assets = Sort(assets, parameters.Sort, parameters.Descending, after);

            // One extra row tells whether there's a next page.
            var rows = await assets
                .Take(query.Limit + 1)
                .Select(a => new
                {
                    Item = new Item(
                        a.Id,
                        a.Title,
                        a.Description,
                        a.Status,
                        // A subquery rather than a join, so the sorted page stays as it is.
                        new Uploader(
                            a.UploadedBy,
                            db.Profiles.Where(p => p.Id == a.UploadedBy).Select(p => p.DisplayName).Single()),
                        a.RecordedAt,
                        a.CreatedAt,
                        a.DurationMs,
                        a.Width,
                        a.Height),

                    // The title key as the database lowercases it, so the next page continues at the same place.
                    TitleKey = a.Title.ToLower(),
                })
                .ToListAsync(cancellationToken);

            string? nextCursor = null;
            if (rows.Count > query.Limit)
            {
                rows.RemoveAt(rows.Count - 1);
                var last = rows[^1];
                nextCursor = new Cursor(
                    parameters,
                    new Position(last.Item.RecordedAt ?? last.Item.CreatedAt, last.Item.CreatedAt, last.TitleKey, last.Item.Id))
                    .Encode();
            }

            return new Response(rows.Select(r => r.Item).ToList(), nextCursor);
        }

        private static IQueryable<Asset> Filter(IQueryable<Asset> assets, Parameters parameters)
        {
            if (parameters.UploadedBy is { } uploadedBy)
            {
                assets = assets.Where(a => a.UploadedBy == uploadedBy);
            }

            if (parameters.RecordedFrom is { } from)
            {
                assets = assets.Where(a => (a.RecordedAt ?? a.CreatedAt) >= from);
            }

            if (parameters.RecordedTo is { } to)
            {
                assets = assets.Where(a => (a.RecordedAt ?? a.CreatedAt) < to);
            }

            // lower(...) LIKE '%word%' is served by the pg_trgm indexes on lower(title) and lower(description).
            foreach (var word in parameters.Words)
            {
                var pattern = $"%{EscapeLike(word.ToLowerInvariant())}%";
                assets = assets.Where(a =>
                    EF.Functions.Like(a.Title.ToLower(), pattern, LikeEscape) ||
                    EF.Functions.Like(a.Description!.ToLower(), pattern, LikeEscape));
            }

            return assets;
        }

        /// <summary>Orders by the sort key and then the ID, and continues after <paramref name="after"/>.</summary>
        private static IQueryable<Asset> Sort(
            IQueryable<Asset> assets,
            AssetSort sort,
            bool descending,
            Position? after)
        {
            switch (sort)
            {
                case AssetSort.Recorded:
                    if (after is not null)
                    {
                        var (key, id) = (after.Recorded, after.Id);
                        assets = descending
                            ? assets.Where(a => (a.RecordedAt ?? a.CreatedAt) < key ||
                                ((a.RecordedAt ?? a.CreatedAt) == key && a.Id.CompareTo(id) < 0))
                            : assets.Where(a => (a.RecordedAt ?? a.CreatedAt) > key ||
                                ((a.RecordedAt ?? a.CreatedAt) == key && a.Id.CompareTo(id) > 0));
                    }

                    return descending
                        ? assets.OrderByDescending(a => a.RecordedAt ?? a.CreatedAt).ThenByDescending(a => a.Id)
                        : assets.OrderBy(a => a.RecordedAt ?? a.CreatedAt).ThenBy(a => a.Id);

                case AssetSort.Added:
                    if (after is not null)
                    {
                        var (key, id) = (after.Added, after.Id);
                        assets = descending
                            ? assets.Where(a => a.CreatedAt < key || (a.CreatedAt == key && a.Id.CompareTo(id) < 0))
                            : assets.Where(a => a.CreatedAt > key || (a.CreatedAt == key && a.Id.CompareTo(id) > 0));
                    }

                    return descending
                        ? assets.OrderByDescending(a => a.CreatedAt).ThenByDescending(a => a.Id)
                        : assets.OrderBy(a => a.CreatedAt).ThenBy(a => a.Id);

                case AssetSort.Title:
                    if (after is not null)
                    {
                        var (key, id) = (after.Title, after.Id);
                        assets = descending
                            ? assets.Where(a => string.Compare(a.Title.ToLower(), key) < 0 ||
                                (a.Title.ToLower() == key && a.Id.CompareTo(id) < 0))
                            : assets.Where(a => string.Compare(a.Title.ToLower(), key) > 0 ||
                                (a.Title.ToLower() == key && a.Id.CompareTo(id) > 0));
                    }

                    return descending
                        ? assets.OrderByDescending(a => a.Title.ToLower()).ThenByDescending(a => a.Id)
                        : assets.OrderBy(a => a.Title.ToLower()).ThenBy(a => a.Id);

                default:
                    throw new ArgumentOutOfRangeException(nameof(sort), sort, "Unknown sort.");
            }
        }

        private const string LikeEscape = "\\";

        private static string EscapeLike(string value) =>
            value.Replace(LikeEscape, LikeEscape + LikeEscape)
                .Replace("%", LikeEscape + "%")
                .Replace("_", LikeEscape + "_");
    }

    /// <summary>The query a cursor belongs to: everything except the page size and the cursor itself.</summary>
    private sealed record Parameters(
        string? Q,
        AssetSort Sort,
        bool Descending,
        Guid? UploadedBy,
        DateTimeOffset? RecordedFrom,
        DateTimeOffset? RecordedTo)
    {
        public static Parameters From(Query query) => new(
            string.IsNullOrWhiteSpace(query.Q) ? null : query.Q.Trim(),
            query.Sort,
            (query.Order ?? (query.Sort == AssetSort.Title ? SortOrder.Asc : SortOrder.Desc)) == SortOrder.Desc,
            query.UploadedBy,
            query.RecordedFrom,
            query.RecordedTo);

        public IEnumerable<string> Words =>
            Q?.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Distinct() ?? [];
    }

    /// <summary>The last asset of a page. Only the key of the cursor's sort is used.</summary>
    private sealed record Position(DateTimeOffset Recorded, DateTimeOffset Added, string Title, Guid Id);

    /// <summary>Opaque to clients: base64url-encoded JSON.</summary>
    private sealed record Cursor(Parameters Parameters, Position After)
    {
        public string Encode() => Base64Url.EncodeToString(JsonSerializer.SerializeToUtf8Bytes(this));

        public static Cursor? Decode(string value)
        {
            try
            {
                return JsonSerializer.Deserialize<Cursor>(Base64Url.DecodeFromChars(value));
            }
            catch (Exception e) when (e is FormatException or JsonException)
            {
                return null;
            }
        }
    }
}
