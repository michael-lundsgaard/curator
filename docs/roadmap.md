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
- [ ] Library:
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
- [ ] Documentation review before the app starts using the API: can a newcomer get from clone to a running
      API with passing tests from the README? Does `data-model.md` (still a draft) match the schema, and do the
      ADRs match the code? Is a short overview of how a request flows through the layers missing?
- [ ] Collections: tree, add/remove clips, sorting, cover image
- [ ] React app (`apps/app`, Vite): sign-in, generated API client, browse and upload
- [ ] Hosting and observability: where the API (and later the processing worker) runs and how it's
      deployed; structured logs with request logging, and somewhere to read logs and errors. Avoid hosts that
      sleep when idle: Render's free tier (used for asset-vault) spins down after 15 minutes without traffic
      and the next request waits for a cold start
- [ ] Video processing: transcode to playback version and generate thumbnail
  - Decide how thumbnails are served (ADR) before adding `thumbnailUrl` to `GET /assets`, which 0005
    assumes. Presigned URLs change on every request, so neither the browser nor Cloudflare's cache can keep
    them. Options: a public thumbnails bucket on a custom domain with random keys and immutable caching, or
    presigned URLs with the expiry rounded to fixed windows so they stay the same for a while
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
