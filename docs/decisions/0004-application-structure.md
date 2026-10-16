# 0004: Application structure: use-case handlers and the DbContext

- **Status:** Proposed. Review it at the start of the uploads step, build the first use case with it, and
  accept or adjust it once it has met real code.
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

### Open, to settle in the uploads step

- **How handlers report failures** such as "not found" or "not allowed": a small result type mapped to
  `ProblemDetails` in the controller, or exceptions mapped by middleware.
- **Request validation:** data annotations on the request records, or checks in the handler and domain.

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
