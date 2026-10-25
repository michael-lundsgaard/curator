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
        replaces all three fields, not JSON Merge Patch: the edit form sends them all, and telling a missing
        field from `null` would need an `Optional<T>` with its own JSON converter, OpenAPI transformer and
        validation. `null` clears a field and a missing one is a `400`, so a field left out by mistake can't
        wipe it, and the OpenAPI document's `required` is what the API enforces. Optional fields of create
        requests have a default instead (`description`, `parentId`, `recordedAt`)
  - [x] Delete (`DELETE /assets/{id}`, uploader or admin, soft delete). Returns `200` with `id` and
        `deletedAt`; deleting again is `404`, since deleted assets are filtered out. Files stay in R2 until
        storage cleanup removes them
  - [x] Title sort order: keep the database's default collation for now. The known differences between
        environments, and the ICU collation fix if they cause a bug, are in ADR 0005, _Consequences_
- [x] Documentation review before the app starts using the API: README checked on a fresh clone (Node and a
      Supabase user were missing, and which endpoints need R2 keys), `data-model.md` matched to the schema,
      ADRs 0004 and 0005 matched to the code, and a request-flow overview in
      [architecture.md](architecture.md)
- [x] Collections → [0007](decisions/0007-collection-tree.md) (written afterwards):
  - [x] Covers: none for now, neither uploaded images nor a clip's thumbnail. The image question moved to
        thumbnails ([0006](decisions/0006-serving-thumbnails.md), proposed)
  - [x] Create (`POST /collections`, optional `parentId`, max depth 5) and get one (`GET /collections/{id}`)
  - [x] List (`GET /collections`): the whole tree as a flat list with `parentId`, sorted by name. The app
        builds the sidebar tree from it. Keep items lean (`id`, `parentId`, `name`).
        Scale: 1,000 collections is about a millisecond in Postgres and 150–200 KB of JSON (40–60 KB gzipped),
        fetched once and cached by the app, so not a concern. If it ever reaches tens of thousands, enable
        response compression first, then fetch one level at a time (`?parentId=`)
  - [x] Edit name and description (`PATCH /collections/{id}`, replaces both, like editing an asset), move
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
  - [x] A collection's clips through `GET /assets` with `collectionId` (and `includeSubCollections=true`,
        each clip once), reusing its sorts, filters and cursor. Unknown collection is `404`. See ADR 0005,
        _Filters and visibility_
- [ ] React app (`apps/app`, Vite, TypeScript), looking like [design.md](design.md) and its mocks. The API
      gaps the mocks revealed are added in the step whose screen first needs them, not all up front: real
      screens settle their open questions and may show gaps the mocks missed. Agent tooling is added the same
      way, at the step that needs it:
  - [x] OpenAPI document committed as [`apps/api/openapi.json`](../apps/api/openapi.json), kept current by a
        test, with the `/dev` endpoints in a document of their own →
        [0008](decisions/0008-openapi-document.md). Not written on every build: that runs the app's whole startup
        in every build, so startup could never need a database
  - [x] Bare Vite app (React, TypeScript, oxlint) with a client generated from that file: openapi-typescript
        types and openapi-fetch, kept current by `npm run check:api` →
        [0009](decisions/0009-generated-api-client.md). openapi-react-query adds TanStack Query at the Library step
  - [x] App shell: sidebar and top bar, theme tokens with light and dark (follows the OS; one light/dark button
        in the top bar remembers only a choice that differs from the OS, see [design.md](design.md)), Lucide
        icons. Styled with Tailwind CSS → [0010](decisions/0010-tailwind-css.md), routed with TanStack Router's
        file-based routes → [0011](decisions/0011-tanstack-router.md); Home, Library and Upload are placeholder
        routes. The Vite dev server proxies `/api` to the API, so the API needs no CORS
        until the app is hosted. The VS Code task `dev` starts the API and the app together
    - Tooling: Playwright MCP (`.mcp.json`), so the agent can open the app, click through it and compare
      screenshots with the mocks in both themes
  - [x] Sign in, sign out and reset password with Supabase's auth-js (supabase-js without the clients the app
        doesn't use), pages behind a pathless `_authenticated` layout route, `GET /me` for the account menu →
        [0012](decisions/0012-sign-in-in-the-app.md)
    - Tooling: read-only database lookups against the local Docker Postgres with `psql` through
      `docker compose exec` (in CLAUDE.md). The Supabase MCP isn't added: the few auth settings are set once by
      hand (README). The agent signs in as a test user of its own, a Member, whose login the Playwright MCP fills
      in from `.claude/test-user.local` (`--secrets`), so the password never reaches the agent. An admin test user
      comes with the admin screens
  - [x] Library: grid and list, search, sort, "Uploaded by", "Load more" with the cursor, through TanStack Query
        (openapi-react-query). Search, filter and sort live in the URL (`/library?q=…&sort=Title`), so a search can
        be linked and Back undoes a filter. Grid or list is a preference per device in `localStorage`, like the
        theme, so a shared link doesn't choose it for the person opening it. The search box is in the top bar and searches from any page, after
        a pause in typing. Selecting clips to add to a collection comes with Collections, and cards link to a clip
        with Clip details
    - [x] API first: list profiles (`GET /profiles`: id and display name, sorted by name, not paged) for the
          "Uploaded by" filter, later also the admin members list. Members don't see roles: nothing in the app
          needs them yet, so they're added for admins with the members list
  - [x] Clip details at `/watch/{id}`: playback, edit, delete. Library cards and rows link to it. Edit and Delete
        show for the uploader or an admin, the API's rule. A saved edit is merged into the clip, not fetched again,
        because a fetch signs new URLs and a new URL reloads the video. When the signed URL expires, the page
        fetches a new one and the video carries on from where it was. "Copy link" from the mock comes with share
        links
    - [x] API first: file size on `GET /assets/{id}` (`sourceSizeBytes`)
  - [ ] Collections: sidebar tree, collection page with sub-collections, add and remove clips, create, rename,
        move and delete. On clip details, the clip's collections and "Add to collection", as in the mock (the
        API doesn't list an asset's collections yet)
  - [ ] Upload: bulk queue (create upload, `PUT` to R2, complete), a few files at a time, optionally into a
        collection
  - [ ] Home: recently added, collections, and one's own uploads that need attention
    - [ ] API first: one's own `Pending` and `Failed` uploads for "needs attention" (`GET /assets` hides
          `Pending`). Retrying needs a new upload URL for the same asset, or the clip is discarded and uploaded
          again
  - [ ] Maybe: collapse the sidebar to its icons, with a button, remembered per device like the theme. Not
        hidden on clip details by itself: navigation that comes and goes from page to page confuses. Goes
        with a better phone navigation: today the sidebar sits above the page on a phone and pushes it down.
        Instead, a menu button that opens it as a drawer, or a bar at the bottom
- [ ] Browser tests with Playwright Test, once the app's main screens exist, so later features can lean on them
      (their own dev server and data, fixtures, layout checks such as no sideways scrolling, and screenshots to look
      at). Until then the agent checks screens by hand with the Playwright MCP. Needs an ADR for test data (a
      database of its own) and sign-in (a fake session and test-signed tokens instead of Supabase), and keeps R2
      out
- [ ] Hosting and observability: where the API (and later the processing worker) runs and how it's
      deployed; structured logs with request logging, and somewhere to read logs and errors. Avoid hosts that
      sleep when idle: Render's free tier (used for asset-vault) spins down after 15 minutes without traffic
      and the next request waits for a cold start
  - API versioning: none while our app is the only client, since both deploy together and change in the same
    commit. When an outside client appears (an agent, a script, a mobile app), choose a scheme in an ADR (likely
    `Asp.Versioning` with `/v1` routes, keeping today's routes as v1). The committed `openapi.json` shows every
    breaking change as a diff; in CI, a tool like `oasdiff` could flag them
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
- [ ] Dashboards: admins see everyone's stuck uploads (`Pending`, `Failed`) to diagnose "missing" clips and
      discard them, and the members list; profile settings for everyone. One's own stuck uploads are on Home
      (see the app's Home step above)
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
