# 0009: The generated API client

- **Status:** Accepted
- **Date:** 2026-10-20

## Context

The React app (`apps/app`) calls the API through a client generated from the committed `apps/api/openapi.json`
([0008](0008-openapi-document.md)). The roadmap named two candidates: hey-api/openapi-ts, which asset-vault uses,
and openapi-typescript with openapi-fetch.

What matters here:

- **Readable diffs.** An API change should show up in the app as a change you can read, next to the
  `openapi.json` diff that caused it, not as dozens of regenerated files.
- **Little between the app and `fetch`.** This is a learning project, so the less generated runtime code there is
  to understand, the better.
- **TanStack Query when the Library needs it**, including infinite queries for "Load more" with the cursor.

## Decision

**openapi-typescript generates types only; openapi-fetch makes the calls.**

- `npm run generate:api` writes `src/api/schema.d.ts` (`paths`, `components`, `operations`) from
  `../api/openapi.json`. It's committed, so an API change and its effect on the app's types land in the same
  commit. Prettier skips it, as it does `openapi.json`, so the generator's output stays byte for byte.
- `npm run check:api` regenerates the file and fails when it differs from the committed one, the app-side twin of
  `OpenApiDocumentTests`.
- `src/api/client.ts` creates one client, `createClient<paths>({ baseUrl: "/api" })`. Calls name the method and
  path, and TypeScript checks the parameters, body and response against the document:
  `api.GET("/assets/{id}", { params: { path: { id } } })`. A body missing a required field, or a value outside an
  enum, is a type error.
- The Supabase access token will be added in an openapi-fetch middleware (`onRequest`) at the sign-in step.
- TanStack Query comes through **openapi-react-query** (`$api.useQuery`, `useMutation`, `queryOptions`,
  `useInfiniteQuery`), added at the Library step. Its `useInfiniteQuery` sends the page parameter as `cursor` by
  default, which is what `GET /assets` uses.

## Alternatives considered

- **hey-api/openapi-ts.** Generates a function per operation, named from our operation IDs (`editAsset()`), and
  has plugins for TanStack Query (including infinite queries), Zod schemas and date transformers. Not chosen:
  it's pre-1.0 and its configuration has broken between minor versions (asset-vault's `@hey-api/client-fetch`
  has since been folded into the main package), and it generates an SDK and plugin output to commit or
  regenerate, so an API change touches many files. What we give up: calls by operation name and generated query
  options. The compiler finds every call to a renamed path, and openapi-react-query covers the queries.
- **Orval.** Worth it for generated MSW mocks; we don't need those yet. Heavier than hey-api otherwise.
- **NSwag, Kiota, openapi-generator.** From the .NET and Java worlds: class-based or builder-style clients with
  a heavy runtime and weaker OpenAPI 3.1 support.

## Consequences

- After an API change: update `openapi.json` (0008), run `npm run generate:api` in `apps/app`, fix any type
  errors, and commit all three together.
- Dates are ISO strings in the types (`format: date-time` is only a comment). Parse them where they're shown.
- `$api` in `src/api/client.ts` is the TanStack Query side. For "Load more", `useInfiniteQuery` takes
  `initialPageParam: null`: openapi-react-query sends `0` as the cursor when it's `undefined`, which the API rejects,
  while openapi-fetch leaves `null` out of the query string.
- **TypeScript 6 override.** openapi-typescript 7.13 declares a peer dependency on TypeScript 5, and the Vite
  template uses 6. `package.json` has an `overrides` entry that lets it use the app's TypeScript; it only uses
  TypeScript to print the `.d.ts`, and the output typechecks. Remove the override once openapi-typescript allows
  TypeScript 6.
