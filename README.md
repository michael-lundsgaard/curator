# curator

Media asset manager for videos. Upload in bulk, organize into collections, search, and share.

> Hobby project, built step by step.

## Structure

| Path       | What                                           |
| ---------- | ---------------------------------------------- |
| `apps/api` | .NET 10 Web API (Clean Architecture-style layers) |

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (pinned in `apps/api/global.json`)
- VS Code with the recommended extensions (`.vscode/extensions.json`)

## Getting started

```bash
cd apps/api
dotnet build Curator.slnx
dotnet test Curator.slnx
dotnet watch run --project src/Curator.Api
```

| URL                                   | What                          |
| ------------------------------------- | ----------------------------- |
| http://localhost:5238/health          | Health check                  |
| http://localhost:5238/scalar          | API reference (Development)   |
| http://localhost:5238/openapi/v1.json | OpenAPI document (Development) |

The `.http` files run with the REST Client extension.

## VS Code

Launch configs (F5):

| Name              | What                                          |
| ----------------- | --------------------------------------------- |
| API (debug)       | Builds, runs the API and opens Scalar         |
| Attach to process | Attach the debugger to a running process      |

Tasks (`Ctrl+Shift+P` → _Tasks: Run Task_):

| Task          | What                                              |
| ------------- | ------------------------------------------------- |
| `build`       | Build the solution (default build, `Ctrl+Shift+B`) |
| `test`        | Run all tests (default test task)                 |
| `watch api`   | Run the API with hot reload                       |
| `format all`  | Run `format api` + `format rest`                  |
| `format api`  | `dotnet format` the solution                      |
| `format rest` | Prettier over everything else                     |
| `docker up`   | `docker compose up -d` (no compose file yet)      |
| `docker down` | `docker compose down`                             |

## Claude Code

Project context lives in [CLAUDE.md](CLAUDE.md), and shared settings and skills live in [.claude/](.claude/).

| Command   | What                                       |
| --------- | ------------------------------------------ |
| `/verify` | Build, check formatting and run all tests  |

## License

[MIT](LICENSE)
