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
  - [ ] Get one (`GET /assets/{id}`) with signed playback and thumbnail URLs (playback falls back to the
        original until processing exists)
  - [ ] Edit title, description and `recorded_at` (`PATCH /assets/{id}`, uploader or admin)
  - [ ] Delete (`DELETE /assets/{id}`, uploader or admin, soft delete)
- [ ] Collections: tree, add/remove clips, sorting, cover image
- [ ] React app (`apps/app`, Vite): sign-in, generated API client, browse and upload
- [ ] Video processing: transcode to playback version and generate thumbnail. Also sweep abandoned
      uploads: delete assets still `Pending` after 24 hours, and their R2 objects
- [ ] Dashboards: admins see stuck uploads (`Pending`, `Failed`) to diagnose "missing" clips; possibly a user
      dashboard for one's own uploads (retry or discard) and profile settings
- [ ] Invitations (disable sign-up, `inviteUserByEmail`)
- [ ] Favorites (visible to everyone)
- [ ] Share links (single assets only)
