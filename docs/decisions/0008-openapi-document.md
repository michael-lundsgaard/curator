# 0008: The OpenAPI document

- **Status:** Accepted
- **Date:** 2026-10-20
- **Revised:** 2026-10-21: dropped the tests of how the generator words the document, and the rule that every
  field is described (see "Revised" under Alternatives).

## Context

The React app gets a generated API client, so the OpenAPI document becomes a contract that code is generated
from, not only what Scalar shows. Two things follow:

- **The document must be in the repo**, so API changes show up in a diff and the client can be generated without
  a running API.
- **It must be right**, not just present. The generated client's types and function names come from it, so a
  wrong schema becomes a wrong type in the app.

ASP.NET Core's built-in generator (`Microsoft.AspNetCore.OpenApi`, 10.0.13) gets several things wrong for our
code, mostly because use cases nest their types (`EditAsset.Command`) and validate with attributes on record
parameters (0004). Upgrading from 10.0.2 to 10.0.13 fixed none of them.

## Decision

**A committed file, kept current by a test.** The public document is `apps/api/openapi.json`.
`OpenApiDocumentTests.MatchesTheCommittedFile` compares it with the document the API serves and fails when they
differ. It only writes the file when asked (`UPDATE_OPENAPI=1`), so a stale file keeps failing until someone
updates and commits it. `/verify` runs the tests before every commit, and CI will run them too, so a stale file
can't slip through either way.

This snapshot is also what checks the transformers below: any change to the document shows up as a diff to
review. Apart from it, tests only check rules the app's client relies on (every operation has an ID, enums by
name, camelCase query parameters, `/dev` stays out), not how the generator words things.

**The development endpoints have their own document** (`/openapi/dev.json`, also listed in Scalar), so `/dev`
never reaches the committed file or the app's client, even though the tests run in Development.

**Transformers fix what the generator gets wrong.** They live in `Curator.Api/OpenApi/`, each with a comment
saying why:

| Without it                                                                            | With it                                             |
| ------------------------------------------------------------------------------------- | --------------------------------------------------- |
| Every nested `Command`, `Response` and `Item` collapses into one schema per name      | `EditAssetRequest`, `EditAssetResponse`, …          |
| Enums have no `type`, and `null` is listed as a value                                 | `type: string`, no `null`                           |
| `[MaxLength]`, `[Length]`, `[Range]`, `[Required]` on record parameters are not read  | `maxLength`, `minItems`, `minimum`, `minLength: 1`  |
| Query parameters are PascalCase (`Cursor`)                                            | camelCase, as clients send them                     |
| Errors are listed as `application/json`                                               | `application/problem+json`, what the API sends      |
| XML comments keep the OS's line breaks, so the file differs between Windows and Linux | Joined with a space                                 |
| The host the document was requested from is listed in `servers`                       | No `servers`; Scalar and the app use their own host |

Some fixes are in the API's behaviour instead, so the document describes it truthfully: JSON only (no
`text/plain`, `text/json` or `application/*+json`), numbers as numbers (no `"5"`), and every key without a default
required in request bodies (`RespectRequiredConstructorParameters`).

**Documentation conventions** (in CLAUDE.md): a short `<summary>` per action, the details in `<remarks>`, the
use case as route name for the operation ID, and a `<param>` on a field of a request or response record only when
it says what the name and type can't: what `null` means, a default, a unit, a rule. Nothing enforces it.

## Alternatives considered

- **Writing the file on every build** (`Microsoft.Extensions.ApiDescription.Server`). Tried first and dropped. It
  runs the app's whole startup in every build, including `dotnet test` and `dotnet watch`, and also the code
  after `builder.Build()`, so startup could never need a database or keys (no migrations on start). It also adds
  seconds to every build. The file couldn't go stale locally, but CI would still need a separate check.
- **Copying the document from `/openapi/v1.json` by hand.** Goes stale, needs the API running, and would include
  the Development endpoints.
- **A test that writes the file when it differs, then fails.** The next run passes, so the failure "fixes
  itself" locally and is easy to miss. Only writing on request keeps it failing.
- **Validation attributes on properties** (`[property: MaxLength(255)]`) so the generator reads them. MVC rejects
  validation attributes on the properties of a positional record, which is why the transformer reads them from
  the record's parameters.
- **Removing `null` from enums only where they're optional.** The generator shares one schema per enum, so it's
  all or nothing. The only nullable enum is the optional `order` query parameter; a nullable enum in a body
  throws, which fails the document tests, so it can't be documented wrongly by accident.

**Revised (2026-10-21).** After a review, these were dropped or kept:

- **Dropped: a description on every field and query parameter**, enforced by tests. It produced descriptions
  like "The new title." that only repeated the name.
- **Dropped: tests of the generator's output** (schema names, content types, limits, one-line descriptions, the
  bearer scheme). The snapshot already fails on any of these changing.
- **Kept: validation limits in the document.** The generated client is types only and ignores them today, but
  they're part of the contract the app's forms need (a title's `maxLength`), and without them the app would
  hard-code the numbers.
- **Kept: joining description lines.** Normalizing line breaks in the snapshot test would make the file the same
  on every OS too, but leaves the 120-column wrapping of the XML comments, and the indentation next to a `$ref`,
  in every description.

## Consequences

- After an API change, run the update command and commit `openapi.json` with it. Updating needs Docker, like the
  rest of the integration tests.
- `CommittedFilePath` finds the file through `[CallerFilePath]`. Deterministic CI builds (`PathMap`) rewrite that
  path, so it needs another way to find the file when CI is set up.
- A nullable enum in a request or response needs its own schema before it can be added.
- A missing key in a request body is a 400 whose message names the internal type (`EditAsset+Command`) and adds a
  generic `command` error, as for any unreadable JSON body. Cleaning that up is left to the step where the app
  shows server errors next to form fields.
