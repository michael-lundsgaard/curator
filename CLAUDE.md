# Curator

Hobby project: media asset manager for videos (bulk upload, collections, search, sharing).
A fresh take on the ideas in `C:\code\asset-vault-api` and `C:\code\asset-vault-app`. Use those for
reference on _what_ to build, but don't copy their structure by default. This repo is deliberately a new approach.

We build it step by step. Keep changes small and focused on the step at hand; don't scaffold ahead.

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
```

- API docs (Development only): http://localhost:5238/scalar
- Health: http://localhost:5238/health

If the API is already running (debugger/watch), it locks `bin/`. To build or test in that case, pass
`--artifacts-path <temp dir>` rather than killing the user's process.

## Conventions

- Shared MSBuild settings live in `apps/api/Directory.Build.props` (net10.0, nullable, warnings as errors).
- Package versions are centrally managed in `apps/api/Directory.Packages.props`. Add `<PackageVersion>` there
  and a version-less `<PackageReference>` in the csproj.
- Formatting: `.editorconfig` (LF, 2 spaces; 4 for C#) and Prettier for JSON/MD/web files.
- `.http` files target the VS Code REST Client extension. Variable names must be plain words (`@host`, not `@Curator.Api_HostAddress`).
- Tests: xUnit 2. Every new endpoint gets at least one integration test.
- Commits: [Conventional Commits](https://www.conventionalcommits.org/): `type(scope): description`
  (`feat`, `fix`, `chore`, `docs`, `refactor`, `test`, `build`, `ci`; scope optional, e.g. `api`). Work directly on `main` for now.
