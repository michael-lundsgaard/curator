# 0006: Serving thumbnails

- **Status:** Proposed. C and D are rejected; A or B is chosen when video processing creates thumbnails,
  after finding out what A costs.
- **Date:** 2026-10-19

## Context

Video processing will give every clip a thumbnail, shown in grids: the library (`GET /assets`) and, later, a
collection's clips. Everything in R2 is private and read through presigned URLs created per request, as
`GET /assets/{id}` does for playback. That works for one video, but a grid of 50 thumbnails is downloaded again
whenever the URLs change, and the images flicker in.

Collections don't get covers for now, neither uploaded images nor a clip's thumbnail (decided 2026-10-19), so
thumbnails are the only images the API serves.

Facts that shape the choice:

- **Browsers cache by the full URL.** The same URL is enough for a cache hit; a new signature is a new URL.
- **A presigned URL contains the time it was signed** (`X-Amz-Date`), not only the expiry. Rounding the expiry
  to fixed windows doesn't make it stable, and the AWS SDK can't set the signing time per request.
- **R2 presigned URLs only work on the S3 endpoint** (`<account>.r2.cloudflarestorage.com`), which Cloudflare's
  CDN doesn't cache. Edge caching needs a public bucket on a custom domain, which needs a domain on Cloudflare.
  The `r2.dev` URL is rate-limited and meant for development only.
- **R2 public access is per bucket**, not per prefix, so public thumbnails would need a second bucket.
- The scale is a handful of users. R2 doesn't charge for downloads. A grid of 50 thumbnails is about 1.5 MB.

## Options

### A. Public bucket on a custom domain, with unguessable keys

Thumbnails go to a public bucket, e.g. `https://media.example.com/thumbnails/{assetId}/{random}`. The API
returns plain URLs, `Cache-Control: public, max-age=31536000, immutable`. What YouTube and X do.

- Cached in the browser and at Cloudflare's edge, for good. No signing, and URLs never expire, so the app
  never has to refresh them.
- Needs a domain on Cloudflare, a second bucket (and its settings and CORS) in every environment, and the
  worker writing to it.
- Private only by obscurity: anyone with a link can see that thumbnail, and a leaked link can't be revoked
  except by deleting the image. Videos stay private either way. For a friends' clip library, a frame leaking is
  low stakes.

### B. Presigned URLs per response

The list signs each thumbnail URL, valid for a few hours, as `GET /assets/{id}` does. The URL asks R2 for
`Cache-Control: private, max-age=…` (the signed `response-cache-control` override, to verify against R2).
What GitHub (private images) and Discord (attachments) do.

- Private, one bucket, no domain, and nothing new to build: signing is computed locally and costs nothing per
  item.
- The browser reuses an image only while the app shows the same URL. The app's query cache keeps list
  responses, so going back to a grid usually hits the cache; a reload or refetch downloads the thumbnails
  again.

### C. Auth at the edge with a cookie (rejected)

Stable URLs on a custom domain, with a Cloudflare Worker (or CloudFront signed cookies) checking a session
cookie before reading from R2. Private and fully cacheable, but it needs a Worker, a domain on the same site
as the app, and moving sign-in from bearer tokens to cookies. Far too much for this project.

### D. The API serves the bytes (rejected)

`GET /assets/{id}/thumbnail` streams the image from R2. `<img>` can't send the bearer token, so the app would
fetch each image with the token and show it through a blob URL. Private and cacheable in the browser, but
every image goes through the API, which so far never handles file bytes ([architecture.md](../architecture.md)),
and the app needs its own image-loading code.

## Decision (so far)

- C and D are rejected, for the costs above.
- **Thumbnail keys are versioned** whichever of A or B wins: `assets/{id}/thumbnails/{version}`, with the
  version a new UUID v7 created by the domain. The content behind a key never changes, so a cached image is
  never stale, a re-processed clip gets a new key, and moving from B to A later only changes how URLs are
  built, with no data migration.
- **A or B** is decided with video processing, after finding out what a domain and the rest of A cost. B is
  the fallback if A isn't worth it.

## Rejected along the way

- **Reusing a presigned URL for a while** (an in-memory cache by key, so lists return the same URL for a day).
  It makes B cache better, but it's a workaround for a problem that isn't measured yet, and it adds state to
  the API.
- **Stable signatures by signing by hand.** The SDK can't fix the signing time, so it would mean writing SigV4
  query signing ourselves.
