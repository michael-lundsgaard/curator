# 0003: Authentication and permissions

- **Status:** Accepted
- **Date:** 2026-10-16

## Context

[0001](0001-supabase-for-postgres-and-auth.md) settled that the app signs in with Supabase Auth and sends the
access token to the API. This decision covers how the API validates it, where users and roles live, and
who may change what. The Supabase project signs tokens with an **ECC (P-256)** key (`ES256`).

## Decision

### Token validation

- Standard JWT bearer authentication with `Authority = {Supabase:Url}/auth/v1`. Issuer and signing keys
  come from Supabase's OIDC discovery document (`/.well-known/openid-configuration` → `jwks_uri`) and are
  cached and refreshed by the handler, so key rotation needs no change.
- Audience `authenticated`. Only `ES256` is accepted. HS256 tokens signed with the legacy shared secret
  are rejected, even though Supabase still advertises HS256.
- Claim names are kept as issued (`MapInboundClaims = false`). Supabase's `role` claim
  (`authenticated`) is **not** an ASP.NET role and isn't used.
- `sub` must be a GUID, otherwise the token is rejected.
- **Every endpoint requires a signed-in user by default** (fallback policy). Exceptions are explicit
  (`AllowAnonymous`): `/health`, and the OpenAPI document and Scalar in Development.

### Profiles

- A middleware after authentication loads the user's profile and creates it on their first request
  (`profiles.id = sub`). Endpoints read it with `HttpContext.GetProfile()`. Every authenticated request
  therefore has a profile row, so foreign keys like `uploaded_by` are always satisfied.
- Parallel first requests are safe: a unique violation on insert falls back to reading the existing row.
- `display_name` defaults to the part of the email before `@`. Editing it comes later.
- `GET /me` returns the signed-in user's profile (`id`, `displayName`, `role`).

### Roles and permissions

- `profiles.role` is `Member` (default) or `Admin`, stored as text. Roles live in our database, not in
  Supabase's JWT (no custom access token hook), so local and production databases can differ and a role
  change takes effect on the next request.
- There is no API for changing roles yet. Make someone an admin with SQL, also in the local database (it
  resets with it):

  ```sql
  UPDATE profiles SET role = 'Admin' WHERE id = '<user id>';
  ```

- Rules, enforced in the Application layer as the endpoints arrive:

  | Action                                                     | Who                                      |
  | ---------------------------------------------------------- | ---------------------------------------- |
  | See everything                                             | Everyone (shared library)                |
  | Upload clips, create collections                           | Everyone                                 |
  | Add or remove clips in any collection                      | Everyone                                 |
  | Create a sub-collection inside any collection              | Everyone                                 |
  | Edit or delete a clip                                      | Its uploader (`uploaded_by`), and admins |
  | Edit (name, description, cover), move or delete collection | Its creator (`created_by`), and admins   |

  Collections are shared group spaces ("R.E.P.O", "Jumpscares", "Winter Holiday 2026"), so everyone
  contributes to their contents. Removing a clip from a collection is harmless (the clip stays in the
  library and `added_by` shows who added it), so it isn't restricted. Changing or deleting the
  collection itself stays with its creator, because deleting also removes its sub-collections.

### Tests

- Integration tests never call Supabase. `CuratorApiFactory` creates its own P-256 key and serves it to
  the JWT handler through a static OIDC configuration. `factory.CreateClient(userId)` returns a client
  with a valid token, and `factory.CreateToken(...)` creates invalid variants (unknown key, HS256, wrong
  audience, expired, non-GUID `sub`).

## Alternatives considered

- **Roles as a JWT claim** via a Supabase custom access token hook. Rejected: it ties roles to the one
  shared Supabase Auth instead of each database, needs SQL in Supabase outside EF migrations, and role
  changes wait for the next token refresh.
- **Validating with the JWKS URL only** (no discovery). Not needed: Supabase serves a discovery document
  whose issuer matches the tokens.
- **Creating profiles lazily** where a use case needs one. Rejected: easy to forget, and the extra
  primary-key lookup per request is negligible for a handful of users.

## Consequences

- One primary-key lookup per authenticated request. Cache known profiles in memory if it ever matters.
- The API has no sign-in of its own. Manual calls need a Supabase token (see `Curator.Api.http`).
- If the project's signing key is changed to RSA, `ValidAlgorithms` must be updated.
