# Data model

**Status:** Draft. Settled decisions are marked ✅, and things still being considered are listed under
_Open questions_.

## Settled

- ✅ **Shared library.** Every invited user sees everything. `uploaded_by` / `created_by` are for
  attribution and edit/delete rights, not visibility.
- ✅ **No tags** for now.
- ✅ **Storage keys, not URLs**, are stored. The API returns short-lived signed URLs.
- ✅ **UUID v7** IDs (`Guid.CreateVersion7()`).
- ✅ **Soft delete** via `deleted_at`. `status` describes the upload/processing lifecycle only.
- ✅ **Status stored as text** (`HasConversion<string>()`), so new states don't require data migrations.

## Proposed schema

```
profiles           id (= JWT sub), display_name, created_at

assets             id, uploaded_by → profiles, title, description,
                   original_filename, content_type, status,
                   source_key, source_size_bytes,
                   playback_key, playback_size_bytes,
                   thumbnail_key, processing_error,
                   duration_ms, width, height, recorded_at,
                   created_at, updated_at, deleted_at

collections        id, parent_id? → collections, created_by → profiles,
                   name, description, cover_key?, position,
                   created_at, updated_at

collection_assets  collection_id, asset_id, position, added_by, added_at
                   PK (collection_id, asset_id)

share_links        (later) id, token (unique), collection_id, created_by,
                   expires_at, revoked_at, created_at
```

### Asset lifecycle

```
Pending → Uploaded → Processing → Ready
   └──────────┴───────────┴──────→ Failed
```

1. The API creates the asset (`Pending`) and returns a presigned R2 upload URL.
2. The browser uploads straight to R2, then tells the API it's done (`Uploaded`).
3. A worker transcodes the original into a smaller playback version and creates a thumbnail
   (`Processing` → `Ready`, or `Failed` with `processing_error`).
4. Policy (later): delete `source_key` once the asset is `Ready`, to save storage.

`recorded_at` is when the clip was captured. The browser sends `File.lastModified` on upload, and
processing can correct it from the video's metadata.

### Collections

- A generic tree using `parent_id`. Nothing in it is game- or session-specific: "Best of 2026 → Screams"
  and "R.E.P.O → Funny moments" are the same thing.
- Any depth is allowed. The domain rejects cycles (moving a collection into one of its own descendants).
- A clip is stored only in the collections it was explicitly added to, and can be in several.
  "Show clips from sub-collections too" is a display option, not duplicated data.
- "Sessions" are a view: the clips in a collection grouped by `recorded_at`, not a structure.
- Cover: an uploaded image (`cover_key`). If there is none, the UI uses the thumbnail of the newest
  clip in the collection or its sub-collections.
- Deleting a collection never deletes clips.

## Open questions

- [ ] Is the generic collection tree + `recorded_at` the right model? (Alternative considered and
      rejected: separate `Game` and `Session` entities, which are too specific.)
- [ ] Should the tree have a maximum depth, or is "no cycles" enough?
- [ ] Deleting a collection that has sub-collections: delete them too, or move them up a level?
- [ ] Who can edit and delete: only the uploader/creator, or anyone in the group?
- [ ] Delete the original upload once processing succeeds?
- [ ] Share links: collections only, or single assets too?
