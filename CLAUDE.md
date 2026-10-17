# Curator

Hobby project: media asset manager for videos (bulk upload, collections, search, sharing).
A fresh take on the ideas in `C:\code\asset-vault-api` and `C:\code\asset-vault-app`. Use those for
reference on _what_ to build, but don't copy their structure by default. This repo is deliberately a new approach.

We build it step by step. Keep changes small and focused on the step at hand; don't scaffold ahead.

Design decisions and plans live in `docs/`: `roadmap.md` (next steps), `data-model.md` (draft and open
questions) and `decisions/` (accepted decision records). Read them before starting a step, and update them
when a decision changes.

## Layout

```
apps/
  api/            .NET 10 backend (Curator.slnx)
    src/
      Curator.Api             ASP.NET Core host: endpoints, OpenAPI, Scalar
      Curator.Application     use cases, interfaces
      Curator.Domain          entities, value objects (no dependencies)
      Curator.Infrastructure  implementations of Application interfaces
    tests/
      Curator.UnitTests         Application/Domain tests
      Curator.IntegrationTests  WebApplicationFactory<Program> tests against the API
```

Dependency rule: Api → Application → Domain; Infrastructure → Application.

## Commands

Run from `apps/api`:

```bash
dotnet build Curator.slnx
dotnet test Curator.slnx
dotnet format Curator.slnx --verify-no-changes
dotnet watch run --project src/Curator.Api   # http://localhost:5238
dotnet ef migrations add <Name> --project src/Curator.Infrastructure --startup-project src/Curator.Api --output-dir Persistence/Migrations
dotnet ef database update --project src/Curator.Infrastructure --startup-project src/Curator.Api
```

- Local Postgres: `docker compose up -d` from the repo root (`compose.yaml`). Integration tests use
  Testcontainers, so Docker must be running for `dotnet test`.

- Storage: Cloudflare R2 (`R2` settings; keys in user secrets, see README). Tests never call R2: presigning
  is computed locally with made-up keys in `CuratorApiFactory`.

- API docs (Development only): http://localhost:5238/scalar
- Health: http://localhost:5238/health

If the API is already running (debugger/watch), it locks `bin/`. To build or test in that case, pass
`--artifacts-path <temp dir>` rather than killing the user's process.

## Conventions

- Shared MSBuild settings live in `apps/api/Directory.Build.props` (net10.0, nullable, warnings as errors).
- Package versions are centrally managed in `apps/api/Directory.Packages.props`. Add `<PackageVersion>` there
  and a version-less `<PackageReference>` in the csproj.
- Formatting: `.editorconfig` (LF, 2 spaces; 4 for C#) and Prettier for JSON/MD/web files.
- Constructors: primary constructors for types that only receive dependencies (services, `DbContext`,
  tests); regular constructors with `private set` properties for domain entities.
- Use cases: one file per use case in `Curator.Application/<Feature>/` (`Command` or `Query` for reads, `Response`, `Handler`),
  registered in `AddApplication()`, called from a thin controller. Handlers use `ICuratorDbContext` directly
  (no repositories) and validate with data annotations on the command. Expected failures (not found,
  forbidden, conflict) are returned as `Result<T>` and mapped with `result.ToActionResult(this, Ok)`;
  exceptions are for bugs. See `docs/decisions/0004-application-structure.md`.
- Auth: every endpoint requires a signed-in user unless marked `AllowAnonymous`. Read the user with
  `HttpContext.GetProfile()`. Integration tests sign in with `factory.CreateClient(userId)`, never real
  Supabase tokens. See `docs/decisions/0003-authentication-and-permissions.md`.
- `.http` files target the VS Code REST Client extension. Variable names must be plain words (`@host`, not `@Curator.Api_HostAddress`).
- Tests: xUnit 2. Every new endpoint gets at least one integration test.
- Commits: [Conventional Commits](https://www.conventionalcommits.org/): `type(scope): description`
  (`feat`, `fix`, `chore`, `docs`, `refactor`, `test`, `build`, `ci`; scope optional, e.g. `api`). Work directly on `main` for now.
