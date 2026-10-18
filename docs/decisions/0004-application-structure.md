# 0004: Application structure: use-case handlers and the DbContext

- **Status:** Accepted, after the first use cases (`CreateUpload`, `CompleteUpload`) settled the open
  details.
- **Date:** 2026-10-16

## Context

The layers are set (Api → Application → Domain, Infrastructure → Application), but the Application layer is
still empty. The uploads step brings the first real use cases, so it has to decide how use cases are shaped
and how they reach the database.

The familiar .NET structure is Controller → Service → Repository: one service class per area
(`AssetService` with many methods), and repositories wrapping data access. This project is deliberately a
chance to learn a more current approach instead. EF Core's `DbContext` is already a repository and unit of
work (`db.Assets`, `SaveChangesAsync`), so custom repositories on top of it mostly forward calls.

## Decision

### One handler per use case

Each action is its own class in `Curator.Application`, grouped by feature. The request, the response and the
handler live in one file:

```
Curator.Application/
  Assets/
    CreateUpload.cs     // CreateUpload.Command, CreateUpload.Response, CreateUpload.Handler
    CompleteUpload.cs
  DependencyInjection.cs  // AddApplication(): registers the handlers explicitly
```

- Handlers are plain classes with primary constructors, registered in DI and injected into controllers.
  There's no mediator library (MediatR or similar).
- Controllers stay thin: map HTTP to a command, call the handler, map the result to HTTP. No business logic.
- Handlers take the signed-in user's profile (or ID) as input. They don't read `HttpContext`.

### The DbContext is the data-access interface

- Application declares `ICuratorDbContext` with the `DbSet`s it needs and `SaveChangesAsync`.
  `CuratorDbContext` in Infrastructure implements it.
- Handlers query it directly with LINQ, including projections straight into response records.
- Application references the `Microsoft.EntityFrameworkCore` package for `DbSet` and the async LINQ
  extensions, but **not** Npgsql or anything Postgres-specific. Database-specific code stays in
  Infrastructure.
- Other external systems get an interface in Application and an implementation in Infrastructure, for
  example storage for presigned R2 URLs.

### Rules live in the domain

- Entities guard their own state (`private set`, methods like `Asset.Delete(now)`), as they do today.
- Permission rules from [0003](0003-authentication-and-permissions.md) become domain methods, for example
  `asset.CanBeEditedBy(profile)`, written once and unit-tested without a database. Handlers call them.

### Tests

- Domain rules: unit tests (`Curator.UnitTests`).
- Use cases: integration tests through the endpoints, as now (every endpoint gets at least one). Handlers
  aren't unit-tested against a mocked `DbContext`, because that tests the mock rather than the queries.

### Unchanged

`ProfileProvisioner` stays an Infrastructure implementation of `IProfileProvisioner`. It's plumbing for
authentication (handling a Postgres unique violation), not a use case.

### Settled with the first use case (`CreateUpload`)

- **Handler signature:** `HandleAsync(Command command, Profile user, CancellationToken)`. The user is a
  separate parameter, not a field on the command, so the command can be the request body directly. There's
  no separate request DTO in the Api project that only copies fields. Route values that aren't part of the
  body come first (`EditAsset`: `HandleAsync(id, command, user, ct)`), and handlers that don't need the user
  leave it out (`ListAssets`).
- **Request validation:** data annotations on the command record. `[ApiController]` checks them and returns
  `ValidationProblemDetails` (400) before the handler runs, and OpenAPI picks them up. Entities still guard
  their invariants (`ArgumentException`), as a safety net rather than user-facing validation.
- **Time:** handlers take `TimeProvider` (registered as `TimeProvider.System`) instead of calling
  `DateTimeOffset.UtcNow`.
- **Storage keys are derived in the domain** from the entity's ID (`assets/{id}/original`). No user input
  (filenames) ends up in keys, and handlers don't need to know the ID before creating the entity.
- **Order of side effects:** call external systems that can fail (e.g. presigning) before
  `SaveChangesAsync`, so a failure doesn't leave orphaned rows.

### Settled with `CompleteUpload`: how handlers report failures

- **Expected failures are return values.** A handler that can fail returns `Result<Response>`
  (`Curator.Application.Common`): either the response or an `Error(Kind, Message)`. Implicit conversions let
  it `return response;` or `return Error.NotFound("...");`.
- **Kinds are few and generic:** `NotFound` (404), `Forbidden` (403), `Conflict` (409, wrong state), and
  `Invalid` (400) for input data annotations can't check, added with `ListAssets` for an unreadable cursor.
  Add one when a use case needs a status these don't cover, not up front.
- **The controller maps them** with `result.ToActionResult(this, Ok)`, which returns `ProblemDetails` with the
  message as `detail`. The endpoint lists each status with `[ProducesResponseType<ProblemDetails>]`.
- **Exceptions remain for bugs** and broken invariants (an entity's `InvalidOperationException`, a database
  that's down). They become a 500, never an expected outcome.
- Handlers that can't fail (like `CreateUpload`) keep returning the response directly.

Why not exceptions mapped by middleware (`NotFoundException` → 404 in an `IExceptionHandler`)? It's a valid,
widely used approach, with less code in controllers. It wasn't chosen because:

- Failures are part of the use case's contract. With a result they're visible in the signature, and the
  compiler makes the caller deal with them. A thrown exception can come from any depth, and nothing shows
  which ones a handler throws.
- "The file isn't uploaded yet" is a normal answer, not an exceptional situation. Exceptions as control flow
  make the flow harder to follow (and are slower, though that doesn't matter here).
- The mapping lives next to the endpoint and its `[ProducesResponseType]` attributes, instead of in a global
  handler that has to know every exception type.

The cost is a little more code per endpoint and no "early return from deep inside" (a helper that finds an
asset has to return a result too). If `ToActionResult` boilerplate or nested results become painful, revisit.
C# union types (expected in a future C# version) could replace the hand-written `Result<T>`.

## Alternatives considered

- **Controller → Service → Repository.** Familiar and still widely used. Not chosen: services grow into
  large classes, and repositories over EF Core mostly forward calls while hiding useful EF features
  (projections, `Include`, paging).
- **Repository interfaces in Application, EF only in Infrastructure.** Keeps Application free of EF Core.
  Not chosen for the same forwarding cost; the EF Core package reference is a pragmatic trade-off.
- **MediatR (or another mediator).** Adds indirection and pipeline behaviours that a project this size
  doesn't need. Plain DI is easier to follow and debug. Revisit if cross-cutting behaviour (logging,
  validation) gets repetitive.
- **Vertical slices in a single project** (endpoint, handler and data access per feature folder). Simpler,
  but it gives up the layering this repo is also meant to practise.

## Consequences

- Finding code means finding the feature folder and the use case by name, not scrolling a service class.
- Application depends on EF Core's abstractions. Swapping EF Core itself would touch the handlers;
  swapping Postgres wouldn't.
- Shared query logic (e.g. "assets visible in a collection") will appear. Extract it into extension methods
  on `IQueryable<T>` when it's actually repeated, not up front.

## References

- Jason Taylor's Clean Architecture template (`jasontaylordev/CleanArchitecture`): `IApplicationDbContext`
  in the Application layer.
- Milan Jovanović's posts on vertical slices and using EF Core without repositories.
