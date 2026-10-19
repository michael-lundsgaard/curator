# 0007: Collection tree

- **Status:** Accepted
- **Date:** 2026-10-19

## Context

Collections are a shared tree of clips, such as "R.E.P.O → Funny moments" (see [data-model.md](../data-model.md)
for the rules: generic tree, at most 5 levels, deleting a collection deletes its sub-collections, a clip can be
in several collections). The API has to:

- create a collection inside another one, move it with its sub-collections, and delete it with them, without
  cycles or trees deeper than 5 levels;
- give the app the whole tree for its sidebar;
- list a collection's clips, optionally with those of its sub-collections.

The scale is small: a handful of users and, over years, a few hundred collections. This ADR records how the
tree is stored, where its rules are checked, how concurrent changes are kept safe, and how it's read. The
decisions were made while building the Collections step and are written down here afterwards.

## Decision

### Storage: a `parent_id` column

- `collections.parent_id` references `collections` (`null` at the top level) with `ON DELETE CASCADE`, so
  deleting a collection deletes its sub-collections in the database, in one statement.
- `collection_assets` (`collection_id`, `asset_id`, `added_by`, `added_at`) holds the clips a collection was
  explicitly given. Both foreign keys cascade; assets are soft deleted, so the asset side only cascades when
  storage cleanup removes a row for good. A sub-collection's clips are never copied to its parents.

### Rules: the whole tree in memory, checked in the domain

- `LoadCollectionTreeAsync` loads every collection's `id` and `parent_id` in one query into a
  `CollectionTree` (Domain). It answers `DepthOf`, `HeightOf`, `IsWithin`, `CanAddChildTo`, `CanMove`,
  `CountWithin` and `IdsWithin` with plain C#, and is unit tested without a database.
- Moving is allowed when the target isn't the collection or one of its sub-collections (`IsWithin`) and the
  target's depth plus the moved subtree's height stays within 5 (`CanMove`).
- The tree throws if it finds a cycle in the data instead of looping. A cycle can't be created through the API.

### Concurrency: one advisory lock for the whole tree

- Create (inside a parent), move, delete and adding clips run their checks and their save inside
  `db.LockCollectionTreeAsync()`: a transaction with `pg_advisory_xact_lock` on one fixed key. The SQL is in
  `CuratorDbContext` (Infrastructure); Application only sees the method on `ICuratorDbContext`.
- Without it, two moves at the same moment could each pass the cycle check and together make a cycle, a create
  could land in a collection being deleted, and two adds of the same clip could both find it missing and fail on
  the primary key.
- A transaction-level lock is released on commit or rollback, so it can't be left behind, and it works through
  Supabase's pooler in transaction mode, where a session-level lock wouldn't.
- Editing a name and removing clips change only their own rows, so they don't take the lock.

### Reading

- `GET /collections` returns the whole tree as a flat list (`id`, `parentId`, `name`, `clipCount`), sorted by
  name. The app builds the sidebar from it and caches it. A thousand collections is about a millisecond in
  Postgres and 40–60 KB of gzipped JSON.
- A collection's clips come from `GET /assets?collectionId=…`, with the library's sorts, filters and cursor
  ([0005](0005-listing-and-searching-assets.md)). `includeSubCollections=true` uses `IdsWithin` and filters with
  `EXISTS (… collection_id = ANY(@ids))`, so a clip in several of them is listed once.
- Moving is its own endpoint (`POST /collections/{id}/move`), so an edit that leaves out `parentId` can't move a
  collection to the top level by accident.

### Sorting a collection's clips

- The sorts are the library's: `recorded`, `uploaded` and `title`. Every clip has exactly one of each key, however
  many collections it's in, so they work the same with sub-collections. The `uploaded` sort was called `added`
  until this ADR; it was renamed because in a collection it read as "added to this collection".
- There is no sort by when a clip was put into the collection (`collection_assets.added_at`) for now. In one
  collection that date is unambiguous, but with sub-collections a clip can have one per collection. If the sort is
  wanted, use the **latest** `added_at` within the tree ("most recently put somewhere in here"), as a subquery in
  the sort key and the cursor. The earliest would hide a clip that was just added to a new sub-collection.

## Alternatives considered

- **Recursive queries (`WITH RECURSIVE`)** for depth, cycles and sub-collections. Standard SQL, but EF Core can't
  write them in LINQ, so they'd need raw SQL behind an interface (0004 keeps Postgres-specific code out of
  Application). The tree is small enough to load whole, which keeps the rules in tested C#.
- **`ltree` paths** (`horror.phasmophobia.screams`). Fast subtree queries, but a move rewrites the path of every
  sub-collection, and it brings a Postgres extension and Npgsql types into the model.
- **A closure table** (a row per ancestor–descendant pair). Cheap reads at any size, but every create and move has
  to maintain it, which is the kind of check-then-write that needs a lock anyway.
- **Row locks (`SELECT … FOR UPDATE`) or serializable transactions** instead of the advisory lock. Row locks
  would have to cover every collection a move could conflict with (all ancestors of both places), and serializable
  transactions fail with errors that need retrying. One lock is simpler, and tree changes are rare.
- **A lock per collection or subtree.** Less waiting, but two moves in different places can still make a cycle
  together, so it would need lock ordering for no real gain at this scale.
- **Fetching the tree a level at a time (`?parentId=`).** Only needed with tens of thousands of collections;
  response compression comes first.

## Consequences

- Every tree change and every `includeSubCollections` listing loads the whole tree. Fine for thousands of
  collections. Far beyond that, move the subtree lookup to a recursive query behind an interface.
- Tree changes and adding clips wait for each other, across the whole tree. With a handful of users that's never
  noticeable.
- Deleting a collection deletes sub-collections others created inside it, which they couldn't delete directly.
  This is a known gap, left to the permissions review in the [roadmap](../roadmap.md).
- A clip has no position in a collection. Manual ordering would add a `position` column to `collection_assets`
  (data-model.md).
