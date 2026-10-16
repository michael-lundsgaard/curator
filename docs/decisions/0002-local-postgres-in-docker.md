# 0002: Local Postgres in Docker, shared Supabase Auth

- **Status:** Accepted
- **Date:** 2026-10-16

## Context

[0001](0001-supabase-for-postgres-and-auth.md) uses a single Supabase project and planned to move local
development off it "once friends use Curator". Doing it from the first migration is cheaper: there is no
data to move yet, and it keeps experiments (dropping tables, re-running migrations) away from the real
database.

The free tier gives only one project, so there is no separate Supabase project for development.

## Decision

- **Database:** local development uses Postgres in Docker (`compose.yaml` at the repo root,
  `postgres:17-alpine`, the same major version as Supabase). Integration tests use Testcontainers with the
  same image. Supabase Postgres is only used by the deployed API.
- **Auth:** there is only **one** Supabase Auth, and every environment uses it. The database and the
  identity provider are independent: the API validates the JWT against the Supabase JWKS and reads `sub`,
  and it never needs to query `auth.users`. So a local API against a local database accepts the same
  tokens as production.
  - Local `profiles` rows are created on first request from the token, just like in production. Resetting
    the local database is harmless; the profile is recreated on the next request.
  - The same Supabase user has the same `sub` locally and in production, but the rows are separate data.
- **Integration tests** don't call Supabase. They sign their own JWTs with a test key and point the API's
  JWT validation at that key. Details are settled in the authentication step.
- **Manual API calls** (`.http`, Scalar) need a real access token. Get one by signing in through the app,
  or with Supabase's password grant (`POST {Supabase:Url}/auth/v1/token?grant_type=password` with the
  publishable key as `apikey`) for a user created in the dashboard. You don't create or sign tokens in
  Supabase yourself.

### Alternatives considered

- **Supabase CLI local stack (`supabase start`).** Runs the whole of Supabase (Postgres, Auth, Studio, …)
  in Docker, including a local Auth with its own keys. Rejected for now: roughly ten containers for what
  is only a database, and its migration tooling overlaps with EF Core migrations. Revisit if we need to
  test invitations or auth email flows without touching the real project.
- **Keep using the Supabase database for development.** Rejected for the reasons in _Context_.

## Consequences

- Development and tests need Docker running.
- Real users exist in the shared Supabase Auth while their data is only local. Invitations sent while
  developing create real users, so use your own addresses.
- Free-tier pausing (0001) no longer affects day-to-day development, only signing in.
- Migrations are developed against plain Postgres, not Supabase. With only one project, the first time a
  migration meets Supabase is when it goes live. See _Parity with Supabase_.

## Parity with Supabase

Local Postgres is not identical to Supabase Postgres. Known differences and why they are acceptable:

| Difference                                | Risk and mitigation                                                                                                               |
| ----------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------- |
| Supabase extensions, roles, `auth` schema | Low. We don't depend on them (no FK to `auth.users`, no PostgREST, see 0001).                                                     |
| Automatic RLS on new tables               | Low. The API connects as `postgres`, which owns the tables, and owners bypass RLS. Confirm after the first migration on Supabase. |
| Connection pooler                         | Low on the Session pooler (port 5432). Don't use the Transaction pooler (port 6543): it breaks Npgsql's prepared statements.      |
| SSL and network                           | Only visible when deployed (`SSL Mode=Require`).                                                                                  |
| Major version                             | Pinned to `postgres:17` locally and in Testcontainers. Bump both when Supabase upgrades the project.                              |

Practices:

- **Until launch, Supabase is staging.** It holds no real data, so apply migrations to it now and then
  to catch differences early. Keep the Supabase connection string out of user-secrets (e.g. in a password
  manager) and pass it only for that command, so targeting Supabase is always deliberate:

  ```powershell
  $env:ConnectionStrings__Curator = '<Supabase Session pooler connection string>'
  dotnet ef database update --project src/Curator.Infrastructure --startup-project src/Curator.Api
  Remove-Item Env:ConnectionStrings__Curator
  ```

- **After launch**, review migrations as SQL (`dotnet ef migrations script --idempotent`) and apply them
  as an explicit deploy step, not automatically at API startup. Settled in the deployment step.

## Configuration

- `appsettings.Development.json`: `ConnectionStrings:Curator` points at the compose database
  (`curator`/`curator` on `localhost:5432`). These credentials are local only and not a secret.
- User secrets take precedence over `appsettings.Development.json`, so remove an old Supabase connection
  string: `dotnet user-secrets remove ConnectionStrings:Curator --project src/Curator.Api`.
- Production still uses the environment variable `ConnectionStrings__Curator` (Supabase Session pooler).
