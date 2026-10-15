# 0001: Supabase for Postgres and Auth

- **Status:** Accepted
- **Date:** 2026-10-15

## Context

Curator is a hobby project for a small, invite-only friend group. It needs a relational database and
authentication. Azure (Postgres Flexible Server + Entra External ID) was considered as a learning
opportunity, but it costs money after the first year and takes a lot of setup for ~10 users.

## Decision

Use one Supabase project (free tier, `eu-central-1`) for **Postgres and Auth only**.

- The .NET API is the only database client and uses EF Core + Npgsql. Schema changes go through EF Core
  migrations.
- The Supabase Data API (PostgREST) is **disabled**, and new tables are not exposed automatically.
  Automatic RLS is enabled to deny access by default.
- The React app signs in with `supabase-js` and sends the access token to the API as a Bearer token. The
  API validates it against the project's JWKS (`{Supabase:Url}/auth/v1`).
- Sign-up is disabled. Users join only by invitation (`inviteUserByEmail`, called server-side).
- Video files are **not** stored in Supabase Storage (1 GB, 50 MB upload limit on the free tier). They go
  to Cloudflare R2, and the database stores only metadata and storage keys.
- `profiles.id` equals the JWT `sub` claim. There is **no** foreign key to `auth.users`, so local and
  test databases can be plain Postgres.
- The Supabase GitHub integration is not used, because it would conflict with EF Core migrations.

## Consequences

- Postgres is portable. Moving to another host is mostly a connection string change.
- Auth is the main thing tying us to Supabase, but it's standard JWT validation, so switching means
  changing the issuer and audience config.
- Free-tier projects **pause after ~7 days of inactivity**. Accept this, or add a scheduled ping later.
- Built-in auth email is heavily rate-limited. That's fine for invites; add custom SMTP if needed.
- With only one Supabase project, local development hits the real database. Once friends use Curator,
  move local development to Postgres in Docker. Integration tests use Testcontainers.

## Configuration

- `appsettings.json`: `Supabase:Url` (not a secret) and an empty `ConnectionStrings:Curator`.
- Development: the connection string goes in user-secrets (Session pooler, port 5432, key=value format,
  because Npgsql doesn't accept `postgresql://` URIs):

  ```powershell
  dotnet user-secrets set 'ConnectionStrings:Curator' 'Host=aws-1-eu-central-1.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.jnofblywuslpfnouxniw;Password=...;SSL Mode=Require' --project src/Curator.Api
  ```

- Production: environment variable `ConnectionStrings__Curator`.
