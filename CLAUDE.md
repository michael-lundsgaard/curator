# Curator

Hobby project: media asset manager for videos (bulk upload, collections, search, sharing).
A fresh take on the ideas in `C:\code\asset-vault-api` and `C:\code\asset-vault-app`. Use those for
reference on _what_ to build, but don't copy their structure by default. This repo is deliberately a new approach.

We build it step by step, so each change stays small enough to learn from. A step can hold a few related changes
and land as one commit (or a couple, when parts stand on their own); don't split it into many tiny commits, and
don't scaffold ahead of the step.

Design decisions and plans live in `docs/`: `roadmap.md` (next steps), `data-model.md` (draft and open
questions), `architecture.md` (how a request flows through the layers), `design.md` (the app's brand, colors and
themes, with a link to the screen mocks) and `decisions/` (accepted decision records). Read them before starting a step, and update them
when a decision changes.

A roadmap entry says what was decided and, when it isn't obvious, why in a sentence, not how we got there. A
decision where alternatives were weighed or tried gets an ADR in `decisions/` (the options and why the others
lost), linked from the roadmap, so that history stays findable without digging through commit messages.

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
  app/            React app (Vite, TypeScript)
    src/api/      generated API types (schema.d.ts), the openapi-fetch client and its TanStack Query hooks ($api)
    src/auth/     Supabase auth client, and the frame of the sign-in and reset-password pages
    src/routes/   TanStack Router file-based routes (_authenticated.tsx is the shell and sign-in guard); routeTree.gen.ts is generated
    src/library/  clip listings (grid, table, sort and view controls, "Load more"), shared by the library and
                  collection pages, and how metadata is formatted
    src/collections/  the collection tree from GET /collections (the page is routes/_authenticated/collections.$id.tsx)
    src/clip/     clip details: the video player and the edit form (the page is routes/_authenticated/watch.$id.tsx)
    src/shell/    sidebar, top bar (with the search box), theme switch
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
  Testcontainers, so Docker must be running for `dotnet test`. To look at local data (read only, unless asked):
  `docker compose exec postgres psql -U curator -d curator -c "SELECT * FROM profiles"`.

- Storage: Cloudflare R2 (`R2` settings; keys in user secrets, see README). Tests never call R2: presigning
  is computed locally with made-up keys in `CuratorApiFactory`, and `TestFileStorage` answers "is the object
  there" from `factory.StoredObjects`.

- API docs (Development only): http://localhost:5238/scalar
- `apps/api/openapi.json` is the committed public OpenAPI document; never edit it by hand. `OpenApiDocumentTests`
  fails when it's out of date: update it from `apps/api` with
  `dotnet test tests/Curator.IntegrationTests --filter MatchesTheCommittedFile -e UPDATE_OPENAPI=1` and commit it
  with the API change that caused it. How it's shaped (schema names, enums, limits, query parameter names) is in
  `src/Curator.Api/OpenApi/`.
- Health: http://localhost:5238/health

Run from `apps/app`:

```bash
npm run dev            # http://localhost:5173, proxies /api to the API on :5238
npm run typecheck
npm run lint           # oxlint
npm run generate:api   # src/api/schema.d.ts from ../api/openapi.json, after every openapi.json change
npm run check:api      # fails when the committed schema.d.ts is out of date
```

- `npm run dev` and `npm run build` regenerate `src/routeTree.gen.ts` when a route file is added, renamed or
  removed; commit it with that change.
- Styling: Tailwind with the design tokens (`bg-surface`, `text-meta`, `border-line`; `src/index.css`), never raw
  colors or `dark:` variants: the tokens switch with the theme. See `docs/decisions/0010-tailwind-css.md`.
- Routing: `Link` and `navigate` from TanStack Router, typed against the routes. See
  `docs/decisions/0011-tanstack-router.md`.
- To look at the app, use the Playwright MCP (`.mcp.json`) against `npm run dev`, in both themes. Sign in as the
  test user: type `E2E_EMAIL` and `E2E_PASSWORD` in the sign-in form, and the MCP fills in the values from
  `.claude/test-user.local` (`--secrets`; never read that file). The test user is a Member.

- Call the API through `api` in `src/api/client.ts`, not a hand-written `fetch`; it adds the access token. In
  components, read with `$api.useQuery` / `$api.useInfiniteQuery` (TanStack Query). See
  `docs/decisions/0009-generated-api-client.md`.
- A `.env` file holds secrets only: it's git-ignored and closed to the agent. Public configuration is committed as
  code or `appsettings.json` (the app's Supabase URL and publishable key are in `src/auth/config.ts`).
- Auth: `auth` in `src/auth/client.ts` is Supabase's `AuthClient` (`supabase.auth` in Supabase's docs). Pages that
  need a signed-in user go in `src/routes/_authenticated/`. See `docs/decisions/0012-sign-in-in-the-app.md`.

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
- API documentation: the request and response records are the public contract. A parameter gets a `<param>` only
  when it says what the name and type can't: what `null` means, a default, a unit, a rule. Not "The new title.",
  "required" or limits, which the document already shows. Each action has a short `<summary>` (the label in Scalar), the details in
  `<remarks>`, and the use case as route name for the operation ID: `[HttpPatch("{id:guid}", Name = "EditAsset")]`.
  Other code is commented where it explains a reason or a rule, not on every member (CS1591 and CS1573 are off).
- Auth: every endpoint requires a signed-in user unless marked `AllowAnonymous`. Read the user with
  `HttpContext.GetProfile()`. Integration tests sign in with `factory.CreateClient(userId)`, never real
  Supabase tokens. See `docs/decisions/0003-authentication-and-permissions.md`.
- `.http` files target the VS Code REST Client extension. Variable names must be plain words (`@host`, not `@Curator.Api_HostAddress`).
- Tests: xUnit 2. Every new endpoint gets at least one integration test.
- Commits: [Conventional Commits](https://www.conventionalcommits.org/): `type(scope): description`
  (`feat`, `fix`, `chore`, `docs`, `refactor`, `test`, `build`, `ci`; scope optional, e.g. `api`). Work directly on `main` for now.
