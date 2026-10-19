# 0005: Listing and searching assets

- **Status:** Accepted
- **Date:** 2026-10-17

## Context

The library step adds `GET /assets`: the shared library as a grid the app scrolls through, with a search box,
a few filters and the sort options from [data-model.md](../data-model.md) (recorded, uploaded, title). It has to
decide three things before the first query is written:

1. **Pagination:** offset (`?page=3`) or cursor (`?cursor=…`).
2. **Search:** `pg_trgm` (substring matching) or Postgres full-text search (`tsvector`).
3. **Where Postgres-specific query code lives**, given that [0004](0004-application-structure.md) keeps
   Npgsql out of `Curator.Application`.

The scale is small: a handful of users and, over years, maybe a few thousand clips. Almost any choice performs
well enough, so these are mainly about behaviour and keeping the code simple.

## Decision

### Pagination: cursor (keyset)

- `GET /assets?sort=recorded&order=desc&limit=50&cursor=…` returns `{ items, nextCursor }`. `nextCursor` is
  `null` on the last page. The app keeps requesting the next page while scrolling.
- The query orders by the sort key **and then `id`** as a tie-breaker, and continues after the last row:
  `WHERE (key, id) < (@lastKey, @lastId)`, written in LINQ as
  `key < lastKey || (key == lastKey && id < lastId)`.
- The cursor is opaque to clients: base64url-encoded JSON with the sort, order, search, filters, last key and
  last ID. A cursor used with different parameters is rejected (400), so a page never mixes two queries.
- `limit` defaults to 50, maximum 100.
- **No total count** in the response. The grid scrolls, so it doesn't need page numbers. If the app wants
  "312 clips", a count can be added later as a separate field or endpoint.

Why cursor: uploads happen while others browse. With offsets, every new clip at the top shifts all later pages
by one, so infinite scroll shows duplicates (or skips clips when one is deleted). A keyset continues after
the last row it saw, wherever that row is now. It also stays fast on deep pages, though that won't matter at
this scale.

### Sort keys

| `sort`     | Key                                 | Default order |
| ---------- | ----------------------------------- | ------------- |
| `recorded` | `coalesce(recorded_at, created_at)` | `desc`        |
| `uploaded` | `created_at`                        | `desc`        |
| `title`    | `lower(title)`                      | `asc`         |

- `recorded_at` is nullable. A keyset can't compare with `NULL`, so a clip without one sorts by when it was
  uploaded. The browser sends `File.lastModified`, so this should be rare.
- Titles sort case-insensitively, so "apple" doesn't end up after "Zebra".

### Search: substring matching, backed by `pg_trgm`

- `?q=funny repo` splits the text on whitespace. **Every word must appear** in the title or description,
  case-insensitively and anywhere in the text: "scre" finds "Screams", and "funny repo" finds "Repo night:
  funny moments".
- Search is a filter. It doesn't change the sort or rank by relevance, so it works with every sort and with
  the cursor.
- Each word becomes `lower(title) LIKE '%word%' OR lower(description) LIKE '%word%'`, with `%`, `_` and `\`
  in the word escaped.
- A `pg_trgm` GIN index on `lower(title)` and one on `lower(description)` serve these `LIKE`s. Without them
  Postgres scans the table, which would be fine for a few thousand rows too, so the indexes are cheap
  insurance rather than a necessity.

Why trigrams rather than full-text: titles are short, often a filename, a game name or a nickname, and in
Danish or English (or both in one title). Full-text search matches whole words after stemming with a
language config. It doesn't find partial words ("scre"), a wrong language config stems Danish words oddly,
and its strength (ranking long documents) isn't needed for one-line titles. Substring matching is what a
search box over titles is expected to do.

Punctuation isn't normalized: "repo" doesn't find "R.E.P.O". That's acceptable for now. Typo tolerance
(`similarity()` / `%` from `pg_trgm`) is possible later with the same index.

**Follow-up if needed: a normalized `search_text` column.** The domain computes it in C# whenever the title
or description changes: lowercased, dots and apostrophes removed, other punctuation turned into spaces
("R.E.P.O – Funny moments" → "repo funny moments"). Search words are normalized the same way and matched with
`EF.Functions.Like(a.SearchText, pattern)`, and one trigram index on `search_text` replaces the two above.
Because lowercasing happens in .NET, it also removes the dependency on the database's locale (see
_Consequences_). Cost: a column, a tested normalize function, and a migration that backfills existing rows.
Do it when punctuation search is missed, or if the Danish-letters test fails.

### Postgres-specific code: in the schema, not in the queries

- Handlers write provider-neutral LINQ: `EF.Functions.Like(a.Title.ToLower(), pattern, "\\")` is part of
  `Microsoft.EntityFrameworkCore` and translates to `lower(title) LIKE @pattern`. No `EF.Functions.ILike`, so
  `Curator.Application` still doesn't reference Npgsql, as 0004 decided.
- What makes it fast is Postgres-specific, and lives in Infrastructure: the `pg_trgm` extension
  (`HasPostgresExtension` in `CuratorDbContext`) and the GIN expression indexes (`gin_trgm_ops`). EF Core
  can't model expression indexes, so they're SQL in the `AddAssetSearch` migration. Verified with `EXPLAIN`:
  Postgres uses both indexes even for a generic plan with the pattern as a parameter.
- The listing query (filters, search, sort, cursor) lives in the `ListAssets` handler. A collection's clips
  are a filter on the same endpoint (below), so nothing had to be extracted. If another use case needs the
  same filtering, extract it into `IQueryable<Asset>` extension methods, as 0004 says.
- If a future query genuinely needs a Postgres function in LINQ (`similarity()`, `tsvector`), add an
  interface in Application with the query in Infrastructure, like `IFileStorage`. Revisit this ADR then.

### Filters and visibility

- `uploadedBy` (a profile ID, e.g. "my clips") and `recordedFrom` / `recordedTo` (on the same
  `coalesce(recorded_at, created_at)` key).
- `collectionId` lists a collection's clips, with every sort and filter above; `includeSubCollections=true`
  adds those in its sub-collections at every level. The handler loads the tree, collects the IDs and filters
  with `EXISTS (… collection_id = ANY(@ids))`, so a clip in several of them is listed once. An unknown
  collection is 404, and `includeSubCollections` without `collectionId` is 400. Both are part of the cursor.
  Why there's no sort by when a clip was put into the collection, and how to add one, is in
  [0007](0007-collection-tree.md).
- `Pending` assets are never listed: they're uploads that haven't finished (or were abandoned), with nothing
  to show. The uploading browser already knows about them. Deleted assets are hidden by the query filter.
- `GET /assets/{id}` follows the same rule, except that the uploader can get their own `Pending` asset (without
  a playback URL), so the `Location` of `POST /assets/uploads` works straight away. For everyone else it's 404,
  as if it didn't exist.
- `Uploaded`, `Processing` and `Failed` assets are listed, so a clip appears as soon as its upload completes,
  before it has a thumbnail.
- Finding stuck uploads (`Pending`, and later `Failed`) belongs in a separate admin dashboard, and possibly a
  user dashboard for one's own uploads. Those are separate views with their own endpoints, not a hidden option
  on the library list.

## Alternatives considered

- **Offset pagination.** Simpler, gives page numbers and totals, and the reference project used it. Rejected
  for the shifting pages under concurrent uploads, which infinite scroll makes visible.
- **Full-text search** (`tsvector` with a generated column and a GIN index). Rejected for the reasons above:
  no partial words, language-dependent stemming. Could be added alongside if descriptions grow long.
- **`ILIKE` via `EF.Functions.ILike`.** The most direct Postgres query, and the trigram index supports it on
  the plain column. Rejected because it brings Npgsql into Application for no behavioural gain over
  `lower(...) LIKE`.
- **Referencing Npgsql from Application** and dropping the rule from 0004. Simple, and the tests run against
  Postgres anyway. Not chosen while the neutral LINQ costs nothing; it's the fallback if that changes.
- **A case-insensitive (nondeterministic) collation** on `title`. Elegant, but `LIKE` on nondeterministic
  collations needs Postgres 18 and isn't served by a trigram index.

## Consequences

- No "jump to page 7" and no total count without extra work.
- A cursor is only valid for the query it came from. Changing the search or sort starts from the top, which
  is what the UI does anyway.
- `lower()` follows the database's locale. If Supabase's database lowercases only ASCII, "Øl" wouldn't match
  "øl". An integration test with Danish letters checks this against Postgres; if it fails, switch to the
  `search_text` column described under _Search_.
- Title sorting follows the database's default collation, which comes from the host's C library and differs
  between environments: `postgres:17-alpine` (musl, local and tests) compares code points, so Å, Æ and Ø sort
  after Z, while Supabase uses glibc. Digits compare as text everywhere ("Clip 10" before "Clip 2"). Accepted
  for now (decided 2026-10-18). If sorting turns out wrong, the fix that was tried and works: an ICU collation
  on the column, so every environment sorts the same and the LINQ needs no `COLLATE`:
  - `modelBuilder.HasCollation("titles", locale: "en-u-kn", provider: "icu")` in `CuratorDbContext`
    (English rules with Å, Æ and Ø next to A and O, as most of the group use English systems; `kn` compares
    numbers by value) and `builder.Property(a => a.Title).UseCollation("titles")` in `AssetConfiguration`.
    `lower(title)` takes the column's collation, and the trigram index keeps working.
  - Expected order: Åben, apple, Bil, Clip 2, Clip 10, Øl, Oscar, Zebra. Use it in an integration test.
  - Check the generated migration's `Down`: EF drops the collation before resetting the column, which fails.
    Swap the two steps.
- Listed items were meant to carry signed thumbnail URLs (presigning is computed locally, so it costs nothing
  per item). They don't yet: there are no thumbnails before processing, and presigned URLs change on every
  request, so browsers can't cache them. How thumbnails are served is decided with video processing (see the
  [roadmap](../roadmap.md)).

## References

- Use The Index, Luke: "Paging Through Results" (keyset pagination).
- PostgreSQL docs: `pg_trgm`, index support for `LIKE` and `ILIKE`.
