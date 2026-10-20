# curator

Media asset manager for videos. Upload in bulk, organize into collections, search, and share.

> Hobby project, built step by step.

## Structure

| Path       | What                                              |
| ---------- | ------------------------------------------------- |
| `apps/api` | .NET 10 Web API (Clean Architecture-style layers) |
| `infra`    | Cloud configuration (R2 bucket CORS)              |
| `docs`     | Roadmap, data model, architecture, design, ADRs   |

New here? [docs/architecture.md](docs/architecture.md) shows how a request flows through the layers, and
[docs/roadmap.md](docs/roadmap.md) what's next.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (pinned in `apps/api/global.json`)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (local Postgres and integration tests)
- [Node.js](https://nodejs.org/) for `npx` (Prettier, and wrangler for the R2 CORS policy)
- VS Code with the recommended extensions (`.vscode/extensions.json`)

## Getting started

```bash
docker compose up -d          # Postgres on localhost:5432 (curator/curator)
cd apps/api
dotnet tool restore           # dotnet-ef
dotnet ef database update --project src/Curator.Infrastructure --startup-project src/Curator.Api
dotnet build Curator.slnx
dotnet test Curator.slnx      # integration tests start their own Postgres container
dotnet watch run --project src/Curator.Api
```

Uploads go to a Cloudflare R2 bucket (`curator-dev` in Development, set in `appsettings.Development.json`).
Create an R2 API token with _Object Read & Write_ on that bucket only, and store its S3 keys as user
secrets (not the `cfat_` token value):

```bash
dotnet user-secrets set "R2:AccessKeyId" "<access key id>" --project src/Curator.Api
dotnet user-secrets set "R2:SecretAccessKey" "<secret access key>" --project src/Curator.Api
```

Without them the API starts and lists assets, but uploads and `GET /assets/{id}` (which signs playback URLs)
return 500. The tests don't need them.

Browsers `PUT` straight to the presigned URL, so the bucket needs a CORS policy for the app's origin
([`infra/r2/cors.json`](infra/r2/cors.json), the Vite dev server on `http://localhost:5173`). Apply it
once per bucket, and again after changing the file (`wrangler login` first):

```bash
npx wrangler r2 bucket cors set curator-dev --file infra/r2/cors.json   # from the repo root
npx wrangler r2 bucket cors list curator-dev
```

Add a migration after changing the model:

```bash
dotnet ef migrations add <Name> --project src/Curator.Infrastructure --startup-project src/Curator.Api --output-dir Persistence/Migrations
```

| URL                                   | What                           |
| ------------------------------------- | ------------------------------ |
| http://localhost:5238/health          | Health check                   |
| http://localhost:5238/scalar          | API reference (Development)    |
| http://localhost:5238/openapi/v1.json | OpenAPI document (Development) |

Every build also writes the OpenAPI document to [`apps/api/openapi.json`](apps/api/openapi.json), which is
committed: API changes show up in the diff, and the app's API client is generated from it. Commit it with the
change that caused it. It leaves out the Development endpoints (`/dev`).

The `.http` files run with the REST Client extension.

### Local data and tokens

Every endpoint except `/health` needs a Supabase access token, so you need a user in the Supabase project
(created in the dashboard under _Authentication_, until invitations exist). Run the `signIn` request in
[`Curator.Api.http`](apps/api/src/Curator.Api/Curator.Api.http) (credentials come from a git-ignored `.env` next
to it, see the top of the file; the publishable key is under _Project Settings → API Keys_). The other requests there use the token automatically. For Scalar, paste
`access_token` from the response into the _Bearer_ field; Scalar remembers it.

In Development, two endpoints manage local data:

| Request                 | What                                                                                     |
| ----------------------- | ---------------------------------------------------------------------------------------- |
| `POST /dev/sample-data` | Adds sample clips and collections by you and two sample users. No files are stored in R2 |
| `DELETE /dev/data`      | Deletes all assets, collections and the sample users. Your profile and role stay         |

`DELETE /dev/data` only clears a database on this machine (host `localhost`), and returns 403 otherwise, so a
server started in Development by mistake can't wipe the hosted one. It leaves files in R2: uploads made while
testing stay in `curator-dev` until storage cleanup exists (see the [roadmap](docs/roadmap.md)). To start
completely over, drop the database instead:
`docker compose down -v`, `docker compose up -d` and `dotnet ef database update`.

## VS Code

Launch configs (F5):

| Name              | What                                     |
| ----------------- | ---------------------------------------- |
| API (debug)       | Builds, runs the API and opens Scalar    |
| Attach to process | Attach the debugger to a running process |

Tasks (`Ctrl+Shift+P` → _Tasks: Run Task_):

| Task          | What                                               |
| ------------- | -------------------------------------------------- |
| `build`       | Build the solution (default build, `Ctrl+Shift+B`) |
| `test`        | Run all tests (default test task)                  |
| `watch api`   | Run the API with hot reload                        |
| `format all`  | Run `format api` + `format rest`                   |
| `format api`  | `dotnet format` the solution                       |
| `format rest` | Prettier over everything else                      |
| `docker up`   | `docker compose up -d` (local Postgres)            |
| `docker down` | `docker compose down`                              |

## Claude Code

Project context lives in [CLAUDE.md](CLAUDE.md), and shared settings and skills live in [.claude/](.claude/).

| Command   | What                                      |
| --------- | ----------------------------------------- |
| `/verify` | Build, check formatting and run all tests |

## License

[MIT](LICENSE)
