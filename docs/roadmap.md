# Roadmap

Built step by step. Each step is small and shippable. Order may change.

- [x] API foundation and tooling
- [x] Choose hosting for database and auth → [0001](decisions/0001-supabase-for-postgres-and-auth.md)
- [x] Settle the open questions in [data-model.md](data-model.md) (permissions deferred to
      authentication, favorites scope to the favorites step)
- [x] EF Core + Npgsql: `profiles` + `assets`, first migration, database health check, local Postgres in
      Docker → [0002](decisions/0002-local-postgres-in-docker.md)
- [x] Authentication: validate Supabase JWTs, create the profile on first request, decide roles and
      edit/delete permissions → [0003](decisions/0003-authentication-and-permissions.md)
- [x] Uploads, following [0004](decisions/0004-application-structure.md):
  - [x] Create asset and presigned R2 upload URL (`POST /assets/uploads`)
  - [x] Mark as uploaded (`POST /assets/{id}/complete-upload`): check the object exists in R2 and its size
        matches. Failures are results, 0004 accepted
  - [x] Sign `Content-Length` into the upload URL, so R2 rejects a `PUT` of another size (403) and nothing
        is stored
  - [x] R2 bucket CORS for browser `PUT`s from the app's origin (`infra/r2/cors.json`, applied with
        wrangler). Add the production origin when the app is deployed
- [x] Library:
  - [x] ADR [0005](decisions/0005-listing-and-searching-assets.md): pagination (cursor vs offset), search
        (`pg_trgm` vs full-text) and where Postgres-specific query code lives
  - [x] List assets (`GET /assets`): cursor pagination, sort, search, filters, `pg_trgm` indexes
  - [x] Get one (`GET /assets/{id}`) with signed playback and thumbnail URLs (playback falls back to the
        original until processing exists)
  - [x] Edit title, description and `recorded_at` (`PATCH /assets/{id}`, uploader or admin). The body
        replaces all three fields (missing or `null` clears), not JSON Merge Patch: the edit form sends them
        all, and telling a missing field from `null` would need an `Optional<T>` with its own JSON converter,
        OpenAPI transformer and validation
  - [x] Delete (`DELETE /assets/{id}`, uploader or admin, soft delete). Returns `200` with `id` and
        `deletedAt`; deleting again is `404`, since deleted assets are filtered out. Files stay in R2 until
        storage cleanup removes them
  - [x] Title sort order: keep the database's default collation for now. The known differences between
        environments, and the ICU collation fix if they cause a bug, are in ADR 0005, _Consequences_
- [x] Documentation review before the app starts using the API: README checked on a fresh clone (Node and a
      Supabase user were missing, and which endpoints need R2 keys), `data-model.md` matched to the schema,
      ADRs 0004 and 0005 matched to the code, and a request-flow overview in
      [architecture.md](architecture.md)
- [ ] Collections:
  - [x] Covers: none for now, neither uploaded images nor a clip's thumbnail. The image question moved to
        thumbnails ([0006](decisions/0006-serving-thumbnails.md), proposed)
  - [x] Create (`POST /collections`, optional `parentId`, max depth 5) and get one (`GET /collections/{id}`)
  - [x] List (`GET /collections`): the whole tree as a flat list with `parentId`, sorted by name. The app
        builds the sidebar tree from it. Keep items lean (`id`, `parentId`, `name`).
        Scale: 1,000 collections is about a millisecond in Postgres and 150–200 KB of JSON (40–60 KB gzipped),
        fetched once and cached by the app, so not a concern. If it ever reaches tens of thousands, enable
        response compression first, then fetch one level at a time (`?parentId=`)
  - [x] Edit name and description (`PATCH /collections/{id}`, replaces both), move
        (`POST /collections/{id}/move` with `parentId`; no cycles, depth counts the moved sub-collections) and
        delete with sub-collections (`DELETE /collections/{id}`, returns `deletedCount`). Creator or admin. Moving
        is its own endpoint so an edit that leaves out `parentId` can't move a collection to the top by accident.
        Create (inside a parent), move and delete take a Postgres advisory lock (`pg_advisory_xact_lock`) around
        their tree checks and save, so two moves at the same moment can't make a cycle and a create can't land in
        a collection being deleted
  - [x] Add and remove clips (`collection_assets`): `POST /collections/{id}/assets` with up to 100 `assetIds`
        (already added ones are skipped, deleted or pending ones fail the request; returns `addedCount`) and
        `POST /collections/{id}/assets/remove` the same way (ones not in it are skipped; returns `removedCount`),
        for selecting several clips in the app. A `POST`, since a body on `DELETE` is often dropped. Anyone, in
        any collection. Adding takes the collection tree
        lock, so it can't race a delete of the collection or another add of the same clip. `clipCount` on
        `GET /collections` items for sidebar badges: the collection's own clips, not its sub-collections', and not
        deleted ones
  - [ ] A collection's clips through `GET /assets` with `collectionId` (and sub-collections as an option),
        reusing its sorts and cursor
- [ ] React app (`apps/app`, Vite): sign-in, generated API client, browse and upload
- [ ] Hosting and observability: where the API (and later the processing worker) runs and how it's
      deployed; structured logs with request logging, and somewhere to read logs and errors. Avoid hosts that
      sleep when idle: Render's free tier (used for asset-vault) spins down after 15 minutes without traffic
      and the next request waits for a cold start
- [ ] Video processing: transcode to playback version and generate thumbnail
  - Choose between options A (public bucket on a custom domain) and B (presigned URLs per response) in
    [0006](decisions/0006-serving-thumbnails.md): find out what a domain and the rest of A cost. Then store
    thumbnails under versioned keys and add `thumbnailUrl` to `GET /assets`, which 0005 assumes
- [ ] Storage cleanup: a scheduled job that keeps R2 in step with the database
  - Abandoned uploads: delete assets still `Pending` after 24 hours, and their R2 objects
  - Deleted clips: remove the original, playback version and thumbnail of soft-deleted assets after a grace
    period
  - Orphaned objects with no asset row (e.g. after a local database reset): find them by listing the bucket,
    or an R2 lifecycle rule on `curator-dev` that expires objects after N days
- [ ] Dashboards: admins see stuck uploads (`Pending`, `Failed`) to diagnose "missing" clips; possibly a user
      dashboard for one's own uploads (retry or discard) and profile settings
- [ ] Invitations (disable sign-up, `inviteUserByEmail`)
- [ ] Favorites (visible to everyone)
- [ ] Share links (single assets only)
  - Short, unguessable tokens of their own, not the asset ID: these are the URLs that leave the app. Inside the
    app, `/watch/{id}` with the UUID is fine (decided against slugs and YouTube-style short IDs: titles change
    and repeat, and nobody types these links)
- [ ] Permissions review, once invitations and real use show how roles work out. Today the creator or an admin
      edits, moves and deletes a collection (ADR [0003](decisions/0003-authentication-and-permissions.md)),
      on the assumption that the core group are admins and only newcomers stay members
  - Known gap: deleting a collection also deletes sub-collections others created inside it, which they can't
    delete directly. Options considered: also allow the creator of any collection above it, or let everyone
    change collections
  - Lower roles (e.g. viewers who can't upload or change collections), and how a member becomes an admin
    without SQL
