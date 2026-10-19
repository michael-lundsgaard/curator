# Architecture

How a request travels through the API. The reasons behind each part are in the ADRs linked below.

## Layers

| Project                  | Holds                                                       | References          |
| ------------------------ | ----------------------------------------------------------- | ------------------- |
| `Curator.Api`            | Host, middleware, thin controllers                          | Application, Infra  |
| `Curator.Application`    | Use cases (handlers), interfaces for the outside world      | Domain, EF Core     |
| `Curator.Domain`         | Entities and their rules (`Asset`, `Collection`, `Profile`) | nothing             |
| `Curator.Infrastructure` | `CuratorDbContext`, migrations, R2 storage, profile setup   | Application, Npgsql |

`Curator.Api` references Infrastructure only to call `AddInfrastructure()` in `Program.cs`.

## A request, step by step

Taking `PATCH /assets/{id}` (edit an asset) as the example:

1. **Authentication.** The JWT bearer handler validates the Supabase token (ES256, audience
   `authenticated`, `sub` must be a GUID). See [0003](decisions/0003-authentication-and-permissions.md).
2. **Profile.** `ProfileMiddleware` loads the profile with `id = sub`, creating it on the user's first
   request, and stores it on the `HttpContext`.
3. **Authorization.** The fallback policy rejects anonymous requests (401) unless the endpoint is
   `AllowAnonymous`.
4. **Controller.** `AssetsController.Edit` binds the body to `EditAsset.Command`. `[ApiController]` checks its
   data annotations and returns 400 before any of our code runs. The controller passes the route ID, the
   command and `HttpContext.GetProfile()` to the handler.
5. **Handler.** `EditAsset.Handler` (in Application) loads the asset through `ICuratorDbContext`, asks the
   entity whether the user may edit it (`asset.CanBeEditedBy(user)`), calls `asset.Edit(...)` and saves.
   Expected failures are returned as `Result<T>` errors (`NotFound`, `Forbidden`, …), not thrown. See
   [0004](decisions/0004-application-structure.md).
6. **Domain.** `Asset` guards its own state: private setters, methods that change it, and permission checks.
   No database or HTTP here.
7. **Response.** `result.ToActionResult(this, Ok)` turns the response into 200, or an error into
   `ProblemDetails` with the matching status. Anything thrown is a bug and becomes a 500.

```
HTTP ─► JWT bearer ─► ProfileMiddleware ─► authorization ─► Controller
                                                              │ command + profile
                                                              ▼
                            ICuratorDbContext ◄──────────── Handler ───► IFileStorage
                            (CuratorDbContext, Postgres)      │          (R2FileStorage)
                                                              ▼
                                                     Asset (domain rules)
```

## Outside systems

- **Postgres** through EF Core. Handlers write provider-neutral LINQ; Postgres-specific parts (the `pg_trgm`
  extension and indexes) live in Infrastructure. See [0005](decisions/0005-listing-and-searching-assets.md).
- **R2** through `IFileStorage`. The API never handles file bytes: it hands out presigned URLs, the browser
  uploads to and plays from R2 directly, and `complete-upload` asks R2 for the stored size.
- **Supabase Auth** only for validating tokens. The API doesn't call it per request; signing keys come from
  its discovery document and are cached.

## Where to add things

- A new endpoint: a use case file in `Curator.Application/<Feature>/` (`Command` or `Query`, `Response`,
  `Handler`), registered in `AddApplication()`, called from a controller action, and an integration test.
- A new rule about who may do what: a method on the entity, with a unit test.
- A new outside system: an interface in Application, the implementation in Infrastructure.
